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

            internal void close()
            {
                baseStream.Close();
                clear();
            }
        }

        // Handles a simple state, such as parsing position, is a struct to keep it on the stack
        internal struct StateData
        {
            // Position mangement
            long line;
            long position;

            // Active Parser
            internal Parser ActiveParser;

            // Byte buffer
            // increases speed and decreases memory footprint of the parser
            internal byte[] bytes;
            // Bytes converted into char overall
            internal int bytesUsed;
            // Bytes read from stream last time
            internal int bytesRead;

            // Character buffer
            // It's good to use a buffer to increase speed and and minimize the amout of systemcalls neccessary
            internal char[] chars;
            // Number of chars already parsed
            internal int charsUsed;
            // Chars read from bytes last time
            internal int charsRead;
            // Pointer to char currently toParse
            internal int charToParse;

            // Handles EOF
            internal bool EOF;

            internal void clear()
            {
                bytes = null;
                chars = null;
                bytesUsed = 0; 
                charsUsed = 0;
                bytesRead = 0;
                charsRead = 0;
                EOF = false;
            }

            internal void close() => clear();
        }

        internal class Parser
        {
            private LinkedList<string> _openNodes = new LinkedList<string>();
            private int _noNodes = 0;

            internal LinkedList<string> openNodes
            {
                get => _openNodes;
                set => _openNodes = value;
            }
            
            internal int noNodes
            {
                get => _noNodes;
                set => _noNodes = value;
            }

            internal Parser Pass(Parser toPassTo) {
                toPassTo.noNodes = this.noNodes;
                toPassTo.openNodes = this.openNodes;
                return toPassTo;
            }

            internal void Parse() 
            {
                
            }
        }
        // End Custom Data Types
    }
}
