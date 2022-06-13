using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PdfUtility.PdfTokenizer;

namespace PdfUtility
{
    /// <summary>
    /// PDFのドキュメントクラス
    /// </summary>
    public class PdfDocument : IDisposable
    {
        public interface IListner
        {
            string OnRequestPassword();
            void OnPasswordError();
        }

        public PdfDocument(IListner? listner = null)
        {
            mListner = listner;
        }

        /// <summary>
        /// PDFのバージョンが入ります。
        /// </summary>
        public string PdfVerson { get; private set; } = "";

        /// <summary>
        /// Rootディクショナリを返します。
        /// </summary>
        public PdfDictionary Root
        {
            get
            {
                var obj = Trailer.GetValue<PdfReference>("/Root") ??
                    throw new Exception("Cannot find /Root");
                return GetEntityObject<PdfDictionary>(obj) ??
                    throw new Exception("Cannot parse root object");
            }
        }

        /// <summary>
        /// Trailerディクショナリを返します。
        /// </summary>
        public PdfDictionary Trailer { get; private set; } = new();

        /// <summary>
        /// ドキュメントを開きます。
        /// </summary>
        /// <param name="stream">
        /// PDFファイルのストリーム。渡されたストリームはPdfDocumentの
        /// Close()で閉じられます。外部でストリームが閉じられると失敗します。
        /// </param>
        /// <exception cref="Exception"></exception>
        public void Open(Stream stream)
        {
            InitDocument();
            mStream = stream;
            PdfVerson = GetPdfVersion(mStream) ?? throw new Exception("Not pdf file.");
            var startxref = GetStartxref(mStream);
            Debug.WriteLine($"startxref:{startxref}");
            if (startxref < 0) throw new Exception("PDF:startxref not found.");
            mStream.Position = startxref;
            InitXrefTableAndTrailer(mStream);
            mParser = new PdfParser(mStream, XrefTable);

            if (Trailer.ContainsKey("/Encrypt"))
            {
                var password = mListner?.OnRequestPassword();
                if (password != null)
                {
                    throw new Exception("This program is not supported encrypted PDF.");
                }
            }
        }

        /// <summary>
        /// 暗号化されていればtrue
        /// </summary>
        public bool IsEncrypt() => Trailer.ContainsKey("/Encrypt") == true;

        /// <summary>
        /// ドキュメントを閉じます。ストリームはここで閉じられます。
        /// </summary>
        public void Close()
        {
            mParser = null;
            mStream?.Close();
            mStream = null;
            Trailer.Clear();
            XrefTable.Clear();
        }

        public void Dispose() => Close();

        /// <summary>
        /// Xrefテーブルから見つかったオブジェクトのリストを返します。
        /// </summary>
        /// <returns></returns>
        public List<(int ObjectNumber, PdfObject Obj)> GetXrefObjects()
        {
            var objectList = new List<(int ObjectNumber, PdfObject Obj)>();

            foreach (var x in XrefTable.XrefMap)
            {
                var v = x.Value;
                if (v.valid)
                {
                    var p = Parser.GetObject(x.Key);
                    if (p != null) objectList.Add((x.Key, p));
                }
            }
            foreach (var x in XrefTable.XrefStreamMap)
            {
                var p = Parser.GetObject(x.Key);
                if (p != null) objectList.Add((x.Key, p));
            }
            objectList.Sort((x, y) => x.ObjectNumber - y.ObjectNumber);
            return objectList;
        }

        /// <summary>
        /// グラフィックス命令を１つづつ列挙します。
        /// </summary>
        /// <param name="contents">コンテンツ</param>
        /// <param name="func">列挙された命令を処理する関数</param>
        /// <exception cref="Exception"></exception>
        public void ParserGraphics(byte[] contents, Func<List<PdfObject>, PdfDocument, bool> func)
        {
            var ret = new List<PdfObject>();
            using var ms = new MemoryStream(contents);
            var parser = new PdfParser(ms, XrefTable);
            //            if (parser == null) throw new Exception("Cannot create graohics parser.");
            while (true)
            {
                var obj = parser.ParseObject();
                if (obj == null) break;
                //                Debug.WriteLine(obj.ToString());
                ret.Add(obj);
                if (obj is PdfIdentifier)
                {
                    if (!func(ret, this)) break;
                    ret.Clear();
                }
            }
        }

        /// <summary>
        /// 総ページ数を返す
        /// </summary>
        /// <returns></returns>
        public int GetPageSize()
        {
            var rootPages = GetEntityObject<PdfDictionary>(Root.GetValue("/Pages"));
            return rootPages?.GetInt("/Count") ?? 0;
        }

