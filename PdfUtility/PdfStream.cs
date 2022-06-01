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
            if (filter is not PdfArray fs)
            {
                fs = new PdfArray();
                fs.Add(filter);
            }
            foreach (var f in fs)
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
                            var predcNo = decodeParms?.GetValue<PdfNumber>("/Predictor")?.IntValue ?? 1;
                            if (predcNo == 1) return m.ToArray();
                            if (predcNo >= 10)
                            {
                                var colorsVal = decodeParms?.GetValue<PdfNumber>("/Colors");
                                var colors = colorsVal?.IntValue ?? 1;
                                var columnsVal = decodeParms?.GetValue<PdfNumber>("/Columns");
                                if (columnsVal == null) throw new Exception("/FlateDecode PNG predictor no columns.");
                                var numColumn = columnsVal.IntValue * colors;
                                var src = m.ToArray();
                                return PngPredictor.Predict(src, numColumn, colors);
                            }
                            Debug.WriteLine("PdfStream extracted /FlateDecode predictor != 12 not support");
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
