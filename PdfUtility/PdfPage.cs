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
        public PdfRectangle CropBox {
            get{
                return  mCropBox ?? MediaBox; 
            }
            set
            {
                mCropBox = value;

            } 
        }
        public int Rotate { get; set; } = 0;

        private PdfRectangle? mCropBox = null;
    }

    public class PdfPage
    {
        public PdfPageAttribute Attribute = new();
        public PdfDictionary ResourcesDictionary { get; set; } = null!;
        public List<byte[]> ContentsList { get; set; } = new();
//        public List<PdfFont> Fonts { get; set; } = new();
    }

}
