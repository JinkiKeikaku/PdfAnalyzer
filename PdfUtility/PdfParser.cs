using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using static PdfUtility.PdfTokenizer;
using static System.Math;

namespace PdfUtility
{
    public class PdfParser
    {
        PdfTokenizer mTokenizer = null!;
        public Dictionary<int, long> Xref = null!;
        public Dictionary<int, byte[]> Xref2 = null!;

        public PdfParser()
        {
        }

        public PdfParser Clone(Stream stream)
        {
            PdfParser parser = new PdfParser();
            parser.mTokenizer = new PdfTokenizer(stream);
            parser.Xref = Xref;
            parser.Xref2 = Xref2;

            return parser;

        }

        public object? GetEntityObject(object? obj)
        {
            if (obj is PdfReference r)
            {
                if (Xref.ContainsKey(r.ObjectNumber))
                {
                    var sp = mTokenizer.StreamPosition;
                    mTokenizer.StreamPosition = Xref[r.ObjectNumber];
                    if (ParseObject() is not PdfIndirectObject io) throw new Exception("GetEntityObject:Not indirect object.");
                    mTokenizer.StreamPosition = sp;
                    return GetEntityObject(io.IndirectObject);
                }
                else
                {
                    if (Xref2?.ContainsKey(r.ObjectNumber) == true)
                    {
                        using var mem = new MemoryStream(Xref2[r.ObjectNumber]);
                        var tmp = mTokenizer;
                        mTokenizer = new PdfTokenizer(mem);
                        var io = ParseObject();
                        if (io == null) throw new Exception("GetEntityObject:cannot parse xref stream.");
                        mTokenizer = tmp;
                        return GetEntityObject(io);
                    }
                    return null;
                }
            }
            return obj;
        }

        //public int GetInt(object? obj)
        //{
        //    var a = GetEntityObject(obj);
        //    if (a is double d) return (int)d;
        //    throw new Exception("GetInt:Not number object.");
        //}
        //public double GetDouble(object? obj)
        //{
        //    var a = GetEntityObject(obj);
        //    if (a is double d) return d;
        //    throw new Exception("GetInt:Not number object.");
        //}

        public T? ParseObject<T>() where T: PdfObject
        {
            return ParseObject() as T;

        }

        public PdfObject? ParseObject()
        {
            var t = mTokenizer.GetNextToken();
            switch (t.Kind)
            {
                case TokenKind.StartDictionary:
                    var dic = ParseDictionary();
                    if (mTokenizer.IsNextTokenStream())
                    {
                        var length = (GetEntityObject(dic.GetValue<PdfObject>("/Length")) as PdfNumber)!.IntValue;
                        var buffer = mTokenizer.ReadBytes(length);
                        var td2 = mTokenizer.GetNextToken();
                        if (td2.Kind != TokenKind.Identifier || td2.GetString() != "endstream") throw new Exception("Stream cannot find 'endstream'");
                        return new PdfStream(dic, buffer);
                    }
                    return dic;
                case TokenKind.StartArray:
                    var a = new PdfArray();
                    {
                        while (true)
                        {
                            var ta2 = mTokenizer.GetNextToken();
                            if (ta2.Kind == TokenKind.EndArray) return a;
                            if (ta2.IsEof) throw new Exception("Unexpected eof in array.");
                            mTokenizer.PushToken(ta2);
                            var obj = ParseObject();
                            a.Add(obj);
                        }
                    }
                case TokenKind.Number:
                    var t2 = mTokenizer.GetNextToken();
                    if (t2.Kind == TokenKind.Number)
                    {
                        var t3 = mTokenizer.GetNextToken();
                        if (t3.Kind == TokenKind.Identifier)
                        {
                            if (!double.TryParse(t.GetString(), out var i1)) throw new Exception("PdfReference number is not Int.");
                            if (!double.TryParse(t2.GetString(), out var i2)) throw new Exception("PdfReference number is not Int.");
                            if (t3.GetString() == "R") return new PdfReference((int)i1, (int)i2);
                            if (t3.GetString() == "obj")
                            {
                                var obj = ParseObject();
                                if(obj == null) throw new Exception("Cannot create PdfObject.ParseObject() return null.");
                                var t4 = mTokenizer.GetNextToken();
                                if (t4.Kind != TokenKind.Identifier || t4.GetString() != "endobj")
                                {
                                    throw new Exception("PdfObject is not terminated endobj.");
                                }
                                return new PdfIndirectObject(obj, (int)i1, (int)i2);
                            }
                        }
                        mTokenizer.PushToken(t3);
                    }
                    mTokenizer.PushToken(t2);
                    return new PdfNumber(t.GetDouble());
                case TokenKind.Name:
                    return new PdfName(t.GetString());
                case TokenKind.String:
                    return new PdfString(t.Bytes);
                case TokenKind.HexString:
                    return new PdfHexString(t.GetString());
                case TokenKind.Identifier:
                    var s = t.GetString();
                    if (s == "null")   return new PdfNull();
                    return new PdfIdentifier(s);
                default:
                    Debug.WriteLine($"Unknown token:{t}");
                    break;

            }
            if (t.Kind == TokenKind.Eof) return null;
            throw new Exception("Cannot parse unknown object");
        }

