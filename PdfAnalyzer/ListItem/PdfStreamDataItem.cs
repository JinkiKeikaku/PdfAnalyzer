using Aga.Controls.Tree;
using PdfUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PdfAnalyzer.ListItem
{
    class PdfStreamDataItem : TreeItem
    {
        public PdfStreamDataItem(PdfStream parent) :
            base("Data", "Bytes", $"Length={parent.Data.Length}")
        {
            Parent = parent;
        }
        public PdfStream Parent { get; }
        public override void Open(TreeList t)
        {
            PdfAnalyzeHelper.OpenStreamData(this, PdfAnalyzeHelper.OpenType.Auto);
        }
        public override List<MenuItem> CreateMenuItems(TreeList t)
        {
            var menuList = new List<MenuItem>();
            var filter = Parent.Dictionary.GetValue<PdfName>("/Filter");
            var subType = Parent.Dictionary.GetValue<PdfName>("/Subtype");

            switch (filter?.Name)
            {
                case "/FlateDecode":
                    {
                        menuList.Add(CreateMenuItem("Extract and open as text", () =>
                        {
                            PdfAnalyzeHelper.OpenStreamData(this, PdfAnalyzeHelper.OpenType.ExtractText);
                        }));
                        menuList.Add(CreateMenuItem("Extract and open as binary", () =>
                        {
                            PdfAnalyzeHelper.OpenStreamData(this, PdfAnalyzeHelper.OpenType.ExtractBinary);
                        }));
                        menuList.Add(CreateMenuItem("Open as binary", () =>
                        {
                            PdfAnalyzeHelper.OpenStreamData(this, PdfAnalyzeHelper.OpenType.Binary);
                        }));

                        if (subType == "/Image")
                        {
                            menuList.Add(CreateMenuItem("Extract and open as image", () =>
                            {
                                PdfAnalyzeHelper.OpenStreamData(this, PdfAnalyzeHelper.OpenType.BinaryImage);
                            }));
                        }
                    }
                    break;
                case "/DCTDecode":
                    {
                        menuList.Add(CreateMenuItem("Open as Image", () =>
                        {
                            PdfAnalyzeHelper.OpenStreamData(this, PdfAnalyzeHelper.OpenType.JpegImage);
                        }));
                        menuList.Add(CreateMenuItem("Open as binary", () =>
                        {
                            PdfAnalyzeHelper.OpenStreamData(this, PdfAnalyzeHelper.OpenType.Binary);
                        }));
                    }
                    break;
                default:
                    {
                        menuList.Add(CreateMenuItem("Open as text", () =>
                        {
                            PdfAnalyzeHelper.OpenStreamData(this, PdfAnalyzeHelper.OpenType.Text);
                        }));
                        menuList.Add(CreateMenuItem("Open as binary", () =>
                        {
                            PdfAnalyzeHelper.OpenStreamData(this, PdfAnalyzeHelper.OpenType.Binary);
                        }));
                    }
                    break;
            }
            return menuList;
        }

    }
}
