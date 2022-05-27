using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfUtility
{
    public class PdfPageAttribute
    {
        public PdfRectangle MediaBox { get; set; } = new(0, 0, 0, 0);
        public PdfRectangle CropBox { get; set; } = new(0, 0, 0, 0);
        public int Rotate { get; set; } = 0;
    }

    public class PdfPage
    {
        public PdfPageAttribute Attribute = new();
        public PdfDictionary? ResourcesDictionary { get; set; }
        public List<byte[]> ContentsList { get; set; } = new();
        public List<PdfFont> Fonts { get; set; } = new();
    }

}
