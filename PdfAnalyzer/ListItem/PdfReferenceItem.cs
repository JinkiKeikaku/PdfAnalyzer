using Aga.Controls.Tree;
using PdfUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PdfAnalyzer.ListItem
{
    class PdfReferenceItem : PdfObjectItem
    {
        public PdfReferenceItem(PdfReference r, string name) :
            base(r, name, "Reference", r.ToString())
        {
        }
        public override void Open(TreeList t)
        {
            if (PdfObject is PdfReference r) PdfAnalyzeHelper.SelectXrefObject(t, r);
        }

        public override List<MenuItem> CreateMenuItems(TreeList t)
        {
            var m = new List<MenuItem>();
            var m1 = CreateMenuItem("Go to object", ()=>
            {
                if (PdfObject is PdfReference r) PdfAnalyzeHelper.SelectXrefObject(t, r);
            });
            m.Add(m1);
            return m;
        }
    }
}
