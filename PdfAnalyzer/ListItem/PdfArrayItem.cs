using PdfUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfAnalyzer.ListItem
{
    class PdfArrayItem : PdfObjectItem
    {
        public PdfArrayItem(PdfArray array, string name) :
            base(array, name, "Array", $"Count={array.Count}")
        {
            for (var i = 0; i < array.Count; i++)
            {
                var child = PdfAnalyzeHelper.CreateItem(array[i], $"[{i}]");
                Children.Add(child);
            }
        }
    }
}
