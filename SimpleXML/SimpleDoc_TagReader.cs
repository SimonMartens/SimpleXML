using System;

namespace SimpleXML
{
    public partial class SimpleDoc
    {
        private void _tagReader()
        {
            _parseState.state = ParseStates.TagReader;
            _tagReaderIncrement();
        }

        private void _tagReaderIncrement()
        {
            var chars = _bufferState.chars;
            int count = 0;
            for (;;)
            {
                if (_parseState.charPos + count >= _bufferState.charsRead-1)
                {
                    _tagReaderSuccessfulFlush();
                    if(!_readData())
                        return;
                }
                if (chars[_parseState.charPos + count] == '>')
                {
                    _tagReaderSuccessfulFlush();
                    count++;
                    _parseState.charPos = _parseState.charPos + count;
                    _parseState.state = ParseStates.TextReader;
                    return;
                }
                count++;
            }
        }

        private void _tagReaderSuccessfulFlush()
        {

        }
    }
}
