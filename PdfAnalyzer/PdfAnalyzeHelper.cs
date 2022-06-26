using Aga.Controls.Tree;
using PdfAnalyzer.ListItem;
using PdfUtility;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PdfAnalyzer
{
    static class PdfAnalyzeHelper
    {
        public enum OpenType
        {
            Auto,
            ExtractBinary,
            ExtractText,
            JpegImage,
            Binary,
            Text,
            BinaryImage,
        }

        public static PdfObjectItem CreateItem(PdfObject obj, string name)
        {
            return obj switch
            {
                PdfDictionary d => new PdfDictionaryItem(d, name),
                PdfArray a => new PdfArrayItem(a, name),
                PdfReference r => new PdfReferenceItem(r, name),
                PdfStream s => new PdfStreamItem(s, name),
                PdfNumber => new PdfObjectItem(obj, name, "Number", obj?.ToString() ?? ""),
                PdfName => new PdfObjectItem(obj, name, "Name", obj?.ToString() ?? ""),
                PdfString => new PdfObjectItem(obj, name, "String", obj?.ToString() ?? ""),
                PdfHexString => new PdfObjectItem(obj, name, "HexString", obj?.ToString() ?? ""),
                _ => new PdfObjectItem(obj, name, "", obj?.ToString() ?? ""),
            };
        }

        public static void OpenStreamData(PdfStreamDataItem item, OpenType ot)
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
                            Process.Start(Properties.Settings.Default.TextEditorPath, path);
                        }
                        break;
                    case OpenType.ExtractBinary:
                        {
                            var bytes = item.Parent.GetExtractedBytes();
                            if (bytes == null) return;
                            File.WriteAllBytes(path, bytes);
                            Process.Start(Properties.Settings.Default.BinaryEditorPath, path);
                        }
                        break;
                    case OpenType.Text:
                        File.WriteAllText(path, Encoding.ASCII.GetString(item.Parent.Data));
                        Process.Start(Properties.Settings.Default.TextEditorPath, path);
                        break;
                    case OpenType.Binary:
                        File.WriteAllBytes(path, item.Parent.Data);
                        Process.Start(Properties.Settings.Default.BinaryEditorPath, path);
                        break;
                    case OpenType.JpegImage:
                        path += ".jpg";
                        File.WriteAllBytes(path, item.Parent.Data);
                        Process.Start("Explorer", path);
                        break;
                    case OpenType.BinaryImage:
                        {
                            path += ".bmp";
                            var bytes = item.Parent.GetExtractedBytes();
                            if (bytes == null) return;
                            var width = item.Parent.Dictionary.GetValue<PdfNumber>("/Width");
                            var height = item.Parent.Dictionary.GetValue<PdfNumber>("/Height");
                            var colorSpace = item.Parent.Dictionary.GetValue("/ColorSpace");
                            if (width != null && height != null)
                            {
                                switch (colorSpace) {
                                    case PdfName csName:
                                        {
                                            OpenImage(bytes, width.IntValue, height.IntValue, path, csName);
                                        }
                                        break;
                                    case PdfArray csArray:
                                        {
                                            throw new Exception("ColorSpace: not support Array.");
                                            //if (csArray[0] is not PdfName csName) throw new Exception("colorSpace [0] is not name.");
                                            //if(csName != "/ICCBased") throw new Exception("ColorSpace only support ICCBased.");
                                            //var dic = doc.GetEntityObject<PdfDictionary>(csArray[1]) ??
                                            //    throw new Exception($"Cannot find ColorSpace");
                                            //var alt = dic.GetValue<PdfName>("/Alternate") ??
                                            //    throw new Exception($"not contains /Alternate color space name");
                                            //OpenImage(bytes, width.IntValue, height.IntValue, path, alt.Name);
                                        }
                                    default:
                                        throw new Exception("ColorSpace: unknown object.");
                                }
                            }
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                Utility.ShowErrorMessage(e.Message);
            }
        }

        static void OpenImage(byte[] bytes, int width, int height, string path, PdfName colorName)
        {
            var cs = colorName.Name switch
            {
                "/DeviceRGB" => ImageType.RGB,
                "/DeviceCMYK" => ImageType.CMYK,
                "/DeviceGray" => ImageType.Gray,
                _ => throw new NotImplementedException($"Not supported image {colorName.Name}")
            };
            var bmp = CtreateImageFromRaw(bytes, width, height, cs);
            bmp.Save(path);
            Process.Start("Explorer", path);
        }

        /// <summary>
        /// 参照されたXrefオブジェクトのTreeListViewのアイテムを選択する。
        /// </summary>
        public static void SelectXrefObject(TreeList treeView, PdfReference r)
        {
            var nodeXrefList = treeView.Nodes[0].Nodes.FirstOrDefault(x => x.Tag is PdfXrefListItem);
            if (nodeXrefList?.Tag is not PdfXrefListItem px) return;
            nodeXrefList.IsExpanded = true;
            int n = px.XrefList.Count;
            for (var i = 0; i < n; i++)
            {
                if (px.XrefList[i].ObjectNumber == r.ObjectNumber)
                {
                    var x = nodeXrefList.Nodes.FirstOrDefault(x => x.Tag is PdfObjectItem pi && pi.PdfObject == px.XrefList[i].Obj);
                    if (x != null)
                    {
                        var a = treeView.Items.IndexOf(x);
                        x.IsExpanded = true;
                        if(a >=0) treeView.SelectedIndex = a;
                        treeView.ScrollIntoView(x);
                    }



                }
            }
        }


        public enum ImageType
        {
            BGR,    //Windows
            RGB,    //PNG
            CMYK,    //CMYK
            Gray,   //8bit
            ICCBased,
        }

        public static Bitmap CtreateImageFromRaw(byte[] src, int width, int height, ImageType imageType)
        {
            switch (imageType)
            {
                case ImageType.BGR:
                    return CtreateImageFromBGR(src, width, height);
                case ImageType.RGB:
                    return CtreateImageFromRGB(src, width, height);
                case ImageType.CMYK:
                    return CtreateImageFromCMYK(src, width, height);
                case ImageType.Gray:
                    return CtreateImageFromGray(src, width, height);
            }
            throw new Exception($"CtreateImageFromRaw Unknown image type {imageType}");
        }

        private static Bitmap CtreateImageFromRGB(byte[] src, int width, int height) {
            var bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var rect = new Rectangle(0, 0, width, height);
            var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
            var arrRowLength = bmpData.Stride;
            var ptr = bmpData.Scan0;
            var buf = new byte[arrRowLength];
            var w3 = width * 3;
            for (var y = 0; y < height; y++)
            {
                var srcTop = y * w3;
                for (var x = 0; x < w3; x += 3)
                {
                    buf[x] = src[srcTop + x + 2];
                    buf[x + 1] = src[srcTop + x + 1];
                    buf[x + 2] = src[srcTop + x];
                }
                Marshal.Copy(buf, 0, ptr, arrRowLength);
                ptr += arrRowLength;
            }
            bmp.UnlockBits(bmpData);
            return bmp;
        }
        private static Bitmap CtreateImageFromBGR(byte[] src, int width, int height)
        {
            var bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var rect = new Rectangle(0, 0, width, height);
            var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
            var arrRowLength = bmpData.Stride;
            var ptr = bmpData.Scan0;
            var buf = new byte[arrRowLength];
            var w3 = width * 3;
            for (var y = 0; y < height; y++)
            {
                var srcTop = y * w3;
                for (var x = 0; x < w3; x += 3)
                {
                    buf[x] = src[srcTop + x];
                    buf[x + 1] = src[srcTop + x + 1];
                    buf[x + 2] = src[srcTop + x + 2];
                }
                Marshal.Copy(buf, 0, ptr, arrRowLength);
                ptr += arrRowLength;
            }
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        private static Bitmap CtreateImageFromCMYK(byte[] src, int width, int height)
        {
            var bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var rect = new Rectangle(0, 0, width, height);
            var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
            var arrRowLength = bmpData.Stride;
            var ptr = bmpData.Scan0;
            var buf = new byte[arrRowLength];
            for (var y = 0; y < height; y++)
            {
                var x3 = 0;
                var x4 = 0;
                var srcTop = y * width*4;
                for (var x = 0; x < width; x++)
                {
                    (buf[x3+2], buf[x3 + 1], buf[x3]) = CmykToRgb(src[srcTop + x4], src[srcTop + x4 + 1], src[srcTop + x4 + 2], src[srcTop + x4 + 3]);
                    x3 += 3;
                    x4 += 4;
                }
                Marshal.Copy(buf, 0, ptr, arrRowLength);
                ptr += arrRowLength;
            }
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        private static Bitmap CtreateImageFromGray(byte[] src, int width, int height)
        {
            var bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var rect = new Rectangle(0, 0, width, height);
            var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
            var arrRowLength = bmpData.Stride;
            var ptr = bmpData.Scan0;
            var buf = new byte[arrRowLength];
            for (var y = 0; y < height; y++)
            {
                var srcTop = y * width;
                for (var x = 0; x < width; x++)
                {
                    var c = src[srcTop + x];
                    var x3 = x * 3;
                    buf[x3] = c;
                    buf[x3 + 1] = c;
                    buf[x3 + 2] = c;
                }
                Marshal.Copy(buf, 0, ptr, arrRowLength);
                ptr += arrRowLength;
            }
            bmp.UnlockBits(bmpData);
            return bmp;
        }
        private static (byte r, byte g, byte b) CmykToRgb(byte c, byte m, byte y, byte k)
        {
            var r = (byte)((255 - c) * (255 - k) / 255);
            var g = (byte)((255 - m) * (255 - k) / 255);
            var b = (byte)((255 - y) * (255 - k) / 255);
            return (r, g, b);

        }
    }
}
