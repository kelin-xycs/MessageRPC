using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;

using MessageRPC;

namespace MessageRPC
{
    public class RPC
    {

        private SocketPool socketPool;

        public RPC(string ip, int port)
        {
            IPAddress ipAddress = IPAddress.Parse(ip);
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, port);

            this.socketPool = SocketPool.GetPool(ipEndPoint);
        }

        public RMessage Send(SMessage m)
        {

            Socket clientSocket = null;

            RMessage rMsg;

            try
            {
                clientSocket = this.socketPool.Get();

                MessageParser mp = new MessageParser();

                mp.Send(ref clientSocket, m, this.socketPool);

                rMsg = mp.Parse(clientSocket);

                this.socketPool.Return(clientSocket);
            }
            catch
            {

                if (clientSocket != null)
                {
                    this.socketPool.Close(clientSocket);
                }

                throw;
            }


            if (rMsg.Error != null)
            {
                throw new RPCServerException(rMsg.Error);
            }

            return rMsg;
        }
    }
}
