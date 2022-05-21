using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace PdfAnalyzer
{
    /// <summary>
    /// SettingsWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = this;
        }
        public string TextEditorPath { get; set; } = Properties.Settings.Default.TextEditorPath;
        public string BinaryEditorPath { get; set; } = Properties.Settings.Default.BinaryEditorPath;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.TextEditorPath = TextEditorPath;
            Properties.Settings.Default.BinaryEditorPath = BinaryEditorPath;
            Close();
        }

        private void Parts_SelectTextEditorPath_Click(object sender, RoutedEventArgs e)
        {
            var s = TextEditorPath;
            SelectPath(TextEditorPath, s => { 
                TextEditorPath = s;
                OnPropertyChanged(nameof(TextEditorPath));
            });
        }

        private void Parts_SelectBinaryEditorPath_Click(object sender, RoutedEventArgs e)
        {
            var s = BinaryEditorPath;
            SelectPath(TextEditorPath, s => {
                BinaryEditorPath = s;
                OnPropertyChanged(nameof(BinaryEditorPath));
            });
        }

        private void SelectPath(string path, Action<string> action)
        {
            var f = new OpenFileDialog
            {
                FileName = path,
                FilterIndex = 1,
                Filter = "Exe file(.exe)|*.exe|All files (*.*)|*.*",
            };

            if (f.ShowDialog(Application.Current.MainWindow) == true)
            {
                action(f.FileName);
            }
        }


        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
