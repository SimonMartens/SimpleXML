using System;

namespace SimpleXML
{
    public abstract partial class SXMLParser
    {
        internal static class Exceptions
        {
            public class ArgumentNullException : System.ArgumentNullException
            {
                public ArgumentNullException(string msg) : base(msg) { }
            }

            internal class InvalidCharException : System.Exception
            {
                public InvalidCharException(string msg) : base(msg) { }
            }
        }
    }
}
