using PdfUtility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfAnalyzer
{
    class PdfObjectItem : TreeItem
    {
        public PdfObject PdfObject { get; }
        public PdfObjectItem(PdfObject obj,  string name, string typeName, string information) : 
            base(name, typeName, information)
        {
            PdfObject = obj;
        }
    }

    class PdfDictionaryItem : PdfObjectItem
    {
        public PdfDictionaryItem(PdfDictionary dict, string name) : 
            base(dict, name, "Dictionary", $"Count={dict.Count}")
        {
        }
    }
    class PdfArrayItem : PdfObjectItem
    {
        public PdfArrayItem(PdfArray array, string name) : 
            base(array, name, "Array", $"Count={array.Count}")
        {
        }
    }

    class PdfReferenceItem : PdfObjectItem
    {
        public PdfReferenceItem(PdfReference r, string name) : 
            base(r, name, "Reference", r.ToString())
        {
        }
    }

    class PdfStreamDataItem : TreeItem
    {
        public PdfStream Parent { get; }

        public PdfStreamDataItem(PdfStream parent) : 
            base("Data", "Bytes", $"Length={parent.Data.Length}")
        {
            Parent = parent;
        }
    }


    class PdfXrefList : PdfObject, IEnumerable<(int ObjectNumber, PdfObject Obj)>
    {
        public List<(int ObjectNumber, PdfObject Obj)> Xrefs { get; } = new();
        public int Count => Xrefs.Count;
        public (int ObjectNumber, PdfObject Obj) this[int index] => Xrefs[index];
        public PdfXrefList(List<(int ObjectNumber, PdfObject Obj)> xrefs) : base()
        {
            Xrefs = xrefs;
        }
        IEnumerator<(int ObjectNumber, PdfObject Obj)> IEnumerable<(int ObjectNumber, PdfObject Obj)>.GetEnumerator()
        {
            return Xrefs.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Xrefs.GetEnumerator();
        }

    }

    class PdfXrefListItem : PdfObjectItem
    {
        public PdfXrefList XrefList { get; }
        public PdfXrefListItem(PdfXrefList xrefList) : base(xrefList, "Xrefs","Object table", $"Count={xrefList.Count}")
        {
            XrefList = xrefList;
        }
    }

}
