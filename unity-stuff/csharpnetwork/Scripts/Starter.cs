using UnityEngine;
using System.Collections;
using NetMsg;

public class Starter : MonoBehaviour {
	public string host;
	public int port;
	
	
	public Net.NetworkConnection conn;
	// Use this for initialization
	void Start () {

		if(conn != null) {
			conn.AddHandler((int)NetMsg.NetMsgID.LOGIN_MSG_GET_VERSION, OnGetVersion);
			conn.networkErrorCallback += OnNetworkError;
			conn.Connect(host, port, (obj) => {
				Debug.Log("network connected...");
//				GetVersion(conn);
//				GetVersion(conn);
//				GetVersion(conn);
			});
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	void GetVersion(Net.NetworkConnection conn)
	{
		var getver = new NetMsg.CGetVersion();
		byte[] buff = Net.PacketHelper.Pack(getver);
		conn.Send((int)NetMsgID.LOGIN_MSG_GET_VERSION, buff);
	}
	void OnGetVersion(int type, byte[] data)
	{
		Debug.Log("server version...");
		SGetVersion ver = Net.PacketHelper.Unpack(typeof(SGetVersion), data) as SGetVersion;
		Debug.Log(ver.version);
	}
	
	void OnNetworkError(string message)
	{
		Debug.Log("on network error:" + message);
		conn.Reconnect((obj) => {
			if(obj)
			Debug.Log("reconnect:.....");	
		});
	}
}
