using Microsoft.Win32;
using PdfAnalyzer.TreeListView;
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
                Datas.Add(new TreeItem("File name", path));
                Datas.Add(new TreeItem("Pdf Version", doc.PdfVerson));
                var rootItem = CreateItem(root, "/Root");
                MakeNode(rootItem);
                Datas.Add(rootItem);
                var xrefs = doc.GetXrefObjects();
                var xrefObj = new PdfXrefList(xrefs);
                var xrefItem = new PdfXrefListItem(xrefObj);
                MakeNode(xrefItem);
                Datas.Add(xrefItem);
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
            return obj switch
            {
                PdfDictionary d => new PdfDictionaryItem(d, name),
                PdfArray a => new PdfArrayItem(a, name),
                PdfReference r => new PdfReferenceItem(r, name),
                _ => new PdfObjectItem(obj, name, obj?.ToString()??""),
            };
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
                        var sd = new PdfStreamDataItem(s);
                        item.Children.Add(sd);
                        foreach (var d in s.Dictionary)
                        {
                            var child = CreateItem(d.Value, d.Key);
                            item.Children.Add(child);
                            MakeNode(child);
                        }
                    }
                    break;
                case PdfXrefList xrefs:
                    {
                        for (var i = 0; i < xrefs.Count; i++)
                        {
                            var child = CreateItem(xrefs[i].Obj, $"[{xrefs[i].ObjectNumber}]");
                            item.Children.Add(child);
                            MakeNode(child);
                        }
                    }
                    break;
            }

        }

        private void Part_Tree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Part_Tree.SelectedItem is not PdfObjectItem item) return;
            if (item.PdfObject is PdfReference r) SelectXrefObject(r);
        }


        private void Part_Tree_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (Part_Tree.SelectedItem is not PdfObjectItem item) return;
            if (item.PdfObject is PdfReference r)
            {
                var menu = new ContextMenu();
                var m1 = new MenuItem();
                m1.Header = "Go to object";
                m1.Click += (sender, e) =>
                {
                    SelectXrefObject(r);
                };
                menu.Items.Add(m1);
                menu.IsOpen = true;
            }
        }


        private void SelectXrefObject(PdfReference r)
        {
            var x = VisualTreeExt.GetDescendants<TreeViewItem>(Part_Tree).FirstOrDefault(tvi =>
            {
                return tvi.DataContext is PdfXrefListItem;
            });
            if (x?.DataContext is not PdfXrefListItem px) return;
            x.IsExpanded = true;
            int n = px.XrefList.Count;
            for (var i = 0; i < n; i++)
            {
                if (px.XrefList[i].ObjectNumber == r.ObjectNumber)
                {
                    x = VisualTreeExt.GetDescendants<TreeViewItem>(Part_Tree).FirstOrDefault(tvi =>
                    {
                        return tvi.DataContext is PdfObjectItem pi && pi.PdfObject == px.XrefList[i].Obj;
                    });
                    if (x != null) x.IsSelected = true;
                }
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
