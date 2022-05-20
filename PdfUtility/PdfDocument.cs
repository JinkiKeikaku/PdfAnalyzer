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
        private PdfParser? mParser;
        private PdfDictionary? mTrailer;

        public string PdfVerson { get; private set; } = "";
        public PdfDictionary? RootPages { get; private set; }
        public PdfDictionary? Root { get; private set; }

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
            var ver = PdfParser.GetPdfVersion(stream);
            if (ver == null) throw new Exception("Not pdf file.");
            PdfVerson = ver;
            mParser = new();
            mTrailer = mParser.Start(stream);
            Root = GetRootObject();
            if (Root == null) throw new Exception("cannot find root object.");
            RootPages = (mParser.GetEntityObject(Root.GetValue<PdfObject>("/Pages")) as PdfDictionary)!;
        }

        public void Close()
        {
            mParser = null;
        }

        public List<(int ObjectNumber, PdfObject Obj)> GetXrefObjects()
        {
            return Parser.GetXReferenceObjects();
        }

        public void ParserGraphics(byte[] script, Func<List<object>, PdfDocument, bool> func)
        {
            var ret = new List<object>();

            using var ms = new MemoryStream(script);// Encoding.ASCII.GetBytes(script));
            var parser = mParser?.Clone(ms);
            if (parser == null) throw new Exception("Cannot create graohics parser.");
            while (true)
            {
                var obj = parser.ParseObject();
                if (obj == null) break;
                ret.Add(obj);
                Debug.WriteLine(obj);
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
            if (a != null)
            {
                var x1 = a.GetAt<PdfNumber>(0)!.DoubleValue;
                var y1 = a.GetAt<PdfNumber>(1)!.DoubleValue;
                var x2 = a.GetAt<PdfNumber>(2)!.DoubleValue;
                var y2 = a.GetAt<PdfNumber>(3)!.DoubleValue;
                return new PdfRectangle(x1, y1, x2, y2);
            }
            if(!inheritable)    return null;
            var parent = Parser.GetEntityObject(pdfDic.GetValue<PdfObject>(name)) as PdfDictionary;
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
            var contents = Parser.GetEntityObject(pageDic.GetValue<PdfObject>("/Contents"));
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
            var resources = Parser.GetEntityObject(pageDic.GetValue<PdfObject>("/Resources"));
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
            var fontDic = mParser?.GetEntityObject(resource.GetValue<PdfObject>("/Font")) as PdfDictionary;
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
            var obj = mTrailer?.GetValue<PdfReference>("/Root");
            if (obj == null) throw new Exception("cannot find /Root");
            if (Parser.GetEntityObject(obj) is not PdfDictionary root) throw new Exception("cannot parse root object");
            return root;
        }
    }
}
