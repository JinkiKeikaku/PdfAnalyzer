using PdfUtility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfUtility
{
    public class PdfFont
    {
        public string Name;
        public PdfDictionary FontDict;
        public PdfRectangle FontBBox = new(0, 0, 1000, 1000);
        private Dictionary<int, Char> mCMap = new();
        private Dictionary<int, string> mDifferences = new();

        //FontDescriptionにCapHeighしかないこともある。
        public double Ascent = 1000.0;
        public double CapHeight = 733.0;
        public double Descent = 0.0;

        static Dictionary<string, string> mNameToSpecialChar = new()
        {
            {"/fl", "fl" },
            {"/f_l", "fl" },
            {"/fi", "fi" },
            {"/f_i", "fi" },
            {"/ff", "ff" },
            {"/f_f", "ff" },
            {"/ffi", "ffi" },
            {"/ffl", "ffl" },
            {"/ft", "ft" },
            {"/st", "st" },
            {"/emdash", "-"}
        };


        public PdfFont(string name, PdfDictionary fontDict)
        {
            Name = name;
            FontDict = fontDict;
        }

        public override string ToString()
        {
            return $"{Name}:{FontDict}";
        }

        public string? ConvertString(ByteString s)
        {
            return ConvertString(s.Buffer);
        }


        public string ConvertString(byte[] bytes)
        {
            var typ = FontDict.GetValue<PdfName>("/Subtype");
            if (typ?.Name == "/Type1") return ConvertAnsiEncoding(bytes);
            var encoding = FontDict.GetValue<PdfName>("/Encoding");
            switch (encoding?.Name)
            {
                case "/90ms-RKSJ-H":
                    {
                        var ms = new MemoryStream(bytes);
                        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                        using var sr = new StreamReader(ms, Encoding.GetEncoding("shift_jis"));
                        return sr.ReadToEnd();
                    }
                case "/WinAnsiEncoding":
                        return ConvertAnsiEncoding(bytes);
                default:
                case "/Identity-H":
                    {
                        var sb = new StringBuilder();
                        for (int i = 0; i < bytes.Length; i += 2)
                        {
                            var c = (i + 1) < bytes.Length ? (((int)bytes[i]) << 8) + bytes[i + 1] : bytes[i];
                            sb.Append(mCMap.GetValueOrDefault(c, (char)c));
                        }
                        return sb.ToString();
                    }

            }
        }

        public string ConvertAnsiEncoding(byte[] bytes)
        {
            var sb = new StringBuilder();
            foreach (var b in bytes)
            {
                if (mDifferences.ContainsKey(b))
                {
                    sb.Append(mNameToSpecialChar.GetValueOrDefault(mDifferences[b], "?"));
                }
                else
                {
                    sb.Append((char)b);
                }
            }
            return sb.ToString();
        }

        public void InitMap(PdfParser parser)
        {
            if (FontDict.ContainsKey("/ToUnicode"))
            {
                var tu = parser.GetEntityObject(FontDict.GetValue("/ToUnicode"));
                if (tu is PdfStream ts)
                {
                    var buf = ts.GetExtractedBytes();
                    if (buf != null)
                    {
                        var ms = new MemoryStream(buf);
                        var sr = new StreamReader(ms, Encoding.ASCII);
                        var s = sr.ReadToEnd();

                        //Debug.WriteLine(Name);
                        //Debug.WriteLine(s);
                        ms = new MemoryStream(buf);
                        ParserCid(parser.Clone(ms));
                    }
                }
            }
            else if (FontDict.ContainsKey("/Encoding"))
            {
                var ed = parser.GetEntityObject(FontDict.GetValue("/Encoding")) as PdfDictionary;
                if (ed != null)
                {
                    var diffArray = parser.GetEntityObject(ed.GetValue("/Differences")) as PdfArray;
                    if(diffArray != null)
                    {
                        var code = 255;
                        foreach(var a in diffArray)
                        {
                            if(a is PdfNumber num)
                            {
                                code = num.IntValue;
                            }
                            if (a is PdfName name)
                            {
                                mDifferences[code++] = name.Name;
                            }
                        }
                        Debug.WriteLine("////////////// begin differences /////////////////");
                        foreach (var a in diffArray)
                        {
                            Debug.WriteLine(a);
                        }
                        Debug.WriteLine("////////////// end differences /////////////////");
                    }
                }
            }
            var fontDescriptor = parser.GetEntityObject(FontDict.GetValue("/FontDescriptor")) as PdfDictionary;
            if (fontDescriptor == null)
            {
                var dd = parser.GetEntityObject(FontDict.GetValue("/DescendantFonts")) as PdfArray;
                if (dd != null && dd.Count > 0)
                {
                    if (parser.GetEntityObject(dd[0]) is PdfDictionary d)
                    {
                        fontDescriptor = parser.GetEntityObject(d.GetValue("/FontDescriptor")) as PdfDictionary;
                    }
                }
            }
            if (fontDescriptor != null)
            {
                var bb = GetFontBBox(parser, fontDescriptor);
                if (bb != null) FontBBox = bb;
                var a = parser.GetEntityObject(fontDescriptor.GetValue("/Ascent")) as PdfNumber;
                var h = parser.GetEntityObject(fontDescriptor.GetValue("/CapHeight")) as PdfNumber;
                var d = parser.GetEntityObject(fontDescriptor.GetValue("/Descent")) as PdfNumber;
                if (a != null) Ascent = a.DoubleValue;
                if (h != null) CapHeight = h.DoubleValue;
                if (d != null) Descent = d.DoubleValue;
            }
        }

        PdfRectangle? GetFontBBox(PdfParser parser, PdfDictionary fontDescriptor)
        {
            var a = parser.GetEntityObject(fontDescriptor.GetValue("/FontBBox")) as PdfArray;
            return a?.GetRectangle();
        }

        enum CidState
        {
            None,
            BfChar,
            BfRange,
        };

        private void ParserCid(PdfParser parser)
        {
            CidState state;

            var stack = new List<object>();
            while (true)
            {
                var obj = parser.ParseObject();
                if (obj == null) break;
                stack.Add(obj);
//                Debug.WriteLine(obj);
                if (obj is PdfIdentifier id)
                {
                    switch (id.Identifier)
                    {
                        case "beginbfrange":
                            state = CidState.BfRange;
                            stack.Clear();
                            break;
                        case "endbfrange":
                            for (var i = 0; i < stack.Count - 1; i += 3)
                            {
                                var srcCode0 = (stack[i] as PdfHexString)!.ConvertToInt();
                                var srcCode1 = (stack[i + 1] as PdfHexString)!.ConvertToInt();
                                var dst = stack[i + 2];
                                if (dst is PdfHexString dh)
                                {
                                    var dstCode = dh.ConvertToInt();
                                    for (var sc = srcCode0; sc <= srcCode1; sc++)
                                    {
                                        mCMap.Add(sc, (Char)dstCode++);
                                    }

                                }
                            }
                            state = CidState.None;
                            stack.Clear();
                            break;


                        case "beginbfchar":
                            state = CidState.BfChar;
                            stack.Clear();
                            break;
                        case "endbfchar":
                            for (var i = 0; i < stack.Count - 1; i += 2)
                            {
                                var srcCode = (stack[i] as PdfHexString)!.ConvertToInt();
                                var dst = stack[i + 1];
                                if (dst is PdfHexString dh)
                                {
                                    var dstCode = dh.ConvertToInt();
                                    mCMap.Add(srcCode, (Char)dstCode);
                                }
                            }
                            state = CidState.None;
                            stack.Clear();
                            break;
                    }
                }
            }
        }
    }
}
