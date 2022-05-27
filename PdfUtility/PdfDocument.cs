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
    public class PdfDocument
    {

        public interface IListner
        {
            string OnRequestPassword();
            void OnPasswordError();
        }
        private PdfParser? mParser;
        private IListner? mListner;

        public PdfDocument(IListner? listner = null)
        {
            mListner = listner;
        }


        public string PdfVerson { get; private set; } = "";
        public PdfDictionary? RootPages { get; private set; }
        public PdfDictionary? Root { get; private set; }
        public PdfDictionary? Trailer { get; private set; }

        private PdfParser Parser
        {
            get
            {
                if (mParser == null) throw new Exception("parser is null. maybe document is not open.");
                return mParser;
            }
        }

        public void Open(Stream stream)
        {
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
            RootPages = (mParser.GetEntityObject(Root.GetValue("/Pages")) as PdfDictionary)!;
        }

        public void Close()
        {
            mParser = null;
        }

        public List<(int ObjectNumber, PdfObject Obj)> GetXrefObjects()
        {
            return Parser.GetXReferenceObjects();
        }

        public void ParserGraphics(byte[] script, Func<List<PdfObject>, PdfDocument, bool> func)
        {
            var ret = new List<PdfObject>();

            using var ms = new MemoryStream(script);// Encoding.ASCII.GetBytes(script));
            var parser = mParser?.Clone(ms);
            if (parser == null) throw new Exception("Cannot create graohics parser.");
            while (true)
            {
                var obj = parser.ParseObject();
                if (obj == null) break;
                ret.Add(obj);
//                Debug.WriteLine(obj);
                if (obj is PdfIdentifier)
                {
                    if (!func(ret, this)) break;
                    ret.Clear();
                }
            }
        }


        public int GetPageSize() => RootPages?.GetInt("/Count") ?? 0;

        public PdfDictionary? GetPageDictionary(PdfDictionary pages, int pageNumber, int topPage)
        {
            var c = topPage;
            var kids = pages.GetValue<PdfArray>("/Kids");
            if (kids == null) throw new Exception("cannot find kids in pages dictionary");
            for (var i = 0; i < kids.Count; i++)
            {
                var kid = kids[i];
                if (Parser.GetEntityObject(kid) is not PdfDictionary pd) throw new Exception("kid is not dictionary.");
                switch (pd.GetValue<PdfName>("/Type")?.Name)
                {
                    case "/Page":
                        if (c == pageNumber) return pd;
                        c++;
                        break;
                    case "/Pages":
                        var dc = pd.GetInt("/Count");
                        if (pageNumber < dc + c)
                        {
                            return GetPageDictionary(pd, pageNumber, c);
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
            var parent = Parser.GetEntityObject(pdfDic.GetValue(name)) as PdfDictionary;
            if(parent == null) return null;
            return GetRectFromPage(parent, name, inheritable);
        }

        PdfPageAttribute GetPageAttribute(PdfDictionary pdfDic)
        {
            PdfPageAttribute pdfPageAttribute = new();
            var mediaBox = GetRectFromPage(pdfDic, "/MediaBox", true);
            if (mediaBox == null) throw new Exception("The page has no MediaBox.");
            pdfPageAttribute.MediaBox = mediaBox;
            return pdfPageAttribute;
        }

        public PdfPage GetPage(int pageNumber)
        {
            if (RootPages == null) throw new Exception("Root pages is null. maybe document is not open.");
            var pageDic = GetPageDictionary(RootPages, pageNumber, 0);
            if (pageDic == null) throw new Exception($"cannot find page {pageNumber}");
            var contents = Parser.GetEntityObject(pageDic.GetValue("/Contents"));
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
            var resources = Parser.GetEntityObject(pageDic.GetValue("/Resources"));
            page.ResourcesDictionary = resources as PdfDictionary;
            if(page.ResourcesDictionary != null)    page.Fonts = GetFonts(page.ResourcesDictionary);

            for (int i = 0; i < contentsArray.Count; i++)
            {
                if (Parser.GetEntityObject(contentsArray.GetAt<PdfObject>(i)) is PdfStream contentsStream)
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

        public List<PdfFont> GetFonts(PdfDictionary resource)
        {
            var fonts = new List<PdfFont>();
            var fontDic = mParser?.GetEntityObject(resource.GetValue("/Font")) as PdfDictionary;
            if (fontDic == null) return fonts;
            int n = fontDic.Count;
            for (var i = 0; i < n; i++)
            {
                var p = fontDic.GetAt(i);
                var name = p.Key;
                var dic = Parser.GetEntityObject(p.Value) as PdfDictionary;
                if (dic != null)
                {
                    var f = new PdfFont(name, dic);
                    f.InitMap(mParser!);
                    fonts.Add(f);
                }
            }
            return fonts;
        }

        private PdfDictionary GetRootObject()
        {
            var obj = Trailer?.GetValue<PdfReference>("/Root");
            if (obj == null) throw new Exception("cannot find /Root");
            if (Parser.GetEntityObject(obj) is not PdfDictionary root) throw new Exception("cannot parse root object");
            return root;
        }
    }
}
