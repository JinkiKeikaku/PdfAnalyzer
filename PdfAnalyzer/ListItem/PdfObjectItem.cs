using PdfUtility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfAnalyzer.ListItem
{
    class PdfObjectItem : TreeItem
    {
        public PdfObjectItem(PdfObject obj, string name, string typeName, string information) :
            base(name, typeName, information)
        {
            PdfObject = obj;
        }

        public PdfObject PdfObject { get; }
    }


}