        /// <summary>
        /// Check pdf header and return version(like 1.x). 
        /// </summary>
        /// <param name="stream">stream</param>
        /// <returns>Version string like '1.x'. if not pdf file , return empty string .</returns>
        public static string GetPdfVersion(Stream stream)
        {
            stream.Position = 0;
            var buf = new byte[5];
            if (stream.Read(buf, 0, 5) == 5)
            {
                if (buf.SequenceEqual(Encoding.ASCII.GetBytes("%PDF-")))
                {
                    var buf2 = new byte[3];
                    if (stream.Read(buf2, 0, 3) == 3)
                    {
                        var pdfVersion = Encoding.ASCII.GetString(buf2);
                        return pdfVersion;
                    }
                }
            }
            return String.Empty;
        }

        public PdfDictionary Start(Stream stream)
        {
            var startxref = ParseStartxref(stream);
            Debug.WriteLine($"startxref:{startxref}");
            if (startxref < 0) throw new Exception("PDF:startxref not found.");
            mTokenizer = new PdfTokenizer(stream);
            mTokenizer.StreamPosition = startxref;
            var trailer = ParseXRefTable();
            if (trailer == null) throw new Exception("Trailer not found.");
            return trailer;
        }

        private long ParseStartxref(Stream stream)
        {
            var size = Min(stream.Length, 1024);
            stream.Seek(-size, SeekOrigin.End);
            var buf = new byte[size];
            stream.Read(buf);
            var s = Encoding.ASCII.GetBytes("startxref");
            var i = (int)(size - s.Length);
            while (i >= 0)
            {
                if (buf[i..(i + s.Length)].SequenceEqual(s)) break;
                i--;
            }
            if (i < 0) return -1;
            i += "startxref".Length;
            stream.Seek(-(size - i), SeekOrigin.End);
            var tokenizer = new PdfTokenizer(stream);
            var t = tokenizer.GetNextToken();
            if (t.Kind == TokenKind.Number) return t.GetInt();
            return -1;
        }

        private PdfDictionary ParseXRefTable()
        {
            Xref = new Dictionary<int, long>();
            if (ParseXRef())
            {
                var trailer = ReadTrailer();
                if (trailer.ContainsKey("/Prev"))
                {
                    var prev = trailer.GetInt("/Prev");
                    mTokenizer.StreamPosition = prev;
                    ParseXRef();
                }
                return trailer;
            }
            return ParseXrefStream();
        }

        private bool ParseXRef()
        {
            var t = mTokenizer.GetNextToken();
            if (t.Kind == TokenKind.Identifier && t.GetString() == "xref")
            {
                var t1 = mTokenizer.GetNextToken();
                var t2 = mTokenizer.GetNextToken();
                if (t1.Kind != TokenKind.Number || t2.Kind != TokenKind.Number) throw new Exception("Cannot find xref numbers");
                var start = t1.GetInt();// Convert.ToInt32(t1.Value);
                var n = t2.GetInt();// Convert.ToInt32(t2.Value);
                for (var i = 0; i < n; i++)
                {
                    t1 = mTokenizer.GetNextToken();
                    mTokenizer.GetNextToken();
                    t2 = mTokenizer.GetNextToken();
                    if (t1.Kind != TokenKind.Number || t2.Kind != TokenKind.Identifier) throw new Exception("Cannot find xref numbers");
                    if (t2.GetString() == "n") Xref.Add(i + start, long.Parse(t1.GetString()));
                }
                return true;
            }
            mTokenizer.PushToken(t);
            return false;
        }

