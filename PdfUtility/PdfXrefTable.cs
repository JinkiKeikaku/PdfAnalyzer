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
        public Dictionary<int, (byte[], int)> XrefStreamMap { get; } = new();

        public void Clear()
        {
            XrefMap.Clear();
            XrefStreamMap.Clear();
        }

        public bool ContainsKey(int key)=> XrefMap.ContainsKey(key) || XrefStreamMap.ContainsKey(key);
        internal void Add(int key, (long, bool) value) => XrefMap[key] = value;
        internal void Add(int key, (byte[] buffer, int pos) streamBuf) => XrefStreamMap[key] = streamBuf;

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
            return XrefStreamMap[key];
        }
    }
}
