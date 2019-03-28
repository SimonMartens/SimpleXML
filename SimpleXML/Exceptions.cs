using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleXML.Exceptions
{
    class ArgumentNullException : System.ArgumentNullException
    {
        public ArgumentNullException(string msg) : base(msg) { }
    }

    class InvalidCharException : System.Exception
    {
        public InvalidCharException(string msg) : base(msg) { }
    }
}