        /// <summary>
        /// インデックスで指定されるページオブジェクトを返します。
        /// インデックスは0から始まり1ページ目が0です。
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public PdfPage GetPage(int pageIndex)
        {
            var rootPages = GetEntityObject<PdfDictionary>(Root.GetValue("/Pages"));
            if (rootPages == null) throw new Exception("Root pages is null. maybe document is not open.");

            var pageAttribute = new PdfPageAttribute();

            var pageDic = GetPageDictionary(rootPages, pageAttribute, pageIndex);
            if (pageDic == null) throw new Exception($"cannot find page {pageIndex}");
            var contents = GetEntityObject(pageDic.GetValue("/Contents"));
            PdfArray contentsArray = new();
            if (contents is PdfStream pdfStream)
            {
                contentsArray.Add(pdfStream);
            }
            if (contents is PdfArray pdfArray)
            {
                contentsArray = pdfArray;
            }
            var page = new PdfPage();
            page.Attribute = pageAttribute;
            var resources = GetEntityObject<PdfDictionary>(pageDic.GetValue("/Resources"));
            if (resources == null) throw new Exception("Page has no resource dictionary.");
            page.ResourcesDictionary = resources;

            for (int i = 0; i < contentsArray.Count; i++)
            {
                var contentsStream = GetEntityObject<PdfStream>(contentsArray.GetAt<PdfObject>(i));
                if (contentsStream != null)
                {
                    var bytes = contentsStream.GetExtractedBytes();
                    if (bytes != null)
                    {
                        page.ContentsList.Add(bytes);
                    }
                }
            }
            return page;
        }

        /// <summary>
        /// /Pageを取得します。
        /// </summary>
        /// <param name="pages"></param>
        /// <param name="pageIndex">0から始まるページのインデックス。１ページ目が0になる。</param>
        /// <returns>ページのディクショナリを返す。無ければnull。</returns>
        /// <exception cref="Exception"></exception>
        public PdfDictionary? GetPageDictionary(PdfDictionary pages, PdfPageAttribute pageAttribute, int pageIndex)
        {
            return GetPageDictionary(pages, pageAttribute, pageIndex, 0);
        }

        /// <summary>
        /// オブジェクトを渡すとリファレンスの場合、そのリファレンスをたどり最終的に指し示すオブジェクトを
        /// 返します。渡されたオブジェクトがリファレンスでない場合は渡されたオブジェクトが返ります。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public PdfObject? GetEntityObject(PdfObject? obj) =>
            Parser.GetEntityObject(obj);

        /// <summary>
        /// オブジェクトを渡すとリファレンスの場合、そのリファレンスをたどり最終的に指し示すオブジェクトを
        /// 返します。渡されたオブジェクトがリファレンスでない場合は渡されたオブジェクトが返ります。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public T? GetEntityObject<T>(PdfObject? obj) where T : PdfObject =>
            Parser.GetEntityObject<T>(obj);

        /// <summary>
        /// リソースディクショナリからフォントのリストを作成します。
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        public List<PdfFont> GetFonts(PdfDictionary resource)
        {
            var fonts = new List<PdfFont>();
            var fontDic = GetEntityObject<PdfDictionary>(resource.GetValue("/Font"));
            if (fontDic == null) return fonts;

            int n = fontDic.Count;
            for (var i = 0; i < n; i++)
            {
                var p = fontDic.GetAt(i);
                var name = p.Key;
                var dic = GetEntityObject<PdfDictionary>(p.Value);
                if (dic != null)
                {
                    var f = new PdfFont(name, dic);
                    f.InitMap(Parser);
                    fonts.Add(f);
                }
            }
            return fonts;
        }

