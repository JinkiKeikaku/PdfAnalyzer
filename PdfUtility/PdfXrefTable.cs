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
        public Dictionary<int, (int streamObjectNumber, int offset)> XrefStreamMap { get; } = new();

        private Dictionary<int, (PdfStream stream, byte[]? buffer)> mObjectNumberToStreamMap = new();

        //        private Dictionary<PdfStream, byte[]?> mXrefStreamBufferMap { get; } = new();

        public void Clear()
        {
            XrefMap.Clear();
            XrefStreamMap.Clear();
            mObjectNumberToStreamMap.Clear();
        }

        public bool ContainsKey(int key) => XrefMap.ContainsKey(key) || XrefStreamMap.ContainsKey(key);
        internal bool ContainXrefStreamObject(int objectNumber)
        {
            return mObjectNumberToStreamMap.ContainsKey(objectNumber);
        }

        internal void Add(int key, (long, bool) value) => XrefMap[key] = value;

        internal void Add(int objectNumber, PdfStream pdfStream)
        {
            mObjectNumberToStreamMap[objectNumber] = (pdfStream, null);
        }
        internal void Add(int key, int objectNumber, int offset)
        {
            XrefStreamMap[key] = (objectNumber, offset);
        }

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
            if (!mObjectNumberToStreamMap.ContainsKey(s.streamObjectNumber)) return (null, -1);
            var ps = mObjectNumberToStreamMap[s.streamObjectNumber].stream;
            var eb = mObjectNumberToStreamMap[s.streamObjectNumber].buffer;
            if (eb == null)
            {
                eb = ps.GetExtractedBytes();
                mObjectNumberToStreamMap[s.streamObjectNumber] = (ps, eb);
            }
            if (eb == null) return (null, -1);
            int first = ps.Dictionary.GetInt("/First");
            var ss = Encoding.ASCII.GetString(eb[0..first]).Split();
            var px = int.Parse(ss[s.Item2 * 2 + 1]) + first;
            return (eb, px);
        }
    }
}
