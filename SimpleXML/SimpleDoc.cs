using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Xml;

namespace SimpleXML
{
    public class SimpleDoc : IDisposable
    {
        // Start Fields
        // Start Constants
        // Remember to call the WINDOWS Read() API as few times as possible
        private const int _defaultBufferSize = 4096;
        private const int _hugeBufferSize = 4096 * 2;
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
            internal int _currLinePos;
            internal int _currLine;
            internal int _currCharPos;
            internal int lineStartPos;

            // Byte buffer
            internal byte[] bytes;
            internal int bytePos;
            internal int bytesUsed;

            // Handles EOF
            internal bool EOF;
            internal bool _isStreamEof;

            // Character buffer
            // It's good to use a buffer to increase speed and and minimize the amout of systemcalls neccessary
            internal char[] chars;
            internal int charPos;
            internal int charsUsed;
            // Def. above -- TODO: copy where it' most often needed -- merge structs: internal Encoding encoding;
            // What is this?: internal bool appendMode;

            internal void clear()
            {
                _currLinePos = 0;
                _currLine = 0;
                _currCharPos = 0;
                lineStartPos = 0;
                bytes = null;
                bytePos = 0;
                bytesUsed = 0; // Bytes already read from the stream
                bytesUsed = 0; // Bytes already read from the stream
                EOF = false;
                _isStreamEof = false;
                chars = null;
                charPos = 0;
                charsUsed = 0;
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
        public void _testRead()
        {

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
            // handled differently
            int bufferSize = _calculateBufferSize(stream);

            // Allocating byte buffer
            if (_state.bytes == null || _state.bytes.Length < bufferSize)
                _state.bytes = new byte[ bufferSize ];
            // Allocating char buffer
            if (_state.chars == null || _state.chars.Length < bufferSize + 1)
                _state.chars = new char[bufferSize + 1];

            // Getting at least 4 bytes to detect encoding (max. UTF-16)
            _state.bytePos = 0;
            while ( _state.bytesUsed < 4 && _state.bytes.Length - _state.bytesUsed > 0)
            {
                int read = stream.Read(_state.bytes, _state.bytesUsed, _state.bytes.Length - _state.bytesUsed);
                if (read == 0)
                {
                    _state.EOF = true;
                    break;
                }
                _state.bytesUsed += read;
            }

            // Detecting and setting the encoding
            Encoding encoding = _detectEncoding();
            _setupEncoding(encoding);

            // Getting the length of the BOM, setting the beginning of the document and read to exclude the BOM
            byte[] preamble = _settings.baseEncoding.GetPreamble();
            int preambleLen = preamble.Length;
            int i;
            for (i = 0; i < preambleLen && i < _state.bytesUsed; i++)
            {
                if (_state.bytes[i] != preamble[i])
                {
                    break;
                }
            }
            if (i == preambleLen)
            {
                _state.bytePos = preambleLen;
            }
            _settings.documentStartPos = _state.bytePos;
        }

        private int _calculateBufferSize(Stream stream)
        {
            int bufferSize = _defaultBufferSize;
            if (stream.CanSeek)
            {
                long len = stream.Length;
                if (len < _maximumBufferSize)
                {
                    return checked((int)len);
                }
                else
                {
                    return _hugeBufferSize;
                }
            }
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
                _settings.baseDecoder = new SafeAsciiDecoder();
            }
            else
            {
                _settings.baseEncoding = encoding;
                _settings.baseDecoder = encoding.GetDecoder();
            }
        }
        // End Initialization Methods


        // Start Buffer Allocation & Character Decoding
        // This is the Method copied form XmlTextReaderImpl.
        // I kept it here for archiving Purposes.
        private int ReadData()
        {
            if (_state.EOF == false) {
                return 0;
            }


            return 0;
        }

        private void InvalidCharRecovery(ref int bytesCount, out int charsCount)
        {
            int charsDecoded = 0;
            int bytesDecoded = 0;
            try
            {
                while (bytesDecoded < bytesCount)
                {
                    int chDec;
                    int bDec;
                    bool completed;
                    _settings.baseDecoder.Convert(_state.bytes, _state.bytePos + bytesDecoded, 1, _state.chars, _state.charsUsed + charsDecoded, 1, false, out bDec, out chDec, out completed);
                    charsDecoded += chDec;
                    bytesDecoded += bDec;
                }
            }
            catch (ArgumentException)
            {
            }

            if (charsDecoded == 0)
            {
                throw new Exceptions.InvalidCharException("Konnte keine Zeichen lesen.");
            }
            charsCount = charsDecoded;
            bytesCount = bytesDecoded;
        }
        // End Buffer Allocation & Character Encoding


        // Start Helper Methods
        // Copied from XmlTextReaderImpl --> do we need it?
        internal class SafeAsciiDecoder : Decoder
        {

            public SafeAsciiDecoder() { }

            public override int GetCharCount(byte[] bytes, int index, int count) => count;

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
            {
                int i = byteIndex;
                int j = charIndex;
                while (i < byteIndex + byteCount)
                {
                    chars[j++] = (char)bytes[i++];
                }
                return byteCount;
            }
        }

        internal static void BlockCopyChars(char[] src, int srcOffset, char[] dst, int dstOffset, int count)
            => Buffer.BlockCopy(src, srcOffset * sizeof(char), dst, dstOffset * sizeof(char), count * sizeof(char));

        internal static void BlockCopy(byte[] src, int srcOffset, byte[] dst, int dstOffset, int count)
            => Buffer.BlockCopy(src, srcOffset, dst, dstOffset, count);
        // End Helper Methods


        // Start IDisosable Implementation
        void IDisposable.Dispose()
        {
            _state.close();
            _settings.close();
        }
        // End IDisposable Implementation
    }
}

