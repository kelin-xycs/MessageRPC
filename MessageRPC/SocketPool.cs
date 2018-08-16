using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MessageRPC
{
    class SocketBag
    {
        public Socket socket;
        public DateTime lastUseTime;

        public SocketBag(Socket socket, DateTime lastUseTime)
        {
            this.socket = socket;
            this.lastUseTime = lastUseTime;
        }
    }

    class SocketPool
    {

        private static Dictionary<IPEndPoint, SocketPool> _dic = new Dictionary<IPEndPoint, SocketPool>();

        //private const int _socketLifeTime = 2;
        //private const int _recycleInterval = 1000;


        private Queue<SocketBag> queue = new Queue<SocketBag>();

        private IPEndPoint ipEndPoint;


        public static SocketPool GetPool(IPEndPoint ipEndPoint)
        {
            SocketPool pool;

            lock(_dic)
            {
                if (_dic.TryGetValue(ipEndPoint, out pool))
                {
                    return pool;
                }

                pool = new SocketPool(ipEndPoint);

                _dic.Add(ipEndPoint, pool);
            }

            return pool;
        }

        private SocketPool(IPEndPoint ipEndPoint)
        {

            this.ipEndPoint = ipEndPoint;


            Thread thread = new Thread(Recycle);

            thread.IsBackground = true;

            thread.Start();
        }

        private void Recycle()
        {
            DateTime d1 = DateTime.Now;
            DateTime d2;

            while (true)
            {
                Thread.Sleep(1000);

                d2 = DateTime.Now;

                //  每 30 秒回收一次
                if ((d2 - d1).TotalSeconds <= 30)
                {
                    continue;
                }

                DoRecycle();

                d1 = DateTime.Now;
            }
        }

        private void DoRecycle()
        {
            SocketBag bag;
            Socket s;

            int count = this.queue.Count;

            for (int i = 0; i < count; i++)
            {

                bag = null;


                lock (this.queue)
                {
                    if (this.queue.Count > 0)
                    {
                        bag = this.queue.Dequeue();
                    }
                }


                if (bag == null)
                    break;

                //  Socket 的 生命期 是 2 分钟，超过 2 分钟 未被使用 就会被回收
                if ((DateTime.Now - bag.lastUseTime).TotalMinutes <= 2)
                {
                    this.queue.Enqueue(bag);
                }
                else
                {
                    s = bag.socket;

                    s.Shutdown(SocketShutdown.Both);
                    s.Close();
                }

                s = null;
                bag = null;
            }
            
        }

        public Socket Get()
        {
            SocketBag bag = null;

            lock(this.queue)
            {
                if (this.queue.Count > 0)
                {
                    bag = this.queue.Dequeue();
                }
            }

            if (bag != null)
            {
                return bag.socket;
            }

            return GetNew();
        }

        public void Return(Socket s)
        {
            SocketBag bag = new SocketBag(s, DateTime.Now);

            lock(this.queue)
            {
                this.queue.Enqueue(bag);
            }
        }

        public void Close(Socket s)
        {
            s.Shutdown(SocketShutdown.Both);
            s.Close();
        }

        public Socket GetNew()
        {

            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.Connect(this.ipEndPoint);

            return s;
        }
    }


}
