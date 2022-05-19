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
        private ObservableCollection<PdfNode> mDatas = new();
        public MainWindow()
        {
            InitializeComponent();

            Part_Tree.ItemsSource = mDatas;
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
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var doc = new PdfDocument();
                doc.Open(stream);
                var root = doc.Root;
                if (root == null) throw new Exception("Cannot get root dictionary.");
                PdfNode rootNode = new("/Root", root);
                MakeNode(rootNode);
                //foreach (var d in root)
                //{
                //    PdfNode node = new(d.Key, d.Value);
                //    rootNode.Children.Add(node);

                //}


                //root.Children.Add(new PdfItem("child1"));
                //root.Children.Add(new PdfItem("child2"));
                mDatas.Add(rootNode);


            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void MakeNode(PdfNode parentNode)
        {
            if (parentNode == null) return;
            if (parentNode.PdfObject is not PdfDictionary dic) return;
            foreach (var d in dic)
            {
                PdfNode node = new(d.Key, d.Value);
                parentNode.Children.Add(node);
                MakeNode(node);
            }
        }

        public class PdfNode
        {
            public PdfObject PdfObject { get; set; }

            public string Name { get; set; }
            public string DisplayString => ToString();
            public List<PdfNode> Children { get; set; } = new();
            public PdfNode(string name, PdfObject obj)
            {
                Name = name;
                PdfObject = obj;
            }

            public override string ToString()
            {
                return $"{Name}::{PdfObject.ToString()}";
            }
        }
    }
}
