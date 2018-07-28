using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Net;
using System.Net.Sockets;
using System.IO;

using MessageRPC;

namespace Client
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnTest_Click(object sender, EventArgs e)
        {

            try
            {
                RPC rpc = new RPC("127.0.0.1", 9527);

                SMessage m = new SMessage();

                m.Parameters.Add(new Para("id", "001"));
                m.Parameters.Add(new Para("name", "小明"));

                RMessage rMsg;

                using (Stream s = File.Open("timg.jpg", FileMode.Open, FileAccess.Read))
                {
                    m.Content = s;
                    m.ContentLength = s.Length;
                    rMsg = rpc.Send(m);
                }


                WriteMsg(rMsg);

            }
            catch(Exception ex)
            {
                WriteMsg(ex.ToString());
            }

        }

        private void WriteMsg(RMessage m)
        {
            foreach(KeyValuePair<string, string> o in m.Parameters)
            {
                WriteMsg(o.Key + " " + o.Value);
            }
        }

        private void WriteMsg(string msg)
        {
            txtMsg.AppendText(msg + "\r\n");
        }

    }
}
