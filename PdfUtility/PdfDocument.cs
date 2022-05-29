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
            RootPages = GetEntityObject<PdfDictionary>(Root.GetValue("/Pages"));
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
                var pd = GetEntityObject<PdfDictionary>(kid) ?? throw new Exception("kid is not dictionary.");
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


        public PdfObject? GetEntityObject(PdfObject? obj) {
            return Parser.GetEntityObject(obj);
        }

        public T? GetEntityObject<T>(PdfObject? obj) where T : PdfObject => GetEntityObject(obj) as T;


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

        public PdfPage GetPage(int pageNumber)
        {
            if (RootPages == null) throw new Exception("Root pages is null. maybe document is not open.");
            var pageDic = GetPageDictionary(RootPages, pageNumber, 0);
            if (pageDic == null) throw new Exception($"cannot find page {pageNumber}");
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
//            page.Fonts = GetFonts(page.ResourcesDictionary);

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

        public PdfStream? GetXObject(PdfDictionary resource, string name)
        {
            var xobjectDic = GetEntityObject<PdfDictionary>(resource.GetValue("/XObject"));
            return GetEntityObject<PdfStream>(xobjectDic?.GetValue(name));
        }

        private PdfDictionary GetRootObject()
        {
            var obj = Trailer?.GetValue<PdfReference>("/Root");
            if (obj == null) throw new Exception("cannot find /Root");
            return GetEntityObject<PdfDictionary>(obj) ?? throw new Exception("cannot parse root object");
        }
    }
}
