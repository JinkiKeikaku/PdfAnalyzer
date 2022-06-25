using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfAnalyzer.ListItem
{
    class PdfPagesItem : TreeItem
    {
        public PdfPagesItem(int pageSize) : base("Pages", "", pageSize.ToString())
        {
        }

        public int PageSize { get; }
    }
}
