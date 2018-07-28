using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageRPC
{
    public class RMessage
    {
        private Dictionary<string, string> parameters = new Dictionary<string, string>();

        public Dictionary<string, string> Parameters
        {
            get { return parameters; }
        }


        private string error;

        public string Error
        {
            get { return error; }
            set { error = value; }
        }


        private ContentStream content;

        public ContentStream Content
        {
            get { return content; }
            internal set { content = value; }
        }


        private long contentLength;

        public long ContentLength
        {
            get { return contentLength; }
            internal set { contentLength = value; }
        }
    }
}
