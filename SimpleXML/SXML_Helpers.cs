using System;

namespace SimpleXML
{
    public static class SXML_Helpers
    {
        // Slices Arrays, returning only a part of the array
        // To be replaced by Buffer.BlockCopy()
        public static T[] Slice<T>(this T[] source, int start, int end)
        {
            // Handles negative ends.
            if (end < 0)
            {
                end = source.Length + end;
            }
            int len = end - start;

            // Return new array.
            T[] res = new T[len];
            for (int i = 0; i < len; i++)
            {
                res[i] = source[i + start];
            }
            return res;
        }


        // To beplaced with Buffer.BlockCopy()
        public static unsafe char[] Copy(char[] source, int offset, int end)
        {
            var len = end-offset;
            char[] target = new char[len];

            // If either array is not instantiated, you cannot complete the copy.
            if ((source == null) || (target == null))
            {
                throw new System.ArgumentException();
            }

            // If either offset, or the number of bytes to copy, is negative, you
            // cannot complete the copy.
            if ((offset < 0) || (len < 0))
            {
                throw new System.ArgumentException();
            }

            // The following fixed statement pins the location of the source and
            // target objects in memory so that they will not be moved by garbage
            // collection.
            
            fixed (char* pSource = source, pTarget = target)
            {
                // Copy the specified number of bytes from source to target.
                for (int i = 0; i < len; i++)
                {
                    pTarget[i] = pSource[offset + i];
                }
            }

            return target;
        }

    }
}
