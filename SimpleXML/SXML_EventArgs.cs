using System;
using System.Collections.Generic;

namespace SimpleXML
{
    public abstract class SXML_EventArgs : EventArgs
    {
        public int Line = 0;
        public int Pos = 0;

        public abstract void Raise(SXMLParser parser);
    }

    public class Element : SXML_EventArgs
    {
        public string Name;
        public Dictionary<string, string> Attributes = new Dictionary<string,string>();
        public bool isEmpty = true;
        public HashSet<Element> Ancestors = new HashSet<Element>();
        public TagType Type = TagType.none;

        public override void Raise(SXMLParser parser)
        {
            switch (this.Type)
            {
                case TagType.open:
                    parser.ParseEvents.RaiseOTag(this);
                    break;
                case TagType.close:
                    parser.ParseEvents.RaiseCTag(this);
                    break;
                case TagType.decl:
                    parser.ParseEvents.RaiseDTag(this);
                    break;
            }
        }
    }

    public class Text : SXML_EventArgs
    {
        public string value;

        public override void Raise(SXMLParser parser)
        {

        }
    }

    public class Comment : SXML_EventArgs
    {
        public string value;

        public override void Raise(SXMLParser parser)
        {

        }
    }

    public class Error : SXML_EventArgs
    {
        public ErrorType ErrorType = ErrorType.none;
        public string value;

        public override void Raise(SXMLParser parser)
        {

        }
    }

    public enum ErrorType
    {
        none
    }

    public enum TagType
    {
        none,
        open,
        close,
        decl
    }
}
