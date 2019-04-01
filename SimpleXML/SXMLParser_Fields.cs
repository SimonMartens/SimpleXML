using System;
using System.Text;

namespace SimpleXML
{
    public abstract partial class SXMLParser
    {
        // Fields and constants to be used in a SXML-Parser

        // Start fields
        // Start constants
        internal const int _defaultBufferSize = 4096 * 2;
        internal const int _hugeBufferSize = 4096 * 4;
        internal const int _maximumBufferSize = 4096 * 8;
        internal const int _maxBytesToMove = 128;
        // End constants

        // Settings
        internal SettingsData _settings;
        internal StateData _state;
        internal Parser _parser;
        // End settings

        // Current Value of String
        internal StringBuilder _sb = new StringBuilder();

        // Exposed fields
        public SXMLOnErrorEvents ErrorEvents = new SXMLOnErrorEvents();
        public SXMLOnManagementEvents MgmtEvents = new SXMLOnManagementEvents();
        public SXMLOnParseEvents ParseEvents = new SXMLOnParseEvents();
        // End exposed fields
        // End Fields

        // Start character-table
        internal static class Characters
        {
            public static readonly uint UTF8BOM = 0xEFBBBF;
            public static readonly uint UTF16LEBOM = 0xFFFE;
            public static readonly uint UTF16BEBOM = 0xFEFF;
            public static readonly uint UTF32LEBOM = 0xFEFF0000;
            public static readonly uint UTF32BEBOM = 0x0000FEFF;
            public static readonly uint UTF16BRACKET = 0x003C;
            public static readonly uint OBRACKET = 0x3C;
        }
        // End character-table
    }
}
