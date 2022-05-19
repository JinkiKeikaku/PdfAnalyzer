using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfUtility
{
    class PdfStream : PdfObject
    {
        public PdfDictionary Dictionary;
        public byte[] Data;

        public PdfStream(PdfDictionary dictionary, byte[] data)
        {
            Dictionary = dictionary;
            Data = data;
        }

        public byte[]? GetExtractedBytes()
        {
            var filter = Dictionary.GetValue<PdfObject>("/Filter");
            if (filter == null) return Data;
            if(filter is not PdfArray fs) {
                fs = new PdfArray();
                fs.Add(filter);
            }
            foreach(var f in fs)
            {
                if (f is PdfName ff)
                {
                    switch (ff.Name)
                    {
                        case null: return Data;
                        case "/FlateDecode":
                            var s = new MemoryStream(Data);
                            var z = new ZLibStream(s, CompressionMode.Decompress);
                            var m = new MemoryStream();
                            z.CopyTo(m);
                            return m.ToArray();
                        default:
                            return null;
                    }
                }
            }
            return null;
        }
    }
}
