using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Sockets;
using System.Web;

namespace MessageRPC
{
    class MessageParser
    {

        private const int headSize = 1024 + 128;    // 1024 表示 参数字符串 的允许的长度， 128 是 Paramters 和 Content-Length 的预留长度

        private const int sendContentBufferSize = 1024;


        public RMessage Parse(Socket s)
        {

            byte[] b = new byte[headSize];

            int headEndIndex = -1;

            int receiveCount;
            int totalReceiveCount = 0;

            int beginIndex = 0;

            while(true)
            {


                receiveCount = s.Receive(b, totalReceiveCount, b.Length - totalReceiveCount, SocketFlags.None);

                if (receiveCount == 0)
                {
                    throw new RPCException("receiveCount = 0 。 对方主机已断开连接 。");
                }

                totalReceiveCount += receiveCount;


                if (receiveCount > 0)
                {
                    headEndIndex = FindHeadEnd(b, totalReceiveCount, ref beginIndex);
                }



                if (headEndIndex != -1)
                    break;

                if (totalReceiveCount >= b.Length)
                    break;
            }

            

            

            if (headEndIndex == -1)
            {
                throw new RPCException("Bad Request . 找不到 Head 结束符 。");
            }


            string head = Encoding.ASCII.GetString(b, 0, headEndIndex + 1);

            RMessage m = new RMessage();

            ParseHead(m, head);

            if (m.ContentLength > 0)
            {
                ParseContent(m, s, b, headEndIndex, totalReceiveCount);
            }


            return m;
        }

        private int FindHeadEnd(byte[] b, int totalReceiveCount, ref int beginIndex)
        {


            if (totalReceiveCount < 4)
                return -1;

            

            int headEndIndex = -1;

            int i = beginIndex;

            for (; i <= totalReceiveCount - 4; i++)
            {


                if (b[i] == 13 && b[i + 1] == 10 && b[i + 2] == 13 && b[i + 3] == 10)
                {
                    if (i < 1)
                        throw new RPCException("Bad Request . Head 仅包含结束符 ， 未包含有效内容 。");


                    headEndIndex = i - 1;

                    

                    break;
                }

            }

            beginIndex += i;

            return headEndIndex;
        }

        private void ParseHead(RMessage m, string head)
        {
            string[] tokens = head.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            string t;

            string[] tokens2;

            Dictionary<string, string> dic = new Dictionary<string, string>();

            for (int i = 0; i < tokens.Length; i++)
            {
                t = tokens[i];

                tokens2 = t.Split(new char[] { ':' });

                if (tokens2.Length < 2)
                    throw new RPCException("Header 应包含 Name 和 Value 。 Name 和 Value 在 冒号 : 的 两边 。");

                dic.Add(tokens2[0].Trim(), tokens2[1].Trim());
            }


            string p;

            if (dic.TryGetValue("Parameters", out p))
            {
                tokens = p.Split(new char[] { '&' });

                string name;
                string value;

                for (int i = 0; i < tokens.Length; i++)
                {
                    t = tokens[i];

                    if (string.IsNullOrWhiteSpace(t))
                        continue;

                    tokens2 = t.Split(new char[] { '=' });

                    if (tokens2.Length < 2)
                        throw new RPCException("参数应包含 Name 和 Value 。 Name 和 Value 在 等号 = 的 两边 。");

                    name = tokens2[0];
                    value = tokens2[1];

                    name = HttpUtility.UrlDecode(name);

                    if (m.Parameters.ContainsKey(name))
                        throw new Exception("重复的 参数名 ： \"" + name + "\" 。");

                    value = HttpUtility.UrlDecode(value);

                    m.Parameters.Add(name, value);
                }
            }


            string e;

            if (dic.TryGetValue("Error", out e))
            {
                m.Error = HttpUtility.UrlDecode(e);
            }


            string c;

            if (dic.TryGetValue("Content-Length", out c))
            {
                m.ContentLength = long.Parse(c);
            }
        }

        private void ParseContent(RMessage m, Socket s, byte[] b, int headEndIndex, int totalReceiveCount)
        {
            int contentBeginIndex = headEndIndex + 4 + 1;

            byte[] b2 = null;
            int b2Length;

            if (totalReceiveCount > contentBeginIndex)
            {

                b2Length = totalReceiveCount - contentBeginIndex;

                b2 = new byte[b2Length];

                
                for (int i = 0; i < b2.Length; i++)
                {
                    b2[i] = b[contentBeginIndex + i];
                }
                
            }


            m.Content = new ContentStream(s, b2, m.ContentLength);
            
        }

        private byte[] GetBytes(SMessage m)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Parameters: ");

            Para p;
            string name;
            string value;

            for (int i=0; i<m.Parameters.Count; i++)
            {
                p = m.Parameters[i];

                name = HttpUtility.UrlEncode(p.name);
                value = HttpUtility.UrlEncode(p.value);

                sb.Append(name + "=" + value + "&");
            }

