using PdfUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfAnalyzer
{
    class PdfObjectItem : TreeItem
    {
        public PdfObject PdfObject { get; }
        public PdfObjectItem(PdfObject obj,  string name, string information) : base(name, information)
        {
            PdfObject = obj;
        }
    }

    class PdfDictionaryItem : PdfObjectItem
    {
        public PdfDictionaryItem(PdfDictionary dict, string name) : base(dict, name, "Dictionary")
        {
        }
    }
    class PdfArrayItem : PdfObjectItem
    {
        public PdfArrayItem(PdfArray array, string name) : base(array, name, "Array")
        {
        }
    }

    //class PdfXrefList : PdfObject
    //{
    //    public List<PdfObject> Elements { get; } = new();

    //}
}
