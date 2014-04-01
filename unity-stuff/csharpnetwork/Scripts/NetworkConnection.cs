using System;
using System.Collections.Generic;
using System.Net.Sockets;
using ProtoBuf.Meta;
using UnityEngine;

namespace Net {
	public enum NetworkState
	{
		Disconnected,
		Disconnecting,
		Connecting,
		Connected,
		MAX,
	}
	/// <summary>
	/// protobuf  PacketHelper functions .
	/// </summary>
	public static class PacketHelper
	{
		public static TypeModel serializer = new ProtosSerializer();
		public static byte[] Pack(object msg)
		{
			System.IO.MemoryStream mmstream = new System.IO.MemoryStream();
			serializer.Serialize(mmstream, msg);
			return mmstream.ToArray();
		}
		
		public static object Unpack(Type type, byte[] data)
		{
			object packet = null;
			using(var mms = new System.IO.MemoryStream(data,2,data.Length-2))
			{
				packet = serializer.Deserialize(mms, null, type);
			}
			return packet;
		}
		
		public static byte[] PackHeader(int header)
		{
			//endian convert
			//not use bitcovert
			byte[] buff = new byte[2];
			buff[0] = (byte)(header>>8 & 0xff);
			buff[1] = (byte)(header&0xff);
			return buff;
		}
		
		public static int UnpackHeader(byte[] buff)
		{
			int head = 0;
			head |= (buff[0] << 8) & 0xff00;
            head |= (buff[1]) & 0xff;
			return head;
		}
	}
	
	public class NetworkConnection : MonoBehaviour {
		delegate void Notification();
		NetworkState netstate = NetworkState.Disconnected;
		Socket socket = null;
		
		const int headerSize = 2;//packet header size in byte
		//TODO: check sum packet data
		const int checksumSize = 4;//packet check sum size in byte.
		
		int packetSize = 0;
		byte[] header = new byte[headerSize];
		byte[] buffer = null;
		
		public string host;
		public int port;
		
		public System.Action<bool> connectCallback;
		public System.Action<string> networkErrorCallback;
		
		Dictionary<int, System.Action<int, byte[]>> handlerMap = new Dictionary<int, System.Action<int, byte[]>>();
		
		public NetworkState networkState
		{
			get {
				return netstate;	
			}
			private set {
				netstate = value;	
			}
		}
		
		public Socket Socket
		{
			get {
				if(socket == null) {
					socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					socket.SetSocketOption(SocketOptionLevel.Tcp,SocketOptionName.NoDelay, true);
				}
				return socket;
			}
		}
		
		List<byte[]> sendQueue = new List<byte[]>();
		
		Queue<Notification> notifications = new Queue<Notification>();
		
		#region Unity callback
		void Awake () {
			//null object parttern
			connectCallback = NullConnectCallback;
			networkErrorCallback = NullErrorCallback;
		}

		void Update () {
			Notify();
		}
		
		void OnDisable()
		{
			Close();
		}
		#endregion
		
		#region public network interface
		/// <summary>
		/// Send the specified packet type id and packet data.
		/// </summary>
		/// <param name='typeId'>
		/// packet type identifier.
		/// </param>
		/// <param name='data'>
		/// packet data
		/// </param>
		public void Send(int typeId, byte[] data)
		{
			int length = data.Length + 2;
			byte[] needsend = new byte[data.Length + 4];
			byte[] header = PacketHelper.PackHeader(length);
			byte[] typeheader = PacketHelper.PackHeader(typeId);
 			for (int i = 0; i < header.Length; i++) {
				needsend[i] = header[i];
			}
			for (int i = 0; i < typeheader.Length; i++) {
				needsend[i+2] = typeheader[i];
			}
			for (int i = 0; i < data.Length; i++) {
				needsend[i+4] = data[i];
			}
			
			if(networkState == NetworkState.Connected) {
				Debug.Log("send data: "+typeId);
				socket.BeginSend(needsend,0,needsend.Length,SocketFlags.None, OnSendResult, socket);	
			}
		}
		
		/// <summary>
		/// Connect the specified ip, port and connectCallback.
		/// </summary>
		/// <param name='ip'>
		/// host Ip.
		/// </param>
		/// <param name='port'>
		/// host Port.
		/// </param>
		/// <param name='connectCallback'>
		/// Connect callback.
		/// </param>
		public void Connect(string host, int port, System.Action<bool> connectCallback)
		{
			this.host = host;
			this.port = port;
			this.connectCallback = connectCallback ?? NullConnectCallback;
			try {
				Debug.Log("connecting...");
				this.Socket.BeginConnect(this.host, this.port, OnConnectResult, this.Socket);
				networkState = NetworkState.Connecting;
			} catch (Exception ex) {
				networkState = NetworkState.Disconnected;
				Debug.LogException(ex);
			}
		}
		
		/// <summary>
		/// Reconnect network.
		/// </summary>
		/// <param name='reconnectCallback'>
		/// Reconnect callback.
		/// </param>
		public void Reconnect(System.Action<bool> reconnectCallback)
		{
			this.connectCallback = reconnectCallback ?? NullConnectCallback;
			try {
				if(networkState == NetworkState.Disconnected && !Socket.Connected) {
					Debug.Log("start reconnecting...");
					this.Socket.BeginConnect(this.host, this.port, OnConnectResult, this.Socket);
					networkState = NetworkState.Connecting;
				}
			} catch (SocketException ex) {
				networkState = NetworkState.Disconnected;
				Debug.LogException(ex);
			} catch (Exception ex) {
				//pass	
			}
		}
		
