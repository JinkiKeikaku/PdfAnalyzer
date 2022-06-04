using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using static PdfUtility.PdfTokenizer;
using static System.Math;

namespace PdfUtility
{
    class PdfParser
    {
        readonly PdfTokenizer mTokenizer;
        readonly PdfXrefTable mXrefTable;
        public PdfParser(Stream stream, PdfXrefTable xrefTable)
        {
            mTokenizer = new PdfTokenizer(stream);
            mXrefTable = xrefTable;
        }
        public PdfParser Copy(Stream stream)
        {
            return new PdfParser(stream, mXrefTable);
        }

        public PdfObject? GetObject(int reference)
        {
            var pos = mXrefTable.GetStreamPosition(reference);
            if (pos >= 0)
            {
                var saved = mTokenizer.StreamPosition;
                mTokenizer.StreamPosition = pos;
                var obj = ParseObject<PdfIndirectObject>() ??
                    throw new Exception("GetObject:Not indirect object.");
                mTokenizer.StreamPosition = saved;
                return obj.IndirectObject;
            }
            else
            {
                var buf = mXrefTable.GetObjectStreamBuffer(reference);
                if (buf == null) return null;
                using var mem = new MemoryStream(buf);
                var parser = new PdfParser(mem, mXrefTable);
                return parser.ParseObject() ??
                    throw new Exception("GetObject:Cannot get xref strteam object Not indirect object.");
            }
        }

        public T? ParseObject<T>() where T : PdfObject  => ParseObject() as T;

        public PdfObject? ParseObject()
        {
            var t = mTokenizer.GetNextToken();
            switch (t.Kind)
            {
                case TokenKind.StartDictionary:
                    var dic = ParseDictionary();
                    if (mTokenizer.IsNextTokenStream())
                    {
                        var length = GetEntityObject<PdfNumber>(dic.GetValue("/Length"))!.IntValue;
                        var buffer = mTokenizer.ReadBytes(length);
                        var td2 = mTokenizer.GetNextToken();
                        if (td2.Kind != TokenKind.Identifier || td2.GetString() != "endstream") throw new Exception("Stream cannot find 'endstream'");
                        return new PdfStream(dic, buffer);
                    }
                    return dic;
                case TokenKind.StartArray:
                    return ParseArray();
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
                                var obj = ParseObject() ?? throw new Exception("Cannot create PdfObject.ParseObject() return null.");
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
                    if (s == "null") return new PdfNull();
                    return new PdfIdentifier(s);
                case TokenKind.Eof:
                    return null;
                default:
                    Debug.WriteLine($"Unknown token:{t}");
                    break;
            }
            if (t.Kind == TokenKind.Eof) return null;
            throw new Exception("Cannot parse unknown object");
        }

        public T? GetEntityObject<T>(PdfObject? obj) where T : PdfObject => 
            GetEntityObject(obj) as T;

        public PdfObject? GetEntityObject(PdfObject? obj)
        {
            if (obj is not PdfReference r) return obj;
            return GetEntityObject(GetObject(r.ObjectNumber));
        }

        PdfArray ParseArray()
        {
            PdfArray a = new PdfArray();
            var t = mTokenizer.GetNextToken();
            while (t.Kind != TokenKind.EndArray)
            {
                if (t.IsEof) throw new Exception("Unexpected eof in array.");
                mTokenizer.PushToken(t);
                var obj = ParseObject() ?? new PdfNull();
                a.Add(obj);
                t = mTokenizer.GetNextToken();
            }
            return a;
        }


        PdfDictionary ParseDictionary()
        {
            PdfDictionary dict = new PdfDictionary();
            var t = mTokenizer.GetNextToken();
            while (t.Kind != TokenKind.EndDictionary)
            {
                if (t.Kind != TokenKind.Name) throw new Exception("Invalid key in dictionary.");
                var key = t.GetString();
                var obj = ParseObject();
                dict.Add(key, obj);
                t = mTokenizer.GetNextToken();
            }
            return dict;
        }
    }
}
