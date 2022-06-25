using PdfUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfAnalyzer.ListItem
{
    class PdfPageReferenceListItem : TreeItem
    {
        public PdfPageReferenceListItem(string name, List<PdfReference> pageList) : 
            base(name, "", $"Page size={pageList.Count}")
        {
            PageReferenceList = pageList;
            for (var i = 0; i < pageList.Count; i++)
            {
                var child = PdfAnalyzeHelper.CreateItem(pageList[i], $"[{i}]");
                Children.Add(child);
            }
        }

        public List<PdfReference> PageReferenceList;

    }
}
