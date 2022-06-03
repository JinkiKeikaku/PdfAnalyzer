using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
//            PdfFont.GetCIDFontCMap();
        }

        /// <summary>
        /// PDFのバージョンが入ります。
        /// </summary>
        public string PdfVerson { get; private set; } = "";
        /// <summary>
        /// RootのPagesディクショナリを返します。
        /// </summary>
        public PdfDictionary? RootPages { get; private set; }
        /// <summary>
        /// Rootディクショナリを返します。
        /// </summary>
        public PdfDictionary? Root { get; private set; }
        /// <summary>
        /// Trailerディクショナリを返します。
        /// </summary>
        public PdfDictionary? Trailer { get; private set; }

        /// <summary>
        /// ドキュメントを開きます。
        /// </summary>
        /// <param name="stream">PDFファイルのストリーム。渡されたストリームはPdfDocumentの
        /// Close()で閉じられます。外部でストリームが閉じられると失敗します。
        /// </param>
        /// <exception cref="Exception"></exception>
        public void Open(Stream stream)
        {
            mStream = stream;
            var ver = PdfParser.GetPdfVersion(stream) ?? throw new Exception("Not pdf file.");
            PdfVerson = ver;
            mParser = new();
            Trailer = mParser.Start(stream);
            if (Trailer.ContainsKey("/Encrypt"))
            {
                var password = mListner?.OnRequestPassword();
                if(password != null)
                {
                    throw new Exception("This program is not supported encrypted PDF.");
                }
            }

            Root = GetRootObject() ?? throw new Exception("cannot find root object.");
            RootPages = GetEntityObject<PdfDictionary>(Root.GetValue("/Pages"));
        }

        /// <summary>
        /// 暗号化されていればtrue
        /// </summary>
        public bool IsEncrypt()
        {
            return Trailer?.ContainsKey("/Encrypt") == true;
        }  

        /// <summary>
        /// ドキュメントを閉じます。ストリームはここで閉じられます。
        /// </summary>
        public void Close()
        {
            mParser = null;
            mStream?.Close();
            mStream = null;
        }

        public void Dispose()
        {
            Close();
        }

        /// <summary>
        /// Xrefテーブルから見つかったオブジェクトのリストを返します。
        /// </summary>
        /// <returns></returns>
        public List<(int ObjectNumber, PdfObject Obj)> GetXrefObjects()
        {
            return Parser.GetXReferenceObjects();
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
            var parser = mParser?.Clone(ms);
            if (parser == null) throw new Exception("Cannot create graohics parser.");
            while (true)
            {
                var obj = parser.ParseObject();
                if (obj == null) break;
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
        public int GetPageSize() => RootPages?.GetInt("/Count") ?? 0;

        /// <summary>
        /// インデックスで指定されるページオブジェクトを返します。
        /// インデックスは0から始まり1ページ目が0です。
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public PdfPage GetPage(int pageIndex)
        {
            if (RootPages == null) throw new Exception("Root pages is null. maybe document is not open.");
            var pageDic = GetPageDictionary(RootPages, pageIndex, 0);
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
            page.Attribute = GetPageAttribute(pageDic);
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
        public PdfDictionary? GetPageDictionary(PdfDictionary pages, int pageIndex)
        {
            return GetPageDictionary(pages, pageIndex, 0);
        }

        /// <summary>
        /// オブジェクトを渡すとリファレンスの場合、そのリファレンスをたどり最終的に指し示すオブジェクトを
        /// 返します。渡されたオブジェクトがリファレンスでない場合は渡されたオブジェクトが返ります。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public PdfObject? GetEntityObject(PdfObject? obj) {
            return Parser.GetEntityObject(obj);
        }

        /// <summary>
        /// オブジェクトを渡すとリファレンスの場合、そのリファレンスをたどり最終的に指し示すオブジェクトを
        /// 返します。渡されたオブジェクトがリファレンスでない場合は渡されたオブジェクトが返ります。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public T? GetEntityObject<T>(PdfObject? obj) where T : PdfObject => GetEntityObject(obj) as T;



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
                    f.InitMap(mParser!);
                    fonts.Add(f);
                }
            }
            return fonts;
        }

        public PdfStream? GetXObjectStream(PdfDictionary resource, string name)
        {
            var xobjectDic = GetEntityObject<PdfDictionary>(resource.GetValue("/XObject"));
            return GetEntityObject<PdfStream>(xobjectDic?.GetValue(name));
        }


        PdfDictionary? GetPageDictionary(PdfDictionary pages, int pageIndex, int topPage)
        {
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
                        if (c == pageIndex) return pd;
                        c++;
                        break;
                    case "/Pages":
                        var dc = pd.GetInt("/Count");
                        if (pageIndex < dc + c)
                        {
                            return GetPageDictionary(pd, pageIndex, c);
                        }
                        c += dc;
                        break;
                }
            }
            return null;
        }

        PdfRectangle? GetRectFromPage(PdfDictionary pdfDic, string name, bool inheritable)
        {
            var a = pdfDic.GetValue<PdfArray>(name);
            if (a != null) return a.GetRectangle();
            if(!inheritable)    return null;
            var parent = GetEntityObject<PdfDictionary>(pdfDic.GetValue(name));
            if(parent == null) return null;
            return GetRectFromPage(parent, name, inheritable);
        }

        PdfNumber? GetNumberFromPage(PdfDictionary pdfDic, string name, bool inheritable)
        {
            var a = pdfDic.GetValue<PdfNumber>(name);
            if (a != null) return a;
            if (!inheritable) return null;
            var parent = GetEntityObject<PdfDictionary>(pdfDic.GetValue(name));
            if (parent == null) return null;
            return GetNumberFromPage(parent, name, inheritable);
        }


        PdfPageAttribute GetPageAttribute(PdfDictionary pdfDic)
        {
            PdfPageAttribute pdfPageAttribute = new();
            var mediaBox = GetRectFromPage(pdfDic, "/MediaBox", true);
            if (mediaBox == null) throw new Exception("The page has no MediaBox.");
            pdfPageAttribute.MediaBox = mediaBox;
            var cropBox = GetRectFromPage(pdfDic, "/CropBox", true);
            pdfPageAttribute.CropBox = cropBox ?? mediaBox.Copy();
            if (mediaBox == null) throw new Exception("The page has no MediaBox.");
            var rotate = GetNumberFromPage(pdfDic, "/Rotate", true);
            if (rotate != null) pdfPageAttribute.Rotate = rotate.IntValue;

            return pdfPageAttribute;
        }

        private PdfDictionary GetRootObject()
        {
            var obj = Trailer?.GetValue<PdfReference>("/Root");
            if (obj == null) throw new Exception("cannot find /Root");
            return GetEntityObject<PdfDictionary>(obj) ?? throw new Exception("cannot parse root object");
        }

        private PdfParser Parser
        {
            get
            {
                if (mParser == null) throw new Exception("parser is null. maybe document is not open.");
                return mParser;
            }
        }
        private PdfParser? mParser;
        private IListner? mListner;
        private Stream? mStream;
    }
}
