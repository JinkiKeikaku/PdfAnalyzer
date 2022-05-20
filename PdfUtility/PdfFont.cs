﻿using PdfUtility;
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
        private Dictionary<int, Char> mCMap = new();

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


        public string? ConvertString(byte[] bytes)
        {
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
                    {
                        var ms = new MemoryStream(bytes);
                        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                        using var sr = new StreamReader(ms, Encoding.ASCII);
                        return sr.ReadToEnd();
                    }
                default:
                case "/Identity-H":
                    {
                        var sb = new StringBuilder();
                        for(int i = 0; i < bytes.Length; i += 2)
                        {
                            var c = (i + 1) < bytes.Length ? (bytes[i] << 8) + bytes[i + 1] : bytes[i];
                            sb.Append(mCMap.GetValueOrDefault(c, '?'));
                        }
                        return sb.ToString();
                    }

            }
            return "unknown string";
        }

        public void InitMap(PdfParser parser)
        {
            if (FontDict.ContainsKey("/ToUnicode"))
            {
                var tu = parser.GetEntityObject(FontDict.GetValue<PdfObject>("/ToUnicode"));
                if(tu is PdfStream ts)
                {
                    var buf = ts.GetExtractedBytes();
                    if(buf != null)
                    {
                        var ms = new MemoryStream(buf);
                        var sr = new StreamReader(ms, Encoding.ASCII);
                        var s = sr.ReadToEnd();

                        Debug.WriteLine(Name);
                        Debug.WriteLine(s);
                        ms = new MemoryStream(buf);

                        ParserCid(parser.Clone(ms));
                    }
                }

            }
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
                Debug.WriteLine(obj);
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
                                var srcCode1 = (stack[i+1] as PdfHexString)!.ConvertToInt();
                                var dst = stack[i+2];
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
                                var dst = stack[i+1];
                                if(dst is PdfHexString dh)
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