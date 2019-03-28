using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleXML
{
    internal static class Characters
    {
        // Character-Table
        public static readonly uint UTF8BOM = 0xEFBBBF;
        public static readonly uint UTF16LEBOM = 0xFFFE;
        public static readonly uint UTF16BEBOM = 0xFEFF;
        public static readonly uint UTF32LEBOM = 0xFEFF0000;
        public static readonly uint UTF32BEBOM = 0x0000FEFF;

        public static readonly uint UTF16BRACKET = 0x003C;
        public static readonly uint OBRACKET = 0x3C;
    }
}
