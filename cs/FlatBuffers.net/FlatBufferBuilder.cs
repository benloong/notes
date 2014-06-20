using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatBuffers
{
    //reverse grow buffer
    public class Buffer
    {
        private byte[] buf = new byte[1024];
        private int    cur = 1024;

        public byte[] Buf
        {
            get
            {
                return buf;
            }
        }

        public int Offset
        {
            get
            {
                
                return cur;
            }
            set
            {
                cur = value;
                if (value < 0)
                {
                    byte[] newbuf = new byte[System.Math.Max(buf.Length * 2, -value)];
                    buf.CopyTo(newbuf, newbuf.Length - buf.Length);
                    cur = newbuf.Length - buf.Length + cur;
                }
            }
        }

        public int Length
        {
            get
            {
                return buf.Length - cur;
            }
        }

        public Buffer()
        {

        }

        public Buffer(byte[] bb)
        {
            buf = bb;
            Offset = 0;
        }

        public void Push(byte b)
        {
            Offset -= sizeof(Int32);
            buf[Offset] = b;
        }

        public void Push(byte[] data)
        {
            Offset -= PaddingBytes(data.Length, sizeof(Int32)) + data.Length;
            data.CopyTo(buf, Offset);
        }

        public void Fill(int bytes, byte b = 0)
        {
            Offset -= PaddingBytes(Length, bytes) + bytes;
            for (int i = 0; i < bytes; i++)
            {
                buf[Offset + i] = b;
            }
        }

        public void Pop(int sz)
        {
            Offset += sz;
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[Length];
            for (int i = 0; i < Length; i++)
			{
                bytes[i] = buf[Offset+i];
			}
            return bytes;
        }
        
        public static int PaddingBytes(int buf_size, int scalar_size) 
        {
          return ((~buf_size) + 1) & (scalar_size - 1);
        }
    }

    public struct Struct
    {
        int offset;
        Buffer buffer;

        public void __Init(int off, Buffer buf)
        {
            offset = off;
            buffer = buf;
        }

        public Single GetSingle(int pos)
        {
            return BitConverter.ToSingle(buffer.Buf, offset + pos);
        }

        public Double GetDouble(int pos)
        {
            return BitConverter.ToDouble(buffer.Buf, offset + pos);
        }

        public Int32 GetInt32(int pos)
        {
            return BitConverter.ToInt32(buffer.Buf, offset + pos);
        }

        public Int16 GetInt16(int pos)
        {
            return BitConverter.ToInt16(buffer.Buf, offset + pos);
        }

        public Byte GetByte(int pos)
        {
            return buffer.Buf[offset + pos];
        }

        public Boolean GetBoolean(int pos)
        {
            return BitConverter.ToBoolean(buffer.Buf, offset + pos);
        }
    }

    public struct Table
    {
        int offset;
        Buffer buffer;
        public void __Init(int off, Buffer buf)
        {
            offset = off;
            buffer = buf;
        }
    }

    public class FlatBufferBuilder
    {
        Buffer buffer = new Buffer();

        public int CreateString(string s)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(s);
            buffer.Push(bytes);
            return buffer.Length;
        }

        public static T GetRoot<T>(byte[] data) where T : Table
        {
            T root = default(T);
            Buffer buffer = new Buffer(data);
            root.__Init(BitConverter.ToInt32(buffer.Buf, 0), buffer);
            return root;
        }

        public void AddList(int off, int count)
        {

        }

        public void AddOffset(int off)
        {
            AddInt32(off);
        }

        private void AddBytes(byte[] p)
        {
            throw new NotImplementedException();
        }

        public void AddByte(Byte value)
        {
            buffer.Push(value);
        }

        public void AddInt32(Int32 value)
        {
            buffer.Push(BitConverter.GetBytes(value));
        }

        public void AddInt64(Int64 value)
        {
            buffer.Push(BitConverter.GetBytes(value));
        }

        public void AddSingle(Single value)
        {
            buffer.Push(BitConverter.GetBytes(value));
        }

        public void AddDouble(Single value)
        {
            buffer.Push(BitConverter.GetBytes(value));
        }

        public void AddBool(Boolean value)
        {
            buffer.Push(BitConverter.GetBytes(value));
        }

        public void BeginTable(int sz)
        {

        }

        public int EndTable()
        {
            return 0;
        }
    }
}
