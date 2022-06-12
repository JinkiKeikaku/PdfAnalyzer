using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PdfUtility.PdfTokenizer;

namespace PdfUtility
{
    class PdfXrefTable
    {
        public Dictionary<int, (long pos, bool valid)> XrefMap { get; } = new();
        public Dictionary<int, (PdfStream, int)> XrefStreamMap { get; } = new();

        private Dictionary<PdfStream, byte[]?> mXrefStreamBufferMap { get; } = new();

        public void Clear()
        {
            XrefMap.Clear();
            XrefStreamMap.Clear();
        }

        public bool ContainsKey(int key)=> XrefMap.ContainsKey(key) || XrefStreamMap.ContainsKey(key);
        internal void Add(int key, (long, bool) value) => XrefMap[key] = value;
        internal void Add(int key, (PdfStream strm, int pos) streamPos) => XrefStreamMap[key] = streamPos;

        internal long GetStreamPosition(int key)
        {
            if (!XrefMap.ContainsKey(key)) return -1;
            var v = XrefMap[key];
            if (!v.valid) throw new Exception("Reference is invalid(f).");
            return v.pos;
        }

        internal (byte[]?, int) GetObjectStreamBuffer(int key)
        {
            if (!XrefStreamMap.ContainsKey(key)) return (null, -1);
            var s = XrefStreamMap[key];
            var ps = s.Item1; 
            byte[]? eb;
            if (mXrefStreamBufferMap.ContainsKey(ps))
            {
                eb = mXrefStreamBufferMap[ps];
            }
            else
            {
                eb = ps.GetExtractedBytes();
                mXrefStreamBufferMap[ps] = eb;
            }
            if (eb == null) return (null, -1);
 //           int n = ps.Dictionary.GetInt("/N");
            int first = ps.Dictionary.GetInt("/First");
            var ss = Encoding.ASCII.GetString(eb[0..first]).Split();
            var px = int.Parse(ss[s.Item2 * 2 + 1]) + first;
            return (eb, px);
        }
    }
}