        /// <summary>
        /// リソースディクショナリの/XObjectからから[name]のストリームを取得する。
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public PdfStream? GetXObjectStream(PdfDictionary resource, string name)
        {
            var xobjectDic = GetEntityObject<PdfDictionary>(resource.GetValue("/XObject"));
            return GetEntityObject<PdfStream>(xobjectDic?.GetValue(name));
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

        internal PdfXrefTable XrefTable { get; } = new();

        private void InitDocument()
        {
            PdfVerson = "";
            Trailer.Clear();
            XrefTable.Clear();
            mParser = null;
            mStream = null;
        }

        private static long GetStartxref(Stream stream)
        {
            //終わりから1024バイト以内にstartxrefがあるか調べます。
            //仕様上は改行などが1024バイト以上後ろにあるファイルも許されますが、そこまで考えません。
            var size = Math.Min(stream.Length, 1024);
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

        private PdfDictionary? GetPageDictionary(PdfDictionary pages, PdfPageAttribute pageAttribute, int pageIndex, int topPage)
        {
            GetPageAttribute(pages, pageAttribute);
            var c = topPage;
            var kids = pages.GetValue<PdfArray>("/Kids");
            if (kids == null) throw new Exception("cannot find kids in pages dictionary");
            for (var i = 0; i < kids.Count; i++)
            {
                var kid = kids[i];
                var pd = GetEntityObject<PdfDictionary>(kid) ?? throw new Exception("kid is not dictionary.");
                switch (pd.GetValue<PdfName>("/Type")?.Name)
                {
                    case "/Page":
                        if (c == pageIndex)
                        {
                            GetPageAttribute(pd, pageAttribute);
                            return pd;
                        }
                        c++;
                        break;
                    case "/Pages":
                        var dc = pd.GetInt("/Count");
                        if (pageIndex < dc + c)
                        {
                            return GetPageDictionary(pd, pageAttribute, pageIndex, c);
                        }
                        c += dc;
                        break;
                }
            }
            return null;
        }

        private PdfRectangle? GetRectFromPage(PdfDictionary pdfDic, string name, bool inheritable)
        {
            var a = pdfDic.GetValue<PdfArray>(name);
            if (a != null) return a.GetRectangle();
            if (!inheritable) return null;
            var parent = GetEntityObject<PdfDictionary>(pdfDic.GetValue(name));
            if (parent == null) return null;
            return GetRectFromPage(parent, name, inheritable);
        }

        private PdfNumber? GetNumberFromPage(PdfDictionary pdfDic, string name, bool inheritable)
        {
            var a = pdfDic.GetValue<PdfNumber>(name);
            if (a != null) return a;
            if (!inheritable) return null;
            var parent = GetEntityObject<PdfDictionary>(pdfDic.GetValue(name));
            if (parent == null) return null;
            return GetNumberFromPage(parent, name, inheritable);
        }


        private void GetPageAttribute(PdfDictionary pdfDic, PdfPageAttribute pageAttribute)
        {
            var mediaBox = GetRectFromPage(pdfDic, "/MediaBox", true);
            if (mediaBox != null) pageAttribute.MediaBox = mediaBox;
            var cropBox = GetRectFromPage(pdfDic, "/CropBox", true);
            if (cropBox != null) pageAttribute.CropBox = cropBox;
            var rotate = GetNumberFromPage(pdfDic, "/Rotate", true);
            if (rotate != null) pageAttribute.Rotate = rotate.IntValue;
        }


        private void InitXrefTableAndTrailer(Stream stream)
        {
            PdfDictionary? trailer = null;
            while (true)
            {
                if (ParseXRef(stream))
                {
                    var t = ReadTrailer(stream);
                    if (trailer == null) trailer = t;

                    if (t.ContainsKey("/XRefStm"))
                    {
                        var xrefStmPos = t.GetInt("/XRefStm");
                        stream.Position = xrefStmPos;
                        ParseXrefStream(stream);
                    }
                    if (!t.ContainsKey("/Prev")) break;
                    var prev = t.GetInt("/Prev");
                    stream.Position = prev;
                }
                else
                {
                    var t = ParseXrefStream(stream);
                    if (trailer == null) trailer = t;
                    if (t.ContainsKey("/XRefStm"))
                    {
                        //hybrid referenceの時。ちょっと自信がない。
                        var xrefStmPos = t.GetInt("/XRefStm");
                        stream.Position = xrefStmPos;
                        ParseXrefStream(stream);
                    }
                    if (!t.ContainsKey("/Prev")) break;
                    var prev = t.GetInt("/Prev");
                    stream.Position = prev;
                }
            }
            Trailer = trailer;
        }

        private PdfDictionary ReadTrailer(Stream stream)
        {
            var tokenizer = new PdfTokenizer(stream);
            var t = tokenizer.GetNextToken();
            if (t.Kind != TokenKind.Identifier || t.GetString() != "trailer")
            {
                throw new Exception("cannot find trailer.");
            }
            var parser = new PdfParser(stream, XrefTable);
            var d = parser.ParseObject<PdfDictionary>() ?? throw new Exception("cannot find trailer dictionary.");
            return d;
        }

        private bool ParseXRef(Stream stream)
        {
            long GetPos(byte[] bytes)
            {
                var c = 0L;
                for (var i = 0; i < 10; i++)
                {
                    c = c * 10 + ((bytes[i] - '0') & 255);
                }
                return c;
            }

            //xref
            //0 1
            //00000000000 65535 f
            //4 1
            //00000000632 00000 n
            //更新するとこのようなエントリになるそうなので対応。
            var tokenizer = new PdfTokenizer(stream);
            var saved = stream.Position;
            var t1 = tokenizer.GetNextToken();
            if (t1.Kind != TokenKind.Identifier || t1.GetString() != "xref")
            {
                stream.Position = saved;
                return false;
            }
            while (true)
            {
                saved = stream.Position;
                t1 = tokenizer.GetNextToken();
                if (t1.Kind != TokenKind.Number)
                {
                    stream.Position = saved;
                    return true;
                }
                var t2 = tokenizer.GetNextToken();
                if (t2.Kind != TokenKind.Number) throw new Exception("Cannot find xref numbers");
                var start = t1.GetInt();
                var n = t2.GetInt();
                tokenizer.Skip();
                var bytes = new byte[20];
                for (var i = 0; i < n; i++)
                {
                    stream.Read(bytes, 0, 20);
                    var pos = GetPos(bytes);
                    var c = (char)bytes[17];
                    if (!XrefTable.ContainsKey(i + start))
                    {
                        XrefTable.Add(i + start, (pos, c == 'n'));
                    }

/*
                    t1 = tokenizer.GetNextToken();
                    tokenizer.GetNextToken();
                    t2 = tokenizer.GetNextToken();
                    if (t1.Kind != TokenKind.Number || t2.Kind != TokenKind.Identifier) throw new Exception("Cannot find xref numbers");
                    if (!XrefTable.ContainsKey(i + start))
                    {
                        XrefTable.Add(i + start, (long.Parse(t1.GetString()), t2.GetString() == "n"));
                    }
*/
                }
            }
        }


        private PdfDictionary ParseXrefStream(Stream stream)
        {
            var parser = new PdfParser(stream, XrefTable);
            var indirectObject = parser.ParseObject<PdfIndirectObject>() ?? throw new Exception("connot find xref object.");
            var pdfStream = indirectObject.IndirectObject as PdfStream ?? throw new Exception("connot find xref."); ;
            var buf = pdfStream.GetExtractedBytes() ?? throw new Exception("connot extract xref.");
            var w = pdfStream.Dictionary.GetValue<PdfArray>("/W") ?? throw new Exception("connot extract xref, cannot find W.");
            var n1 = w.GetAt<PdfNumber>(0)!.IntValue;
            var n2 = w.GetAt<PdfNumber>(1)!.IntValue;
            var n3 = w.GetAt<PdfNumber>(2)!.IntValue;
            var n0 = n1 + n2 + n3;

            var indexArray = pdfStream.Dictionary.GetValue<PdfArray>("/Index") ?? new();
            var indexList = new List<int>();
            for (var i = 0; i < indexArray.Count; i += 2)
            {
                if (indexArray[i] is not PdfNumber topNum) throw new Exception("ParseXrefStream bad index");
                if (indexArray[i + 1] is not PdfNumber countNum) throw new Exception("ParseXrefStream bad index");
                var top = topNum.IntValue;
                var count = countNum.IntValue;
                for (var x = 0; x < count; x++)
                {
                    indexList.Add(x + top);
                }
            }
            var index = 0;
            //まずtypeが1の時を調べる。
            for (int i = 0; i < buf.Length; i += n0)
            {
                if (buf[i + n1 - 1] == 1)//Type == 1
                {
                    //indexList.Count == 0=>/Indexが無かったときは並び順を使う（？）。
                    var idx = indexList.Count == 0 ? index : indexList[index];
                    if (!XrefTable.ContainsKey(idx))
                    {
                        XrefTable.Add(idx, (ConvertInt(buf, i + n1, n2), true));
                    }
                }
                index++;
            }
            //typeが2の時を調べる(圧縮)。
            index = 0;
            for (int i = 0; i < buf.Length; i += n0)
            {
                if (buf[i + n1 - 1] == 2)//Type == 2
                {
                    var objectNumber = ConvertInt(buf, i + n1, n2);
                    var objectIndex = ConvertInt(buf, i + n1 + n2, n3);
                    //var ps = parser.GetObject(objectNumber) as PdfStream ??
                    //    throw new Exception("cannnot find xref indirect stream.");
                    var idx = indexList.Count == 0 ? index : indexList[index];
                    if (!XrefTable.ContainsKey(idx))
                    {
                        XrefTable.Add(idx, objectNumber, objectIndex);
                        if (!XrefTable.ContainXrefStreamObject(objectNumber))
                        {
                            var ps = parser.GetObject(objectNumber) as PdfStream ??
                                throw new Exception("cannnot find xref indirect stream.");
                            XrefTable.Add(objectNumber, ps);
                        }
                    }
                }
                index++;
            }
            return pdfStream.Dictionary;
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


        private PdfParser Parser
        {
            get
            {
                if (mParser == null) throw new Exception("Parser is null. maybe document is not open.");
                return mParser;
            }
        }
        private Stream Stream
        {
            get
            {
                if (mStream == null) throw new Exception("Stream is null. maybe document is not open.");
                return mStream;
            }
        }

        private PdfParser? mParser;
        private IListner? mListner;
        private Stream? mStream;
    }
}
