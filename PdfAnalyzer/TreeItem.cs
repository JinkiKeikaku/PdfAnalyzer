using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfAnalyzer
{
    public class TreeItem
    {
        public ObservableCollection<TreeItem> Children { get; } = new();
        public string Name { get; }
        public string TypeName { get; }
        public string Information { get; }

        public TreeItem(string name, string typeName, string information)
        {
            Name = name;
            TypeName = typeName;
            Information = information;
        }
    }
}