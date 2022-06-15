using PdfUtility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PdfUtility
{
    public class PdfFont
    {
        public string Name;
        public PdfDictionary FontDict;
        public PdfRectangle FontBBox = new(0, 0, 1000, 1000);
//        private Dictionary<int, Char> mCMap = new();
        private Dictionary<int, string> mDifferences = new();
        private CMap mCmap = new();



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
            {"/one", "1" },
            {"/two", "2" },
            {"/three", "3" },
            {"/four", "4" },
            {"/five", "5" },
            {"/six", "6" },
            {"/seven", "7" },
            {"/eight", "8" },
            {"/nine", "9" },
            {"/zero", "0" },
            {"/emdash", "-"},
            {"/space", " " },
            {"/period", "." },
            {"/comma", "," },
            {"/exclam", "!" },
            {"/question", "?" },
            {"/numbersign ", "#" },
            {"/percent", "%" },
            {"/ampersand", "&" },
            {"/hyphen", "-" },
            {"/quoteright", "”" },
            {"/quoteleft", "“" },
            {"/parenleft", "(" },
            {"/parenright", ")" },
            {"/equal", "=" },
            {"/plus", "+" },
            {"/dollar", "$" },
            {"/collon", ":" },
            {"/semicolon ", ";" },
            {"/bullet", "∙" },
            {"/less", "<" },
            {"/greater", ">" },
            {"/underscore", "_" },
        };


        public PdfFont(string name, PdfDictionary fontDict)
        {
            Name = name;
            FontDict = fontDict;
            for (var i = 0; i < 26; i++)
            {
                var a = (char)((int)'a' + i);
                mNameToSpecialChar[$"/{a}"] = a.ToString();
                a = (char)((int)'A' + i);
                mNameToSpecialChar[$"/{a}"] = a.ToString();
            }
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
            if (typ?.Name == "/TrueType") return ConvertAnsiEncoding(bytes);
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
                        return mCmap.Convert(bytes);
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
                    var d = mDifferences[b];
                    var c = mNameToSpecialChar.GetValueOrDefault(d, "?");
                    sb.Append(c);
                }
                else
                {
                    sb.Append((char)b);
                }
            }
            return sb.ToString();
        }

        internal void InitMap(PdfParser parser)
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
                        ParserCid(parser.Copy(ms));
                    }
                }
            }
            else if (FontDict.ContainsKey("/Encoding"))
            {
                var encodingObject = parser.GetEntityObject(FontDict.GetValue("/Encoding"));
                switch (encodingObject)
                {
                    case PdfDictionary ed:
                        {
                            var diffArray = parser.GetEntityObject(ed.GetValue("/Differences")) as PdfArray;
                            if (diffArray != null)
                            {
                                var code = 255;
                                foreach (var a in diffArray)
                                {
                                    if (a is PdfNumber num)
                                    {
                                        code = num.IntValue;
                                    }
                                    if (a is PdfName name)
                                    {
                                        mDifferences[code++] = name.Name;
                                    }
                                }
                            }
                        }
                        break;
                    case PdfName name:
                        {
                            if (name.Name == "/Identity-H")
                            {
                                var dd = parser.GetEntityObject(FontDict.GetValue("/DescendantFonts")) as PdfArray;
                                if (dd != null && dd.Count > 0)
                                {
                                    if (parser.GetEntityObject(dd[0]) is PdfDictionary d)
                                    {
                                        var cidSystemInfo = parser.GetEntityObject(d.GetValue("/CIDSystemInfo")) as PdfDictionary;
                                        if (cidSystemInfo != null)
                                        {
                                            var registry = cidSystemInfo.GetString("/Registry").ToString();
                                            var ordering = cidSystemInfo.GetString("/Ordering").ToString();
                                            MakeCIDFontCMap(parser, registry, ordering);
                                        }
                                    }
                                }
                            }
                            break;
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
        void MakeCIDFontCMap(PdfParser parser, string registry, string ordering)
        {
            if (registry != "Adobe")
            {
                Debug.WriteLine("CIDFont:Registry is supported only Adobe");
                return;
            }
            switch (ordering)
            {
                case "Japan1":
                    {
                        using var mem = new MemoryStream(Properties.Resources.UniJIS2004_UTF16_H);
                        var p = parser.Copy(mem);
                        ParserCid(p);
                    }
                    break;
                default:
                    Debug.WriteLine($"CIDFont:Ordering name is not supported :: {ordering} ");
                    return;
            }

        }

        PdfRectangle? GetFontBBox(PdfParser parser, PdfDictionary fontDescriptor)
        {
            var a = parser.GetEntityObject(fontDescriptor.GetValue("/FontBBox")) as PdfArray;
            return a?.GetRectangle();
        }


        /// <summary>
        /// cidをunicodeに変換するマップの初期化
        /// ただし、サロゲートペアは対応しない
        /// </summary>
        /// <param name="parser"></param>
        private void ParserCid(PdfParser parser)
        {
            mCmap.ParserCid(parser);
        }
        private int GetIntFromCode(PdfObject obj)
        {
            if (obj is PdfHexString dh) return dh.ConvertToInt();
            if (obj is PdfNumber num) return num.IntValue;
            throw new Exception("GetIntFromCode() cannot parse to int.");
        }
    }
}