        private PdfDictionary ParseXrefStream()
        {
            var obj = ParseObject();
            if (obj is not PdfIndirectObject indirectObject) throw new Exception("connot find xref.");
            var stream = indirectObject.IndirectObject as PdfStream;
            if (stream == null) throw new Exception("connot find xref.");
            var buf = stream.GetExtractedBytes();
            if (buf == null) throw new Exception("connot extract xref.");
            var w = stream.Dictionary.GetValue<PdfArray>("/W");
            if (w == null) throw new Exception("connot extract xref, cannot find W.");
            var n1 = w.GetAt<PdfNumber>(0)!.IntValue;
            var n2 = w.GetAt<PdfNumber>(1)!.IntValue;
            var n3 = w.GetAt<PdfNumber>(2)!.IntValue;
            var n0 = n1 + n2 + n3;

            var index = 0;
            for (int i = 0; i < buf.Length; i += n0)
            {
                //まずtypeが1の時を調べる。
                if (buf[i + n1 - 1] == 1)
                {
                    Xref.Add(index, ConvertInt(buf, i + n1, n2));
                }
                index++;
            }
            index = 0;

            var bufDic = new Dictionary<int, (byte[] buf, (int objextIndex, int bufPos)[])>();
            Xref2 = new Dictionary<int, byte[]>();
            for (int i = 0; i < buf.Length; i += n0)
            {
                //typeが2の時を調べる(圧縮)。
                if (buf[i + n1 - 1] == 2)
                {
                    var objectNumber = ConvertInt(buf, i + n1, n2);
                    if (!bufDic.ContainsKey(objectNumber))
                    {
                        var r = new PdfReference(objectNumber, 0);
                        var ps = GetEntityObject(r) as PdfStream;
                        if (ps == null) throw new Exception("cannnot find xref indirect stream.");
                        var eb = ps.GetExtractedBytes();
                        if (eb == null) throw new Exception("cannnot extract xref object stream.");
                        if (ps.Dictionary.GetValue<PdfName>("/Type")?.Name != "/ObjStm")
                        {
                            throw new Exception("stream is not object stream.");
                        }
                        int n = ps.Dictionary.GetInt("/N");
                        int first = ps.Dictionary.GetInt("/First");
                        var bufPos = new (int, int)[n];
                        var tmp = Encoding.ASCII.GetString(eb);
                        var ss = Encoding.ASCII.GetString(eb[0..first]).Split();
                        for (var i0 = 0; i0 < n; i0++)
                        {
                            var ix = int.Parse(ss[i0 * 2]);
                            var px = int.Parse(ss[i0 * 2 + 1]) + first;
                            bufPos[i0] = (ix, px);
                        }
                        Debug.WriteLine(tmp);
                        bufDic[objectNumber] = (eb, bufPos);
                    }
                    var pos = ConvertInt(buf, i + n1 + n2, n3);
                    var bb = bufDic[objectNumber];
                    Xref2.Add(index, bb.buf[bb.Item2[pos].bufPos..]);
                    Debug.WriteLine($"{index} Xref2:{bb.Item2[pos]}");

                }
                index++;
            }
            return stream.Dictionary;
        }

        private int ConvertInt(byte[] buf, int pos, int size)
        {
            var x = 0;
            for (var j = 0; j < size; j++)
            {
                x = x * 256 + buf[pos + j];
            }
            return x;
        }

        private PdfDictionary ReadTrailer()
        {
            var t = mTokenizer.GetNextToken();
            if (t.Kind != TokenKind.Identifier || t.GetString() != "trailer")
            {
                throw new Exception("cannot find trailer.");
            }
            var d = ParseObject() as PdfDictionary;
            if (d == null) throw new Exception("cannot find trailer dictionary.");
            return d;
        }

        PdfDictionary ParseDictionary()
        {
            PdfDictionary dict = new PdfDictionary();
            var t = mTokenizer.GetNextToken();
            while (t.Kind != TokenKind.EndDictionary)
            {
                if (t.Kind != TokenKind.Name) throw new Exception("Invalid key in dictionary.");
                Debug.WriteLine(t.ToString());
                var key = t.GetString();
                var obj = ParseObject();
                dict.Add(key, obj);
                t = mTokenizer.GetNextToken();
            }
            return dict;
        }
    }
}
