using System;

namespace SimpleXML
{
    public abstract partial class SXMLParser
    {
        // Fields and constants to be used in a SXML-Parser

        // Start Fields
        // Start Constants
        // Remember to call the WINDOWS Read() API as few times as possible
        internal const int _defaultBufferSize = 4096 * 2;
        internal const int _hugeBufferSize = 4096 * 4;
        internal const int _maximumBufferSize = 4096 * 8;
        internal const int _maximumByteSequenceLength = 6; // A read sequence has the maximum meaningful length of 6 bytes (more likely to be <= 4)
        internal const int _approxXMLDeclLength = 80; // About the Length of an XML declaration
        internal const int _maxBytesToMove = 128;
        // End Constants

        // Settings
        internal SettingsData _settings;
        internal StateData _state;
        internal Parser _parser;
        // End Settings
        // End Fields

        // Character-Table
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
    }
}
