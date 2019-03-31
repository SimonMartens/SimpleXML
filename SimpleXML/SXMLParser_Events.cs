using System;

namespace SimpleXML
{
    public abstract partial class SXMLParser
    {
        public static class Events {
            // Events to be invoked from a SXML-Parser
            // Methods to invoke these events safely from derived parsers

            // Start event fields
            public static event EventHandler StartUpComplete;
            public static event EventHandler ByteBufferReload;
            public static event EventHandler CharBufferReload;
            // End event fields

            // Start event invoking methods
            public static void RaiseStartUpComplete() { 
                EventHandler startUpComplete = StartUpComplete;
                if (startUpComplete != null)
                    startUpComplete(null, EventArgs.Empty);
            }
            // End event invoking methods
        }
    }
}
