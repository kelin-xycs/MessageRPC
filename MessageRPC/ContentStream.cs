using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Net.Sockets;

namespace MessageRPC
{
    public class ContentStream : Stream
    {

        private Socket socket;
        private byte[] bytes;
        private long length;
        private long position = 0;


        internal ContentStream(Socket socket, byte[] bytes, long length)
        {
            this.socket = socket;
            this.bytes = bytes;
            this.length = length;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { throw new NotImplementedException(); }
        }

        public override bool CanWrite
        {
            get { throw new NotImplementedException(); }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Length
        {
            get { return length; }
        }

        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {


            long remainLength = this.length - this.position;


            if (remainLength == 0)
                return 0;


            if (remainLength < count)
            {
                count = (int)remainLength;
            }


            int readCount = 0;


            if (this.bytes != null && this.position < this.bytes.Length)
            {

                int bytesRemainLength = this.bytes.Length - (int)this.position;

                readCount = bytesRemainLength > count ? count : bytesRemainLength;

                
                for (int i = 0; i < readCount; i++)
                {
                    buffer[offset + i] = this.bytes[this.position + i];
                }
                

                this.position += readCount;

            }
            else
            {
                readCount = this.socket.Receive(buffer, offset, count, SocketFlags.None);

                this.position += readCount;
            
            }

            return readCount;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
