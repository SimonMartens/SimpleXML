using System;

namespace SimpleXML
{
    public abstract partial class SXMLParser : IDisposable
    {
        // Methods to be used in all SXML Parsers -- Implementation of IDisposable

        // Clearing of the fields
        public void Close() 
        {
            _state.close();
            _settings.close();
        }

        // Implementation of IDisposable
        public void Dispose() => Close();
    }
}
