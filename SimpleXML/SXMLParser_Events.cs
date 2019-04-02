using System;

namespace SimpleXML
{
    public abstract class SXMLEvents 
    {
        // Events to be invoked from a SXML-Parser
        // Plus methods to invoke these events safely from derived parsers
        // There are three derived classes:
        //  - SXMLOnErrorEvents: are called on recoverable errors and malformed XML
        //  - SXMLOnManagementEvents: are called in management-related functions, lik initialization and buffering
        //  - SXMLOnParseEvents: are events called in parsing methods

        // Shortcut method to safely call an event
        internal Action<EventHandler, EventArgs> Invoke = (e, ea) => {
            EventHandler y = e;
            if (y != null)
                y.Invoke(null, ea);
        };
    }

    public class SXMLOnErrorEvents : SXMLEvents
    {

    }

    public class SXMLOnManagementEvents : SXMLEvents
    {
        // Start event fields
        public event EventHandler ByteBufferReload;
        public event EventHandler CharBufferReload;
        public event EventHandler StartUpComplete;
        // End event fields

        // Start event invoking methods
        public void RaiseStartUpComplete() => Invoke(StartUpComplete, EventArgs.Empty);
        // End event invoking methods
    }

    public class SXMLOnParseEvents : SXMLEvents
    {
        public event EventHandler OTag;
        public event EventHandler CTag;
        public event EventHandler DTag;
        public event EventHandler Text;
        public event EventHandler Elem;
        public event EventHandler ETag;

        public void RaiseOTag(EventArgs ea) => Invoke(OTag, ea);
        public void RaiseCTag(EventArgs ea) => Invoke(CTag, ea);
        public void RaiseDTag(EventArgs ea) => Invoke(DTag, ea);
        public void RaiseETag(EventArgs ea) => Invoke(ETag, ea);
        public void RaiseText(EventArgs ea) => Invoke(Text, ea);
        public void RaiseElem(EventArgs ea) => Invoke(Elem, ea);
    }
}
