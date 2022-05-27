using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfUtility
{
    public class PdfStream : PdfObject
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
            var filter = Dictionary.GetValue("/Filter");
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
                            var decodeParms = Dictionary.GetValue<PdfDictionary>("/DecodeParms");
                            var pred = decodeParms?.GetValue<PdfNumber>("/Predictor");
                            if (pred == null || pred.IntValue == 1) return m.ToArray();
                            var predcNo = pred.IntValue;
                            if (predcNo >= 10)
                            {
                                var columns = decodeParms?.GetValue<PdfNumber>("/Columns");
                                if (columns == null) throw new Exception("/FlateDecode PNG predictor no columns.");
                                var c = columns.IntValue;
                                var buf = m.ToArray();
                                var n = buf.Length / (c + 1);
                                var ret = new byte[n * c];
                                Array.Copy(buf, 1, ret, 0, c);
                                var k1 = 0;
                                var k2 = c;
                                var k3 = c+1;
                                for (var j = 1; j < n; j++) {
                                    if (buf[k3] == 2)
                                    {
                                        k3++;
                                        for (var i = 0; i < c; i++)
                                        {
                                            ret[k2] = (byte)(ret[k1] + buf[k3]);
                                            k1++;
                                            k2++;
                                            k3++;
                                        }
                                    }
                                    else
                                    {
                                        throw new Exception("/FlateDecode PNG predictor only png predictor 2.");
                                    }
                                }
                                return ret;
                            }
                            Debug.WriteLine("PdfStream extracted /FlateDecode predictor < 10 not support");
                            return m.ToArray();
                        default:
                            return null;
                    }
                }
            }
            return null;
        }

        public override string ToString()
        {
            return $"Stream Length={Data.Length}";
        }
    }
}
