using Aga.Controls.Tree;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfAnalyzer
{
    public class PdfModel : ITreeModel
    {
        public ObservableCollection<TreeItem> Datas { get => root.Children; }// = new();

        TreeItem root = new("","","");

        public IEnumerable GetChildren(object parent)
        {
            if (parent == null) parent = root;
            if (parent is not TreeItem t) throw new Exception("PdfModel.HasChildren parent is not TreeItem.");
            return t.Children;
        }

        public bool HasChildren(object parent)
        {
            if(parent is not TreeItem t) throw new Exception("PdfModel.HasChildren parent is not TreeItem.");
            return t.Children.Count > 0;
        }
    }
}
