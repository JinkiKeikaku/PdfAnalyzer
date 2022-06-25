using PdfUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfAnalyzer.ListItem
{
    class PdfReferenceItem : PdfObjectItem
    {
        public PdfReferenceItem(PdfReference r, string name) :
            base(r, name, "Reference", r.ToString())
        {
        }
    }
}
