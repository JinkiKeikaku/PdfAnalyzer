using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfUtility
{
    /// <summary>
    /// PDFの文字列。通常のstringでもいいかもしれないが値がbyteで[0, 255]のため作った。
    /// </summary>
    public class ByteString
    {

        public ByteString() { }
        /// <summary>
        /// バッファーから作る。バッファーは参照として保持する。
        /// </summary>
        public ByteString(byte[] buf) { mBuffer = buf; }

        /// <summary>
        /// 文字列から作る。ただし、文字列をASCIIに変換するのでASCII範囲外の文字は文字化けする。
        /// </summary
        public ByteString(string text) { mBuffer = Encoding.ASCII.GetBytes(text); }

        /// <summary>
        /// バッファーを返す。
        /// </summary>
        public byte[] Buffer => mBuffer;

        /// <summary>
        /// バッファーの内容をASCII文字列で返す。
        /// </summary>
        public override string ToString()
        {
            return Encoding.ASCII.GetString(mBuffer);
        }

        /// <inheritdoc/>>
        public override bool Equals(object? obj)
        {
            return obj is ByteString bs &&
                bs.mBuffer.SequenceEqual(mBuffer);
        }

        /// <inheritdoc/>>
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

        private byte[] mBuffer = Array.Empty<byte>();
    }
}
