using PdfUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfUtility
{
    public class PdfString : PdfObject
    {
        public ByteString Text;

        public PdfString(ByteString text)
        {
            Text = text;
        }

        public PdfString(byte[] buffer)
        {
            Text = new ByteString(buffer);
        }

        public override bool Equals(object? obj)
        {
            return obj is PdfString s &&
                   Text == s.Text;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Text);
        }

        public override string ToString()
        {
            return Text.ToString();
        }

    }
}
