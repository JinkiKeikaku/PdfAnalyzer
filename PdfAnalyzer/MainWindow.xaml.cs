﻿using Microsoft.Win32;
using PdfAnalyzer.TreeListView;
using PdfUtility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
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
            if (Properties.Settings.Default.IsUpgrade == false)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.IsUpgrade = true;
                Debug.WriteLine(this, "Upgraded");
            }
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
                Datas.Add(new TreeItem("File name", "", path));
                Datas.Add(new TreeItem("Pdf Version", "", doc.PdfVerson));
                var rootItem = CreateItem(root, "/Root");
                Datas.Add(MakeNode(rootItem));
                var xrefs = doc.GetXrefObjects();
                var xrefItem = new PdfXrefListItem(new PdfXrefList(xrefs));
                Datas.Add(MakeNode(xrefItem));
                mFileName = path;
            }
            catch (Exception e)
            {
                Utility.ShowErrorMessage(e.Message);
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
                PdfNumber => new PdfObjectItem(obj, name, "Number", obj?.ToString() ?? ""),
                PdfName => new PdfObjectItem(obj, name, "Name", obj?.ToString() ?? ""),
                PdfString => new PdfObjectItem(obj, name, "String", obj?.ToString() ?? ""),
                PdfHexString => new PdfObjectItem(obj, name, "HexString", obj?.ToString() ?? ""),
                PdfStream => new PdfObjectItem(obj, name, "Stream", obj?.ToString() ?? ""),
                _ => new PdfObjectItem(obj, name, "", obj?.ToString() ?? ""),
            };
        }

        private PdfObjectItem MakeNode(PdfObjectItem item)
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
            return item;
        }

        private void Part_Tree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Part_Tree.SelectedItem is PdfStreamDataItem s) OpenStreamData(s, OpenType.Auto);
            if (Part_Tree.SelectedItem is not PdfObjectItem item) return;
            if (item.PdfObject is PdfReference r) SelectXrefObject(r);
        }

        enum OpenType
        {
            Auto,
            ExtractBinary,
            ExtractText,
            JpegImage,
            Binary,
            Text,
        }

        private void Part_Tree_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var menu = new ContextMenu();
            if (Part_Tree.SelectedItem is PdfObjectItem item)
            {
                if (item.PdfObject is PdfReference r)
                {
                    var m1 = new MenuItem();
                    m1.Header = "Go to object";
                    m1.Click += (sender, e) =>
                    {
                        SelectXrefObject(r);
                    };
                    menu.Items.Add(m1);
                }
            }
            if (Part_Tree.SelectedItem is PdfStreamDataItem s)
            {
                var filter = s.Parent.Dictionary.GetValue<PdfName>("/Filter");
                switch (filter?.Name)
                {
                    case "/FlateDecode":
                        {
                            var m1 = new MenuItem();
                            m1.Header = "Extract and open as text";
                            m1.Click += (sender, e) =>
                            {
                                OpenStreamData(s, OpenType.ExtractText);
                            };
                            menu.Items.Add(m1);
                            var m2 = new MenuItem();
                            m2.Header = "Extract and open as binary";
                            m2.Click += (sender, e) =>
                            {
                                OpenStreamData(s, OpenType.ExtractBinary);
                            };
                            menu.Items.Add(m2);
                        }
                        break;
                    case "/DCTDecode":
                        {
                            var m1 = new MenuItem();
                            m1.Header = "Open as Image";
                            m1.Click += (sender, e) =>
                            {
                                OpenStreamData(s, OpenType.JpegImage);
                            };
                            menu.Items.Add(m1);
                            var m2 = new MenuItem();
                            m2.Header = "Open as binary";
                            m2.Click += (sender, e) =>
                            {
                                OpenStreamData(s, OpenType.Binary);
                            };
                            menu.Items.Add(m2);
                        }
                        break;
                    default:
                        {
                            var m1 = new MenuItem();
                            m1.Header = "Open as text";
                            m1.Click += (sender, e) =>
                            {
                                OpenStreamData(s, OpenType.Text);
                            };
                            menu.Items.Add(m1);
                            var m2 = new MenuItem();
                            m2.Header = "Open as binary";
                            m2.Click += (sender, e) =>
                            {
                                OpenStreamData(s, OpenType.Binary);
                            };
                            menu.Items.Add(m2);
                        }
                        break;
                }
            }
            if (menu.Items.Count > 0) menu.IsOpen = true;
        }

        private void OpenStreamData(PdfStreamDataItem item, OpenType ot)
        {
            var filter = item.Parent.Dictionary.GetValue<PdfName>("/Filter");
            if (ot == OpenType.Auto)
            {
                ot = filter?.Name switch
                {
                    "/FlateDecode" => OpenType.ExtractText,
                    "/DCTDecode" => OpenType.JpegImage,
                    _ => OpenType.Binary,
                };
            }
            try
            {
                var path = System.IO.Path.GetTempFileName();
                switch (ot)
                {
                    case OpenType.ExtractText:
                        {
                            var bytes = item.Parent.GetExtractedBytes();
                            if (bytes == null) return;
                            File.WriteAllText(path, Encoding.ASCII.GetString(bytes));
                            System.Diagnostics.Process.Start(Properties.Settings.Default.TextEditorPath, path);
                        }
                        break;
                    case OpenType.ExtractBinary:
                        {
                            var bytes = item.Parent.GetExtractedBytes();
                            if (bytes == null) return;
                            File.WriteAllBytes(path, bytes);
                            System.Diagnostics.Process.Start(Properties.Settings.Default.BinaryEditorPath, path);
                        }
                        break;
                    case OpenType.Text:
                        File.WriteAllText(path, Encoding.ASCII.GetString(item.Parent.Data));
                        System.Diagnostics.Process.Start(Properties.Settings.Default.TextEditorPath, path);
                        break;
                    case OpenType.Binary:
                        File.WriteAllBytes(path, item.Parent.Data);
                        System.Diagnostics.Process.Start(Properties.Settings.Default.BinaryEditorPath, path);
                        break;
                    case OpenType.JpegImage:
                        path += ".jpg";
                        File.WriteAllBytes(path, item.Parent.Data);
                        System.Diagnostics.Process.Start("Explorer", path);
                        break;
                }
            }
            catch (Exception e)
            {
                Utility.ShowErrorMessage(e.Message);
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveWindowBounds();
            var settings = Properties.Settings.Default;
            settings.Save();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RecoverWindowBounds();
        }

        private void Menu_Settings_Click(object sender, RoutedEventArgs e)
        {
            var w = new SettingsWindow();
            w.Owner = this;
            w.ShowDialog();
        }

        private void SaveWindowBounds()
        {
            var settings = Properties.Settings.Default;
            settings.WindowMaximized = WindowState == WindowState.Maximized;
            WindowState = WindowState.Normal; // 最大化解除
            settings.WindowLeft = Left;
            settings.WindowTop = Top;
            settings.WindowWidth = Width;
            settings.WindowHeight = Height;

            var sb = new StringBuilder();
            foreach (var t in Part_Tree.Columns)
            {
                sb.Append($"{t.ActualWidth.ToString()} ");
            }
            settings.HeaderSize = sb.ToString();
        }

        private void RecoverWindowBounds()
        {
            var settings = Properties.Settings.Default;
            // 左
            if (settings.WindowLeft >= 0 &&
                (settings.WindowLeft + settings.WindowWidth) < SystemParameters.VirtualScreenWidth) { Left = settings.WindowLeft; }
            // 上
            if (settings.WindowTop >= 0 &&
                (settings.WindowTop + settings.WindowHeight) < SystemParameters.VirtualScreenHeight) { Top = settings.WindowTop; }
            // 幅
            if (settings.WindowWidth > 0 &&
                settings.WindowWidth <= SystemParameters.WorkArea.Width) { Width = settings.WindowWidth; }
            // 高さ
            if (settings.WindowHeight > 0 &&
                settings.WindowHeight <= SystemParameters.WorkArea.Height) { Height = settings.WindowHeight; }
            // 最大化
            if (settings.WindowMaximized)
            {
                // ロード後に最大化
                Loaded += (o, e) => WindowState = WindowState.Maximized;
            }
            var s = settings.HeaderSize.Split(' ');
            if (s.Length > 0)
            {
                for (var i = 0; i < s.Length && i < Part_Tree.Columns.Count; i++)
                {
                    if (double.TryParse(s[i], out var w))
                    {
                        Part_Tree.Columns[i].Width = w;
                    }
                }
            }
        }
    }
}
