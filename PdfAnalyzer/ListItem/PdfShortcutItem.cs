using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfAnalyzer.ListItem
{
    class PdfShortcutItem : TreeItem
    {
        public PdfShortcutItem(string name) : base(name, "", "")
        {
        }
        public List<TreeItem> ShortcutList { get; } = new();
        public void AddShortcut(TreeItem item)
        {
            Children.Add(item);
        }
    }
}
