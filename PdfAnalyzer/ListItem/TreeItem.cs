using Aga.Controls.Tree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PdfAnalyzer.ListItem
{
    public class TreeItem
    {
        public TreeItem(string name, string typeName, string information)
        {
            Name = name;
            TypeName = typeName;
            Information = information;
        }

        public ObservableCollection<TreeItem> Children { get; } = new();
        public string Name { get; }
        public string TypeName { get; }
        public string Information { get; }

        /// <summary>
        /// ダブルクリックなどの処理
        /// </summary>
        public virtual void Open(TreeList t) { }

        public virtual List<MenuItem> CreateMenuItems(TreeList t) => new();

        /// <inheritdoc/>
        public override string ToString() => $"{Name} {TypeName} {Information}";

        protected MenuItem CreateMenuItem(string header, Action action)
        {
            var m = new MenuItem();
            m.Header = header;
            m.Click += (sender, e) =>
            {
                action();
            };
            return m;
        }
    }
}