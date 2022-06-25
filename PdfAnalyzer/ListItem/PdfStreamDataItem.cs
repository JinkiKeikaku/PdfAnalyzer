using PdfUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfAnalyzer.ListItem
{
    class PdfStreamDataItem : TreeItem
    {
        public PdfStreamDataItem(PdfStream parent) :
            base("Data", "Bytes", $"Length={parent.Data.Length}")
        {
            Parent = parent;
        }
        public PdfStream Parent { get; }
    }
}