		/// <summary>
		/// Adds one process handler of specific packet type.
		/// </summary>
		/// <param name='typeId'>
		/// packet type identifier.
		/// </param>
		/// <param name='handler'>
		/// packet process handler.
		/// </param>
		public void AddHandler(int typeId, System.Action<int, byte[]> handler)
		{
			if(!handlerMap.ContainsKey(typeId)) {
				handlerMap.Add(typeId,(arg1, arg2) => {});
			}
			handlerMap[typeId] += handler;
		}
		
		/// <summary>
		/// Removes the hanlder of specific packet type.
		/// </summary>
		/// <param name='typeId'>
		/// packet type identifier.
		/// </param>
		/// <param name='handler'>
		/// packet handler.
		/// </param>
		public void RemoveHanlder(int typeId, System.Action<int, byte[]> handler)
		{
			if(handlerMap.ContainsKey(typeId)) {
				handlerMap[typeId] -= handler;
			}
		}
		
		/// <summary>
		/// Close network connection, reset socket
		/// </summary>
		public void Close()
		{
			try {
				if(socket != null) {
					if(socket.Connected) {
						socket.Shutdown(SocketShutdown.Both);
					}
					networkState = NetworkState.Disconnected;
					socket.Close();
					socket = null;
				}
			} catch (Exception ex) {
				Debug.LogException(ex);
			}
		}
		#endregion
		
		void OnConnectResult(IAsyncResult result)
		{
			try {
				Socket socket = (Socket)result.AsyncState;
				socket.EndConnect(result);
				networkState = NetworkState.Connected;
				PostNotification(()=> {connectCallback(true);});
				//start recieve packet from server
				RecievePacket(socket);
				
			} catch (SocketException ex) {
				Close();
				networkState = NetworkState.Disconnected;
				PostNotification(() => {connectCallback(false);});
				Debug.LogException(ex);
			} catch (Exception ex) {
				//pass	
			}
		}
		
		void OnSendResult(IAsyncResult result)
		{
			try {
				Socket socket = result.AsyncState as Socket;
				socket.EndSend(result);
				
			} catch (SocketException ex) {
				Close();
				networkState = NetworkState.Disconnected;
				PostNotification(() => {networkErrorCallback(ex.Message);});
				Debug.LogException(ex);
			} catch (Exception ex) {
				//pass	
			}
		}
		
		#region recieve
		void RecievePacket(Socket sock)
		{
			try {
				sock.BeginReceive(header, 0, header.Length, SocketFlags.None, OnRecieveHeader, sock); 
			} catch (Exception ex) {
				Debug.LogException(ex);
			}
		}
		
		void OnRecieveHeader(IAsyncResult result)
		{
			try {
				Socket socket = result.AsyncState as Socket;
				socket.EndReceive(result);
				
				packetSize = PacketHelper.UnpackHeader(header);
				buffer = new byte[packetSize];
				socket.BeginReceive(buffer, 0, packetSize, SocketFlags.None, OnRecievePacket, socket);
			} catch (SocketException ex) {
				Close();
				PostNotification(() => {networkErrorCallback(ex.Message);});
				Debug.LogException(ex);
			} catch	(Exception ex) {
				//pass 
			}
		}
		
		void OnRecievePacket(IAsyncResult result)
		{
			try {
				Socket socket = result.AsyncState as Socket;
				int len = socket.EndReceive(result);
				if(len != packetSize)
				{
					throw new OverflowException("packet size not equal!");
				}
				PostNotification(() => {
					int typeid = PacketHelper.UnpackHeader(buffer);
					if(!handlerMap.ContainsKey(typeid)) {
						Debug.LogWarning("type id not registered: "+typeid.ToString());
						return;
					}
					handlerMap[typeid](typeid, buffer);
				});
				//recieve next packet 
				RecievePacket(socket);
			} catch (SocketException ex) {
				Close();
				PostNotification(() => {networkErrorCallback(ex.Message);});
				Debug.LogException(ex);
			} catch (Exception ex) {
				//pass
			}
		}
		#endregion
		/// <summary>
		/// Posts one completion notification
		/// </summary>
		/// <param name='handler'>
		/// completion notify 
		/// </param>
		void PostNotification(Notification handler)
		{
			lock (notifications) {
				notifications.Enqueue(handler);
			}
		}
		
		/// <summary>
		/// Notify all notifications in completion queue
		/// called per frame in update
		/// return how many notifications process in this call
		/// </summary>
		int Notify()
		{
			int count = 0;
			while(true)
			{
				Notification notice = null;
				lock (notifications) {
					try {
						notice = notifications.Dequeue();
					} catch (InvalidOperationException ex) {
						
					}
				}
				if(notice == null) break;
				count ++;
				notice();
			}
			return count;
		}
		
		
		void NullConnectCallback(bool success)
		{
		}
		
		void NullErrorCallback(string error)
		{
		}
	}
}