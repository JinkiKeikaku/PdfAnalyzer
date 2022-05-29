using Microsoft.Win32;
using PdfUtility;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace PdfAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

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
            //var r = new TreeItem("root", "testtype", "info");
            //Datas.Add(r);
            //for (int i = 0; i < 6000; i++)
            //{
            //    r.Children.Add(new TreeItem(i.ToString(), "testtype", "info"));
            //}
        }

        public ObservableCollection<TreeItem> Datas { get; } = new();

        private void Menu_Exit_Click(object sender, RoutedEventArgs e) => Close();

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

                //Treeを作成。
                Datas.Add(new TreeItem("File name", "", path));
                Datas.Add(new TreeItem("Pdf Version", "", doc.PdfVerson));
                var traliler = PdfAnalyzeHelper.CreateItem(doc.Trailer!, "Trailer");
                Datas.Add(PdfAnalyzeHelper.MakeNode(traliler));
                var root = doc.Root;
                if (root == null) throw new Exception("Cannot get root dictionary.");
                var rootItem = PdfAnalyzeHelper.CreateItem(root, "/Root");
                Datas.Add(PdfAnalyzeHelper.MakeNode(rootItem));
                var xrefs = doc.GetXrefObjects();
                var xrefItem = new PdfXrefListItem(new PdfXrefList(xrefs));
                Datas.Add(PdfAnalyzeHelper.MakeNode(xrefItem));
            }
            catch (Exception e)
            {
                Utility.ShowErrorMessage(e.Message);
            }
        }

        //ダブルクリックの処理
        private void Part_Tree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //ストリームデータは開く。
            if (Part_Tree.SelectedItem is PdfStreamDataItem s) PdfAnalyzeHelper.OpenStreamData(s, PdfAnalyzeHelper.OpenType.Auto);
            if (Part_Tree.SelectedItem is not PdfObjectItem item) return;
            //参照は対応するXrefを選択する。
            if (item.PdfObject is PdfReference r) PdfAnalyzeHelper.SelectXrefObject(Part_Tree, r);
        }

        //右ボタンUPでコンテキストメニュー
        private void Part_Tree_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var menu = PdfAnalyzeHelper.CreateTreeViewContectMenu(Part_Tree);
            if (menu.Items.Count > 0) menu.IsOpen = true;
        }

        private void Window_Drop(object sender, System.Windows.DragEventArgs e)
        {
            Debug.WriteLine("Window_Drop");
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop, true))
            {
                if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] files)
                {
                    OpenPdf(files[0]);
                }
            }
        }

        private void Window_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
        {
            Debug.WriteLine("Window_PreviewDragOver");
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop, true))
            {
                e.Effects = System.Windows.DragDropEffects.None;
                if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] files)
                {
                    // TODO Check acceptable.
                    {
                        e.Effects = System.Windows.DragDropEffects.Copy;
                        e.Handled = true;
                        return;
                    }
                }
            }
        }

        private void Menu_Settings_Click(object sender, RoutedEventArgs e)
        {
            var w = new SettingsWindow();
            w.Owner = this;
            w.ShowDialog();
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


        private void SaveWindowBounds()
        {
            var settings = Properties.Settings.Default;
            settings.WindowMaximized = WindowState == WindowState.Maximized;
            WindowState = WindowState.Normal; // 最大化解除
            settings.WindowLeft = Left;
            settings.WindowTop = Top;
            settings.WindowWidth = Width;
            settings.WindowHeight = Height;

            //var sb = new StringBuilder();
            //foreach (var t in Part_Tree.Columns)
            //{
            //    sb.Append($"{t.ActualWidth.ToString()} ");
            //}
            //settings.HeaderSize = sb.ToString();
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
            //var s = settings.HeaderSize.Split(' ');
            //if (s.Length > 0)
            //{
            //    for (var i = 0; i < s.Length && i < Part_Tree.Columns.Count; i++)
            //    {
            //        if (double.TryParse(s[i], out var w))
            //        {
            //            Part_Tree.Columns[i].Width = w;
            //        }
            //    }
            //}
        }

    }
}
