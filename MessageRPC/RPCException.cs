using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageRPC
{
    public class RPCException : Exception
    {
        internal RPCException(string message) : base(message)
        {

        }
    }
}
