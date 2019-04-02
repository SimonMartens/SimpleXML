using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Xml;
using System.Collections.Generic;

namespace SimpleXML
{
    public abstract partial class SXMLParser
    {
        // Data Types to be used in a SXML-Parser
        
        // Start Custom Data Types
        // Handles the initialisation of Data, is a struct to keep it on the stack
        internal struct SettingsData
        {
            internal Uri baseUri;
            internal Stream baseStream;
            internal Encoding baseEncoding;
            internal Decoder baseDecoder;
            internal bool fullyBuffered;
            internal int documentStartPos;
            
            internal void clear()
            {
                baseUri = null;
                baseStream = null;
                baseEncoding = null;
                baseDecoder = null;
                fullyBuffered = false;
                documentStartPos = 0;
            }

            internal void reset() => clear();
            internal void dispose() => clear();
        }

        internal struct ParseStateData
        {
            // Current state & method
            internal ParseStates state;
            internal ParseMethods method;

            // Position mangement
            internal long line;
            internal long position;

            // Data for bitwise parsing
            // _bufferState.bytes[]-Pointer
            // Pointer to beginning of data to parse
            internal int bytePos;
            // Lookahead pointer to end of data to parse
            internal int bytesUsed;

            // Data for charwise parsing
            // _bufferState.chars[]-Pointer
            // Pointer to beginning of data to parse
            internal int charPos;
            // Lookahead pointer to end of data to parse
            internal int charsUsed;

            // Current Value of String
            internal StringBuilder sb;

            internal void clear()
            {
                state = ParseStates.init;
                method = ParseMethods.init;
                bytePos = 0;
                bytesUsed = 0;
                charPos = 0;
                charsUsed = 0;
                sb = new StringBuilder();
            }

            internal void reset() => clear();
            internal void dispose() => clear();
        }

        // Handles a simple state, such as parsing position, is a struct to keep it on the stack
        internal struct BufferStateData
        {
            // Byte buffer
            // increases speed and decreases memory footprint of the parser
            internal byte[] bytes;
            // Bytes read from stream last time
            internal int bytesRead;

            // Character buffer
            // It's good to use a buffer to increase speed and and minimize the amout of systemcalls neccessary
            internal char[] chars;
            // Chars read from bytes last time
            internal int charsRead;

            // Handles EOF
            internal bool EOF;

            internal void clear()
            {
                bytes = null;
                chars = null;
                bytesRead = 0;
                charsRead = 0;
                EOF = false;
            }

            internal void reset() => clear();
            internal void dispose() => clear();
        }

        internal enum ParseStates {
            init,
            fallback,
            TagReader,
            TextReader
        }

        internal enum ParseMethods {
            init,
            bitwise,
            charwise
        }
        // End Custom Data Types
    }
}
