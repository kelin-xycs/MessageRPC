using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;

using System.Threading;
using System.Web;

namespace MessageRPC
{


    public delegate SMessage MessageCallback(RMessage m);


    public class Host
    {


        private Socket socket;

        private MessageCallback messageCallback;


        public static Host CreateAndListen(string ip, int port, MessageCallback messageCallback)
        {

            IPAddress ipAddress = IPAddress.Parse(ip);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(ipAddress, port));  //绑定IP地址：端口
            socket.Listen(20000);    //设定最多20000个排队连接请求



            Console.WriteLine("启动监听{0}成功", socket.LocalEndPoint.ToString());


            Host host = new Host(socket, messageCallback);

            Thread thread = new Thread(host.Listen);
            thread.Start();

            return host;
        }

        private Host(Socket socket, MessageCallback messageCallback)
        {
            this.socket = socket;
            this.messageCallback = messageCallback;
        }

        private void Listen()
        {
            Socket clientSocket;

            while(true)
            {
                clientSocket = this.socket.Accept();

                Thread thread = new Thread(Receive);
                thread.Start(clientSocket);
            }
        }

        private void Receive(object clientSocket)
        {
            Socket s = (Socket)clientSocket;

            MessageParser mp = new MessageParser();

            RMessage m;

            SMessage sMsg;

            SMessage errorMsg;

            Exception error;

            while(true)
            {
                try
                {

                    m = null;
                    sMsg = null;
                    errorMsg = null;
                    error = null;



                    m = mp.Parse(s);


                    try
                    {
                        sMsg = OnMessageArrived(m);
                    }
                    catch(Exception e)
                    {
                        error = e;
                    }

                    //  如果 在 OnMessageArrived 中没有读取完 m.Content ，则需要继续读完 ， 
                    //  否则没有读完的内容会被当成下一次 Request 的 Head ， 导致请求错误 。
                    mp.ReadToEnd(m.Content);

                    if (error != null)
                    {
                        errorMsg = new SMessage();
                        errorMsg.Error = error.Message;

                        mp.Send(ref s, errorMsg, null);
                    }
                    else
                    {
                        mp.Send(ref s, sMsg, null);
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    s.Shutdown(SocketShutdown.Both);
                    s.Close();
                    break;
                }

            }
        }

        private SMessage OnMessageArrived(RMessage m) 
        {
            return this.messageCallback(m);
        }

    }
}
