using Aga.Controls.Tree;
using PdfUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfAnalyzer.ListItem
{
    class PdfStreamItem : PdfObjectItem
    {
        public PdfStreamItem(PdfStream obj, string name) : 
            base(obj, name, "Stream", obj?.ToString() ?? "")
        {
            var sd = new PdfStreamDataItem(obj);
            Children.Add(sd);
            foreach (var d in obj.Dictionary)
            {
                var child = PdfAnalyzeHelper.CreateItem(d.Value, d.Key);
                Children.Add(child);
            }
        }
    }
}
