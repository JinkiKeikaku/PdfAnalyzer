using PdfUtility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfAnalyzer.ListItem
{
    class PdfXrefListItem : TreeItem
    {
        public PdfXrefListItem(List<(int ObjectNumber, PdfObject Obj)> xrefs) : base("Xrefs", "Object table", $"Count={xrefs.Count}")
        {
            XrefList = xrefs;
            for (var i = 0; i < XrefList.Count; i++)
            {
                var child = PdfAnalyzeHelper.CreateItem(XrefList[i].Obj, $"[{XrefList[i].ObjectNumber}]");
                Children.Add(child);
            }
        }
        public List<(int ObjectNumber, PdfObject Obj)> XrefList;
    }

}
