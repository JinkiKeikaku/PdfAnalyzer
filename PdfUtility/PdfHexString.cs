using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfUtility
{
    public class PdfHexString : PdfObject
    {
        public byte[] Bytes;

        public PdfHexString(string hexString)
        {
            var bs = new List<byte>();
            for (int i = 0; i < hexString.Length / 2; i++)
            {
                bs.Add(Convert.ToByte(hexString.Substring(i * 2, 2), 16));
            }
            Bytes = bs.ToArray();
        }

        public int ConvertToInt()
        {
            var cs = 0;
            foreach (var b in Bytes) { cs = (cs << 8) + b; }
//            if ((Bytes.Length & 1) != 0) cs = cs << 8;
            return cs;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append('<');
            sb.Append(BitConverter.ToString(Bytes));
            sb.Append('>');
            return sb.ToString();
        }
    }
}
