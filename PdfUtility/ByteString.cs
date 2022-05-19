using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfUtility
{
    public class ByteString
    {
        byte[] mBuffer = Array.Empty<byte>();
        public byte[] Buffer => mBuffer;

        public ByteString() { }
        public ByteString(byte[] buf) { mBuffer = buf; }
        public ByteString(string text) { mBuffer = Encoding.ASCII.GetBytes(text); }

        public override string ToString()
        {
            return Encoding.ASCII.GetString(mBuffer);
        }

        public override bool Equals(object? obj)
        {
            return obj is ByteString bs &&
                bs.mBuffer.SequenceEqual(mBuffer);
        }

        public override int GetHashCode()
        {
            int n = mBuffer.Length;
            if (n == 0) return 0;
            return HashCode.Combine(n, mBuffer[0], mBuffer[n - 1], mBuffer[n / 2]);
        }

        public static bool operator ==(ByteString bs, string s)
        {
            return bs.ToString() == s;
        }
        public static bool operator !=(ByteString bs, string s)
        {
            return bs.ToString() != s;
        }


    }
}
