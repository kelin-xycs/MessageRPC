using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace MessageRPC
{
    public class Para
    {
        public string name;
        public string value;

        public Para(string name, string value)
        {
            this.name = name;
            this.value = value;
        }
    }

    public class SMessage
    {

        private List<Para> parameters = new List<Para>();

        public List<Para> Parameters
        {
            get { return parameters; }
        }


        private string error;

        public string Error
        {
            get { return error; }
            set { error = value; }
        }


        private Stream content;

        public Stream Content
        {
            get { return content; }
            set { content = value; }
        }


        private long contentLength;
        
        public long ContentLength
        {
            get { return contentLength; }
            set { contentLength = value; }
        }
    }
}
