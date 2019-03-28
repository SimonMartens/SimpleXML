using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Xml;
using System.Collections.Generic;

namespace SimpleXML
{
    public class SimpleDoc : IDisposable
    {
        // Start Fields
        // Start Constants
        // Remember to call the WINDOWS Read() API as few times as possible
        private const int _defaultBufferSize = 4096 * 2;
        private const int _hugeBufferSize = 4096 * 4;
        private const int _maximumBufferSize = 4096 * 8;
        private const int _maximumByteSequenceLength = 6; // A read sequence has the maximum meaningful length of 6 bytes (more likely to be <= 4)
        private const int _approxXMLDeclLength = 80; // About the Length of an XML declaration
        private const int _maxBytesToMove = 128;
        // End Constants

        // Settings
        private SettingsData _settings;
        private StateData _state;
        // End Settings
        // End Fields


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
            internal long streamLength;
            internal void clear()
            {
                baseUri = null;
                baseStream = null;
                baseEncoding = null;
                baseDecoder = null;
                fullyBuffered = false;
                streamLength = 0;
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
            

            // Byte buffer
            // increases speed and decreases memory footprint of the parser
            internal byte[] bytes;
            // Bytes converted into char overall
            internal int bytesUsed;
            // Bytes read from stream last time
            internal int bytesRead;
            
            // Handles EOF
            internal bool EOF;

            // Character buffer
            // It's good to use a buffer to increase speed and and minimize the amout of systemcalls neccessary
            internal char[] chars;
            internal int charsUsed;

            internal void clear()
            {
                bytes = null;
                chars = null;
                bytesUsed = 0; 
                charsUsed = 0;
                EOF = false;
            }
            internal void close() => clear();
        }
        // End Custom Data Types


        // Start Constructors
        // 1. Possibility: input of a stream; passes control over memory management to user
        public SimpleDoc(Stream stream) : this((Uri)null, stream) { }

        public SimpleDoc(Uri uri) : this(uri, (Stream)null) { }

        public SimpleDoc(Uri uri, Stream stream) : this(uri, stream, (byte[])null) { }


        // 2. Possibility: Input of a byte array to be used as buffer -- reading is taking place somewhere else
        public SimpleDoc(byte[] bytes) : this((Uri)null, bytes) { }

        public SimpleDoc(Uri uri, byte[] bytes) : this(uri, (Stream)null, bytes) { }
        // End Constructors

        // Start Exposed Methods
        int rounds = 0; // Test Variable
        public void _testRead()
        {
            while (ReadData())
            ;;
        }
        // End Exposed Methods

        // Start Private Constructors
        private SimpleDoc(Uri uri, Stream stream, byte[] buffer)
        {
            if (uri != null)
                this._settings.baseUri = uri;
            if (stream != null)
                this._settings.baseStream = stream;
            else
                if (buffer != null)
                    _state.bytes = buffer;
                else
                    // TO FIX: INTERPRET AS EOF
                    throw new Exceptions.ArgumentNullException("The given Stream is null, there's nothing to read here.");

            _initStream(stream);
        }
        // End Private Constructors


        // Start Initialization Methods
        private void _initStream(Stream stream)
        {
            // First we allocate a buffer. The buffer allocation and Encoding Detection is pretty much
            // the same as in System.Xml.TextReaderImpl, for the Exception of a MemoryStream, which gets
            // handled differently
            int bufferSize = _calculateBufferSize(stream);

            // Allocating byte buffer
            if (_state.bytes == null || _state.bytes.Length < bufferSize)
                _state.bytes = new byte[ bufferSize ];
            // Allocating char buffer
            if (_state.chars == null || _state.chars.Length < bufferSize + 1)
                _state.chars = new char[bufferSize]; // Hier wurde +1 aufgerechnet (why tho?)

            // Getting at least 4 bytes to detect encoding (max. UTF-16)
            while ( _state.bytesRead < 4 && _state.bytes.Length - _state.bytesUsed > 0)
            {
                int read = stream.Read(_state.bytes, _state.bytesRead, 1);
                if (read == 0)
                {
                    _state.EOF = true;
                    break;
                }
                _state.bytesRead += read;
                
            }

            // Detecting and setting the encoding
            Encoding encoding = _detectEncoding();
            _setupEncoding(encoding);

            // Getting the length of the BOM, setting the beginning of the document and read to exclude the BOM
            // Example: UTF BOM is 3 bytes in length.. So we set the byte order mark to be 3
            byte[] preamble = _settings.baseEncoding.GetPreamble();
            int preambleLen = preamble.Length;
            int i;
            for (i = 0; i < preambleLen && i < _state.bytesRead; i++)
            {
                if (_state.bytes[i] != preamble[i])
                {
                    break;
                }
            }
            _settings.documentStartPos = i; 
            GetChars(_settings.documentStartPos);
            ReadData();
        }

