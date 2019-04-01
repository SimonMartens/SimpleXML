using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Xml;
using System.Collections.Generic;

namespace SimpleXML
{
    // SimpleXML will be a fast and event-based SAX-Parser for C# Core (forward-only, no DOM). Goals and objectives:
    //      - almost all exceptions are to be avoided. Instead raise lots of events, which can be subscribed to optionally.
    //      - a great level of detail to event raising => ability to parse invalid XML at the same time allowing it's detection and handling
    //      - support for ASCII, UTF-8, UTF-16, UTF-32 character sets
    //      - support for XML 1.0
    //      - xml schema validation
    //      - keep it fast and the memory footprint of the parser to 16 (or 32, with complete file in buffer) kB max. when operating with streams

    // No links will be followed for security reasons.
    // Done:
    //  - input and buffering via stream
    //  - testing of speed & event-based interface
    public partial class SimpleDoc : SXMLParser, IDisposable
    {
        // All fields and data types are declared in the base class, SXMLParser

        // Start Constructors
        // 0. Possibility: no input - preferred. It ist possible to subscribe to events before initializing
        //                 to catch all initialization errors; load date with the public Load() method.
        public SimpleDoc() { }

        // 1. Possibility: input of a stream; passes control over memory management to user
        public SimpleDoc(Stream stream) : this((Uri)null, stream) { }

        public SimpleDoc(Uri uri) : this(uri, (Stream)null) { }

        public SimpleDoc(Uri uri, Stream stream) { this._initDoc(uri, stream, (byte[])null); }


        // 2. Possibility: Input of a byte array to be used as buffer -- reading is taking place somewhere else
        public SimpleDoc(byte[] bytes) : this((Uri)null, bytes) { }

        public SimpleDoc(Uri uri, byte[] bytes) { this._initDoc(uri, (Stream)null, bytes); }
        // End Constructors

        // Start Exposed Methods
        // Pass data if empty constructor was used
        public void Load(Stream stream)
        {
            this._initDoc((Uri)null, stream, (byte[])null);
        }   

        private Element _el = new Element();
        public void _testRead()
        {
            for (;;)
            {
                if (!_state.EOF)
                {
                    if (_state.chars[_state.charToParse] == '<')
                    {
                        _increment();
                        if (_state.chars[_state.charToParse] == '/') 
                        {
                            _increment();
                            while (_state.chars[_state.charToParse] != ' ' && _state.chars[_state.charToParse] != '>')
                            {
                                _sb.Append(_state.chars[_state.charToParse]);
                                _increment();
                            }
                            _el.Type = TagType.close;
                            _sbFlush(_el);
                        }
                        else if (_state.chars[_state.charToParse] == '?')
                        {
                        }
                        else if (_state.chars[_state.charToParse] == '!')
                        {
                        }
                        else
                        {   
                            while (_state.chars[_state.charToParse] != ' ' && _state.chars[_state.charToParse] != '>')
                            {
                                _sb.Append(_state.chars[_state.charToParse]);
                                _increment();
                            }
                            _el.Type = TagType.open;
                            _sbFlush(_el);
                        }
                    }
                    _increment();
                }
                else
                {
                    break;
                }
            }
        }
        // End Exposed Methods

        // Start Initialization Methods
        private void _initDoc(Uri uri, Stream stream, byte[] buffer)
        {
            // End Initialization of event engine
            
            if (uri != null)
                this._settings.baseUri = uri;
            if (stream != null)
                this._settings.baseStream = stream;
            else
                if (buffer != null)
                    _state.bytes = buffer;
                else
                    // TO FIX: INTERPRET AS INSTANT EOF
                    throw new Exceptions.ArgumentNullException("The given Stream is null, there's nothing to read here.");

            _initStream(stream);
        }

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

            // Getting 4 bytes to detect encoding (max. UTF-32)
            while ( _state.bytesRead < 4 && _state.bytes.Length - _state.bytesRead > 0)
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

            // Setting the start of the document and getting the chars for it -- without the BOM, which is not
            // of use now that the enconding detection took place. But we still must parse the characters read
            // above, in case the BOM was less then 4 bytes long. So GetChars() is called here.
            _settings.documentStartPos = i;
            MgmtEvents.RaiseStartUpComplete();
            GetChars(_settings.documentStartPos);
            _testRead();
        }

        // Method for calculating the buffer size; adjustong to stream length im neccessary
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
            return bufferSize;
        }

        // Gets the encoding of the file
        // Like so: https://www.w3.org/TR/encoding/#specification-hooks
        private Encoding _detectEncoding()
        {
            // One byte used means it must be ASCII, and not well formed
            int first2Bytes = (_state.bytesRead >= 2) ? (_state.bytes[0] << 8 | _state.bytes[1]) : 0;
            int first3Bytes = (_state.bytesRead >= 3) ? (first2Bytes << 8 | _state.bytes[2]) : 0;
            int first4Bytes = (_state.bytesRead >= 4) ? (first3Bytes << 8 | _state.bytes[3]) : 0;


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
                // As defined in the XML Spec, UTF-8 is standard
                _settings.baseEncoding = new UTF8Encoding(true, false);
                _settings.baseDecoder = _settings.baseEncoding.GetDecoder();
            }
            else
            {
                // Get decoder, if an encoding was detected
                _settings.baseEncoding = encoding;
                _settings.baseDecoder = encoding.GetDecoder();
            }
        }
        // End Initialization Methods

        // Start Buffer Allocation & Character Decoding
        // This method copies data into the buffer.
        // Returns false if EOF -- stores the number of read bytes into _state.bytesRead.
        // This is neccessary, so we know how many bytes need conversion in GetChar(), which, at the end
        // of the file differs from the byte-buffer-size.
        // The number of bytes actually read and needing conversion is stored in _state.bytesRead
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

        // This method allows for a certain offset, essentially allowing to skip bytes that don't need
        //  conversion into chars. It is used during initialization, since the BOM needs to be skipped.
        private void GetChars(int offset) => GetChars(_state.bytes, offset);

        private void GetChars() => GetChars(_state.bytes, 0);

        // Converts the bytes to characters and stores the result into the char-buffer.
        // Stores the number of bytes, that were actually used in the conversion.
        // TODO: WHAT ABOUT 4-BYTE-SEQUENCES that get split?
        // Stores the number of chars in the char buffer that are of significance, since the buffer does not get
        // filled up neccessarily, esp during the end of the file.
        // The number of chars that were read and need parsing is stored in _state.charsRead.
        private void GetChars(byte[] from, int offset)
        {
            if (_state.bytesRead != 0)
            {
                int bytesUsed;
                int charsRead;
                bool completed;
                _settings.baseDecoder.Convert(from, offset, _state.bytesRead - offset, _state.chars, 0, _state.chars.Length, false, out bytesUsed, out charsRead, out completed);
                // Amount of already converted bytes
                _state.bytesUsed += bytesUsed;
                // Amount of meaningful chars from this batch
                _state.charsRead = charsRead;
                // Reset position of character to-parse
                _state.charToParse = 0;            }
            else
            {
                _state.EOF = true;
            }
        }
        // End Buffer Allocation & Character Encoding

        private SXML_EventArgs _sbFlush(SXML_EventArgs arg)
        {
            if (arg is Element)
            {
                var elem = arg as Element;
                elem.Name = _sb.ToString();
                _sb.Clear();
            }
            arg.Raise(this);
            return arg;
        }

        private void _increment()
        {
            if (_state.charToParse == _state.charsRead)
            {
                ReadData();
            }
            else
            {
                ++_state.charToParse;
            }
        }
    }
}