            sb.Append("\r\n");

            if (m.Error != null)
            {
                sb.Append("Error: " + HttpUtility.UrlEncode(m.Error));
                sb.Append("\r\n");
            }

            if (m.Content != null)
            {
                if (m.ContentLength <= 0)
                    throw new RPCException("设置了 Content 属性的情况下 ContentLength 应大于 0 。");

                sb.Append("Content-Length: " + m.ContentLength);

                sb.Append("\r\n");
            }

            sb.Append("\r\n");

            string head = sb.ToString();

            return Encoding.ASCII.GetBytes(head);

        }

        public void Send(ref Socket s, SMessage m, SocketPool socketPool)
        {

            byte[] head = GetBytes(m);

            if (head.Length > headSize)
                throw new RPCException("Head 的 长度不能超过 " + headSize + " Byte ， 注意 Header 值会进行 Url Encode ， 中文字符经过 Url Encode 之后会变长 。");




            //  如果 socketPool != null 表示是 客户端 在调用 Send() 方法 ， 发生异常时需要创建新的 Socket 重新 Send()
            //  反之 ， 则表示是 服务器端 在调用 Send() 方法 ， 不需要重新 Send()

            if (socketPool != null)
            {
                //  这里的 try catch 是为了解决 服务器关闭连接 后 客户端 不知道 服务器连接 已关闭 的问题。
                //  客户端仍然从 SocketPool 中取出之前的 Socket 来使用 ， 但因为服务器连接已经关闭，
                //  所以这个 Socket 是不能使用的，用了会报错，所以，这里在 catch 里会关闭已失效的 Socket ，
                //  并且新创建一个 Socket ，和 服务器建立新的连接，用新的 Socket 来 Send() ， 
                //  返回到 RPC.Send() 方法后 ， 这个新的 Socket 会被 Return 到 SocketPool 。
                //  所以这里的 Socket s 参数是 ref 参数 。 就是为了将 新的 Socket 返回到 RPC.Send() 方法 。
                //  正常来讲，服务器不会主动关闭连接，但在一些意外情况，比如服务器意外关闭时，服务器连接会意外关闭 。
                try
                {
                    s.Send(head);
                }
                catch
                {

                    socketPool.Close(s);

                    //  这里要特别先把 s = null; 是因为如果在 SocketPool.GetNew() 中出现异常，
                    //  没有成功的创建 新 Socket 赋值给 s ， 则返回 RPC.Send() 方法后，会在 catch 中去关闭 s ， 
                    //  但这个时候的 s 是在上面已经被关闭的 s ，于是会报“无法访问已释放的对象”的错误 。
                    s = null;

                    s = socketPool.GetNew();

                    s.Send(head);
                    
                }
            }
            else
            {
                s.Send(head);
            }



            if (m.Content == null)
            {
                return;
            }


            byte[] b;

            int bufferSize;

            int sendCount;
            int readCount;

            long totalSendCount = 0;

            while(true)
            {

                bufferSize = totalSendCount + sendContentBufferSize <= m.ContentLength ? 
                    sendContentBufferSize : (int)(m.ContentLength - totalSendCount);


                b = new byte[bufferSize];

                readCount = m.Content.Read(b, 0, bufferSize);
                
                sendCount = s.Send(b, 0, readCount, SocketFlags.None);

                totalSendCount += sendCount;

                if (totalSendCount >= m.ContentLength)
                    break;

            }



            //  如果 Send 的 Content 长度未达到 Content-Length 指定的长度 ， 则发送 空字符 \0 来补齐
            //  直到达到 Content-Length 的长度

            long vacancyLength = m.ContentLength - totalSendCount; 

            if (vacancyLength > 0)
            {
                SendVacancy(s, vacancyLength);
            }
        }

        //  如果 Send 的 Content 长度未达到 Content-Length 指定的长度 ， 则发送 空字符 \0 来补齐
        //  直到达到 Content-Length 的长度
        private void SendVacancy(Socket s, long vacancyLength)
        {
            byte[] b;

            int bufferSize;
            int sendCount;
            long totalSendCount = 0;

            while(true)
            {

                bufferSize = vacancyLength < sendContentBufferSize ? (int)vacancyLength : sendContentBufferSize;

                b = new byte[bufferSize];

                sendCount = s.Send(b);

                totalSendCount += sendCount;

                if (totalSendCount >= vacancyLength)
                    break;
            }

        }

        //  如果 在 OnMessageArrived 中没有读取完 m.Content ，则需要继续读完 ， 
        //  否则没有读完的内容会被当成下一次 Request 的 Head ， 导致请求错误 。
        public void ReadToEnd(ContentStream s)
        {


            if (s.Position == s.Length)
                return;



            byte[] b = new byte[1024];

            int readCount;

            while(true)
            {
                readCount = s.Read(b, 0, b.Length);

                if (readCount == 0)
                    break;
            }
        }
    }
}
