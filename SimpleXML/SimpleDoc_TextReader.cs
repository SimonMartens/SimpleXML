using System;

namespace SimpleXML
{
    public partial class SimpleDoc
    {
        private void _textReader()
        {
            _parseState.state = ParseStates.TextReader;
            _textReaderIncrement();
        }
        
        private void _textReaderIncrement()
        {   
            int count = 0;
            var chars = _bufferState.chars;
            for (;;)
            {
                if (_parseState.charPos + count >= _bufferState.charsRead-1)
                {
                    _textReaderSccessfulFlush(count);
                    if (!_readData())
                        return;
                    else
                        count = 0;
                }
                if (chars[_parseState.charPos + count] == '<')
                {
                    _textReaderSccessfulFlush(count);
                    _parseState.state = ParseStates.TagReader;
                    _parseState.charPos = _parseState.charPos+count;
                    return;
                }
                count++;
            }
        }

        private void _textReaderSccessfulFlush(int count)
        {
            if (count > 0)
            {
                char[] dst = new char[count];
                Buffer.BlockCopy(_bufferState.chars, _parseState.charPos * sizeof(char), dst, 0, count * sizeof(char));
                _parseState.sb.Append(dst);
            }
        }
    }
}
