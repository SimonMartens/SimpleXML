using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Xml;
using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif

namespace SimpleXML
{
    // SimpleXML will be a fast and event-based SAX-Parser for C# Core (forward-only, no DOM). Goals and objectives:
    //      - almost all exceptions are to be avoided. Instead raise lots of events, which can be subscribed to optionally.
    //      - a great level of detail to event raising => ability to parse invalid XML at the same time allowing it's detection and handling
    //      - support for ASCII, UTF-8, UTF-16, UTF-32 character sets
    //      - support for XML 1.0 -- including namespacess
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

        // All other methods are not maintained and are therefore disabled
        // 1. Possibility: input of a stream; passes control over memory management to user
        // public SimpleDoc(Stream stream) : this((Uri)null, stream) { }

        // public SimpleDoc(Uri uri) : this(uri, (Stream)null) { }

        // public SimpleDoc(Uri uri, Stream stream) { this._initDoc(uri, stream, (byte[])null); }


        // // 2. Possibility: Input of a byte array to be used as buffer -- reading is taking place somewhere else
        // public SimpleDoc(byte[] bytes) : this((Uri)null, bytes) { }

        // public SimpleDoc(Uri uri, byte[] bytes) { this._initDoc(uri, (Stream)null, bytes); }
        // End Constructors

        // Start Exposed Methods
        // Pass data if empty constructor was used
        public void Load(Stream stream)
        {
            this._initDoc((Uri)null, stream, (byte[])null);
        }

        public void _testRead()
        {
            while (! _bufferState.EOF)
                _parse();
            //Console.WriteLine(_parseState.sb.ToString());
            //File.AppendAllText(@"/home/simon/repos/Hamann/XML_Aktuell/2019-03-07/HAMANN.xml.out", _parseState.sb.ToString());
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
                    _bufferState.bytes = buffer;
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
            if (_bufferState.bytes == null || _bufferState.bytes.Length < bufferSize)
                _bufferState.bytes = new byte[ bufferSize ];
            // Allocating char buffer
            if (_bufferState.chars == null || _bufferState.chars.Length < bufferSize + 1)
                _bufferState.chars = new char[bufferSize]; // Hier wurde +1 aufgerechnet (why tho?)

            // Getting 4 bytes to detect encoding (max. UTF-32)
            while ( _bufferState.bytesRead < 4 && _bufferState.bytes.Length - _bufferState.bytesRead > 0)
            {
                int read = stream.Read(_bufferState.bytes, _bufferState.bytesRead, 1);
                if (read == 0)
                {
                    _bufferState.EOF = true;
                    break;
                }
                _bufferState.bytesRead += read;
            }

            // Detecting and setting the encoding
            Encoding encoding = _detectEncoding();
            _setupEncoding(encoding);

            // Getting the length of the BOM, setting the beginning of the document and read to exclude the BOM
            // Example: UTF BOM is 3 bytes in length.. So we set the byte order mark to be 3
            byte[] preamble = _settings.baseEncoding.GetPreamble();
            int preambleLen = preamble.Length;
            int i;
            for (i = 0; i < preambleLen && i < _bufferState.bytesRead; i++)
            {
                if (_bufferState.bytes[i] != preamble[i])
                {
                    break;
                }
            }

            // Setting the start of the document and getting the chars for it -- without the BOM, which is not
            // of use now that the enconding detection took place. But we still must parse the characters read
            // above, in case the BOM was less then 4 bytes long. So GetChars() is called here.
            _settings.documentStartPos = i;
            _initParser();
            _getChars(i);
            MgmtEvents.RaiseStartUpComplete();
        }

        private void _initParser() 
        {
            _parseState = new ParseStateData();
            _parseState.clear();

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
                    bufferSize = _maximumBufferSize;
                }
            }
            return bufferSize;
        }

        // Gets the encoding of the file
        // Like so: https://www.w3.org/TR/encoding/#specification-hooks
        private Encoding _detectEncoding()
        {
            // One byte used means it must be ASCII, and not well formed
            int first2Bytes = (_bufferState.bytesRead >= 2) ? (_bufferState.bytes[0] << 8 | _bufferState.bytes[1]) : 0;
            int first3Bytes = (_bufferState.bytesRead >= 3) ? (first2Bytes << 8 | _bufferState.bytes[2]) : 0;
            int first4Bytes = (_bufferState.bytesRead >= 4) ? (first3Bytes << 8 | _bufferState.bytes[3]) : 0;


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
        // Returns false if EOF -- stores the number of read bytes into _bufferState.bytesRead.
        // This is neccessary, so we know how many bytes need conversion in GetChar(), which, at the end
        // of the file differs from the byte-buffer-size.
        // The number of bytes actually read and needing conversion is stored in _bufferState.bytesRead
        private bool _readData()
        {
            if (!_bufferState.EOF) 
            {
                // Read the buffer, offset in bytes[] is 0, fill up the buffer
                _bufferState.bytesRead = _settings.baseStream.Read(_bufferState.bytes, 0, _bufferState.bytes.Length);
                // Reset position of bytes to parse, clean slate, nothing's parsed
                _parseState.bytePos = 0;
                _parseState.bytesUsed = 0;

                _getChars();
                return true;
            } 
            else 
            {
                return false;
            }
        }

        // This method allows for a certain offset, essentially allowing to skip bytes that don't need
        //  conversion into chars. It is used during initialization, since the BOM needs to be skipped.
        private void _getChars(int offset) => _getChars(_bufferState.bytes, offset);

        // Standard call to _getChars with an offset of zero
        private void _getChars() => _getChars(_bufferState.bytes, 0);

        // Converts the bytes to characters and stores the result into the char-buffer.
        // Stores the number of bytes, that were actually used in the conversion.
        // TODO: WHAT ABOUT 4-BYTE-SEQUENCES that get split?
        // Stores the number of chars in the char buffer that are of significance, since the buffer does not get
        // filled up neccessarily, esp during the end of the file.
        // The number of chars that were read and need parsing is stored in _bufferState.charsRead.
        private void _getChars(byte[] from, int offset)
        {
            if (_bufferState.bytesRead != 0)
            {
                int bytesUsed;
                int charsRead;
                bool completed;
                // Convert the bytes (except for the offset) to chars, expect more
                _settings.baseDecoder.Convert(from, offset, _bufferState.bytesRead - offset, _bufferState.chars, 0, _bufferState.chars.Length, false, out bytesUsed, out charsRead, out completed);
                // Amount of meaningful chars from this batch
                _bufferState.charsRead = charsRead;
                // Reset position of characters to parse, clean slate, nothing's parsed
                _parseState.charPos = 0;
                _parseState.charsUsed = 0;

                // For testing purposes:
                //var currtext = SXML_Helpers.Slice<char>(_bufferState.chars, 0, _bufferState.charsRead);
                //_parseState.sb.Append(currtext);
            }
            else
            {
                _bufferState.EOF = true;
            }
        }
        // End Buffer Allocation & Character Encoding

        public void _parse()
        {
            switch (_parseState.state)
            {
                case ParseStates.init:
                    _parseState.state = ParseStates.TextReader;
                    break;
                case ParseStates.TextReader:
                    _textReader();
                    break;
                case ParseStates.TagReader:
                    _tagReader();
                    break;
                default:
                    break;
            }
        }
    }
}