        private int _calculateBufferSize(Stream stream)
        {
            int bufferSize = _defaultBufferSize;
            if (stream.CanSeek)
            {
                long len = stream.Length;
                if (len < _maximumBufferSize)
                {
                    bufferSize = checked((int)len);
                }
                else
                {
                    bufferSize = _hugeBufferSize;
                }
            }
            _settings.streamLength = stream.Length;
            return bufferSize;
        }

        // Gets the encoding of the file
        // Like so: https://www.w3.org/TR/encoding/#specification-hooks
        private Encoding _detectEncoding()
        {
            // One byte used means it must be ASCII, and not well formed
            int first2Bytes = (_state.bytesUsed >= 2) ? (_state.bytes[0] << 8 | _state.bytes[1]) : 0;
            int first3Bytes = (_state.bytesUsed >= 3) ? (first2Bytes << 8 | _state.bytes[2]) : 0;
            int first4Bytes = (_state.bytesUsed >= 4) ? (first3Bytes << 8 | _state.bytes[3]) : 0;


            if (first4Bytes == Characters.UTF32LEBOM)
                return new UTF32Encoding(true, false);
            if (first4Bytes == Characters.UTF32BEBOM)
                // There's no Encoding for UTF32 Big Endian by default
                return null;
            if (first3Bytes == Characters.UTF8BOM)
                return new UTF8Encoding(true, false);
            if (first2Bytes == Characters.UTF16LEBOM)
                return new UnicodeEncoding(true, false);
            if (first2Bytes == Characters.UTF16BEBOM || first2Bytes == Characters.UTF16BRACKET)
                return Encoding.BigEndianUnicode;

            // Otherwise take the default ASCII until declaration is found and enjoy the exceptions
            return null;
        }

        // Get Decoders and set the settings
        private void _setupEncoding(Encoding encoding)
        {
            if (encoding == null)
            {
                _settings.baseEncoding = new UTF8Encoding(true, false);
                _settings.baseDecoder = _settings.baseEncoding.GetDecoder();
            }
            else
            {
                _settings.baseEncoding = encoding;
                _settings.baseDecoder = encoding.GetDecoder();
            }
        }
        // End Initialization Methods

        // Start Buffer Allocation & Character Decoding
        private bool ReadData()
        {
            if (!_state.EOF) 
            {
                _state.bytesRead = _settings.baseStream.Read(_state.bytes, 0, _state.bytes.Length);
                GetChars();
                return true;
            } 
            else 
            {
                return false;
            }
        }

        private void GetChars(int offset) => GetChars(_state.bytes, offset);

        private void GetChars() => GetChars(_state.bytes, 0);

        private void GetChars(byte[] from, int offset)
        {
            if (_state.bytesRead != 0)
            {
                int bytesUsed;
                int charsUsed;
                bool completed;
                _settings.baseDecoder.Convert(from, offset,  _state.bytesRead - offset, _state.chars, 0, _state.chars.Length, false, out bytesUsed, out charsUsed, out completed);
                _state.bytesUsed += bytesUsed;
                var hans = new char[charsUsed];
                for (int i = 0; i < charsUsed; i++)
                {
                    hans[i] = _state.chars[i];
                }            
                //File.AppendAllText(@"/home/simon/repos/Hamann/XML_Aktuell/2019-03-07/HAMANN.xml.out", String.Join("", hans));
            }
            else
            {
                _state.EOF = true;
            }
        }
        // End Buffer Allocation & Character Encoding


        // Start Helper Methods

        // End Helper Methods


        // Start IDisposable Implementation
        void IDisposable.Dispose()
        {
            _state.close();
            _settings.close();
        }
        // End IDisposable Implementation
    }
}

