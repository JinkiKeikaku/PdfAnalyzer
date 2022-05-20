using Microsoft.Win32;
using PdfUtility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PdfAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<TreeItem> Datas { get; } = new();

        private string mFileName;
        private string mPdfVersion;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Menu_Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Menu_Open_Click(object sender, RoutedEventArgs e)
        {
            //mDatas[0].Children.Add(new PdfItem("aaa"));

            var f = new OpenFileDialog
            {
                FileName = "",
                FilterIndex = 1,
                Filter = "PDF file(.pdf)|*.pdf|All files (*.*)|*.*",
            };

            if (f.ShowDialog(Application.Current.MainWindow) == true)
            {
                OpenPdf(f.FileName);
            }

        }

        private void OpenPdf(string path)
        {
            try
            {
                Datas.Clear();
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var doc = new PdfDocument();
                doc.Open(stream);
                var root = doc.Root;
                if (root == null) throw new Exception("Cannot get root dictionary.");
                var rootItem = CreateItem(root, "/Root");
                MakeNode(rootItem);
                Datas.Add(rootItem);
                var x0 = doc.GetXrefObjects();

                var xrefs = new PdfDictionary();
                foreach(var x in x0)
                {
                    xrefs.Add()
                    var node = CreateItem(x.obj, $"Xref({x.objectNumber})");
                    MakeNode(node);
                    Datas.Add(node);
                }
                mFileName = path;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                mFileName = "";
            }
        }

        private PdfObjectItem CreateItem(PdfObject obj, string name)
        {
            switch (obj)
            {
                case PdfDictionary d:
                    {
                        return new PdfDictionaryItem(d, name);
                    }
                case PdfArray a:
                    {
                        return new PdfArrayItem(a, name);
                    }
            }
            return new PdfObjectItem(obj, name, obj.ToString());
        }

        private void MakeNode(PdfObjectItem item)
        {
            switch (item.PdfObject)
            {
                case PdfDictionary dic:
                    {
                        foreach (var d in dic)
                        {
                            var child = CreateItem(d.Value, d.Key);
                            item.Children.Add(child);
                            MakeNode(child);
                        }
                    }
                    break;
                case PdfArray array:
                    {
                        for (var i = 0; i < array.Count; i++)
                        {
                            var child = CreateItem(array[i], $"[{i}]");
                            item.Children.Add(child);
                            MakeNode(child);
                        }
                        break;
                    }
                case PdfStream s:
                    {
                        foreach (var d in s.Dictionary)
                        {
                            var child = CreateItem(d.Value, d.Key);
                            item.Children.Add(child);
                            MakeNode(child);
                        }
                    }
                    break;
            }

        }



        //public class PdfNode
        //{
        //    public PdfObject PdfObject { get; set; }

        //    public string Name { get; set; }
        //    public string Information {
        //        get
        //        {
        //            switch (PdfObject)
        //            {
        //                case PdfDictionary:
        //                    return "<<...>>";
        //                case PdfArray:
        //                    return "[...]";
        //            }
        //            return PdfObject?.ToString() ?? "";
        //        }
        //    }
        //    public ObservableCollection<PdfNode> Children { get; set; } = new();
        //    public PdfNode(string name, PdfObject obj)
        //    {
        //        Name = name;
        //        PdfObject = obj;
        //    }
        //}

    }
}
