using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PdfAnalyzer
{
    static class Utility
    {
        public static void ShowErrorMessage(string message)
        {
            SystemSounds.Beep.Play();
            MessageBox.Show(message, "Error");
        }
    }
}
