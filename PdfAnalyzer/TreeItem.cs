﻿using System;
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
        virtual public string Name { get; }
        virtual public string Information { get; }

        public TreeItem(string name, string information)
        {
            Name = name;
            Information = information;
        }
    }
}