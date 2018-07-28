using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using MessageRPC;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {

            Host host = Host.CreateAndListen("127.0.0.1", 9527, ProcessMessage);

        }

        static SMessage ProcessMessage(RMessage m)
        {
            
            Console.WriteLine("id = " + m.Parameters["id"]);
            Console.WriteLine("name = " + m.Parameters["name"]);

            using(FileStream s = File.Create("山水.jpg"))
            {
                m.Content.CopyTo(s);
            }

            SMessage sMsg = new SMessage();

            sMsg.Parameters.Add(new Para("Result", "Success"));

            return sMsg;
        }
    }
}
