using PdfUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfAnalyzer.ListItem
{
    class PdfDictionaryItem : PdfObjectItem
    {
        public PdfDictionaryItem(PdfDictionary dict, string name) :
            base(dict, name, "Dictionary", $"Count={dict.Count}")
        {
            foreach (var d in dict)
            {
                var child = PdfAnalyzeHelper.CreateItem(d.Value, d.Key);
                Children.Add(child);
            }
        }
    }
}
