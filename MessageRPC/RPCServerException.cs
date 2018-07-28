using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageRPC
{
    public class RPCServerException : Exception
    {
        internal RPCServerException(string message) : base(message)
        {

        }
    }
}
