using PdfAnalyzer.TreeListView;
using PdfUtility;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Controls;

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
                PdfNumber => new PdfObjectItem(obj, name, "Number", obj?.ToString() ?? ""),
                PdfName => new PdfObjectItem(obj, name, "Name", obj?.ToString() ?? ""),
                PdfString => new PdfObjectItem(obj, name, "String", obj?.ToString() ?? ""),
                PdfHexString => new PdfObjectItem(obj, name, "HexString", obj?.ToString() ?? ""),
                PdfStream => new PdfObjectItem(obj, name, "Stream", obj?.ToString() ?? ""),
                _ => new PdfObjectItem(obj, name, "", obj?.ToString() ?? ""),
            };
        }
        public static  PdfObjectItem MakeNode(PdfObjectItem item)
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
                            if(width != null && height != null)
                            {
                                var bmp = CtreateImageFromRawArray(bytes, width.IntValue, height.IntValue);
                                bmp.Save(path);
//                                File.WriteAllBytes(path, item.Parent.Data);
                                Process.Start("Explorer", path);
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

        /// <summary>
        /// 参照されたXrefオブジェクトのTreeListViewのアイテムを選択する。
        /// </summary>
        public static void SelectXrefObject(TreeView treeView, PdfReference r)
        {
            var x = VisualTreeExt.GetDescendants<TreeViewItem>(treeView).FirstOrDefault(tvi =>
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
                    x = VisualTreeExt.GetDescendants<TreeViewItem>(treeView).FirstOrDefault(tvi =>
                    {
                        return tvi.DataContext is PdfObjectItem pi && pi.PdfObject == px.XrefList[i].Obj;
                    });
                    if (x != null) x.IsSelected = true;
                }
            }
        }

        /// <summary>
        /// TreeListViewの選択位置のコンテキストメニューを作成する。
        /// </summary>
        /// <param name="treeView"></param>
        /// <returns></returns>
        public static ContextMenu CreateTreeViewContectMenu(TreeView treeView)
        {
            var menu = new ContextMenu();
            if (treeView.SelectedItem is PdfObjectItem item)
            {
                if (item.PdfObject is PdfReference r)
                {
                    var m1 = new MenuItem();
                    m1.Header = "Go to object";
                    m1.Click += (sender, e) =>
                    {
                        SelectXrefObject(treeView, r);
                    };
                    menu.Items.Add(m1);
                }
            }
            if (treeView.SelectedItem is PdfStreamDataItem s)
            {
                var filter = s.Parent.Dictionary.GetValue<PdfName>("/Filter");
                var subType = s.Parent.Dictionary.GetValue<PdfName>("/Subtype");

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
                            if(subType == "/Image")
                            {
                                var m3 = new MenuItem();
                                m3.Header = "Extract and open as image";
                                m3.Click += (sender, e) =>
                                {
                                    OpenStreamData(s, OpenType.BinaryImage);
                                };
                                menu.Items.Add(m3);
                            }

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
            return menu;
//            if (menu.Items.Count > 0) menu.IsOpen = true;
        }

            public static Bitmap CtreateImageFromRawArray(this byte[] arr, int width, int height)
            {
                var output = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                var rect = new Rectangle(0, 0, width, height);
                var bmpData = output.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, output.PixelFormat);
                var arrRowLength = width * Bitmap.GetPixelFormatSize(output.PixelFormat) / 8;
                var ptr = bmpData.Scan0;
                for (var i = 0; i < height; i++)
                {
                    Marshal.Copy(arr, i * arrRowLength, ptr, arrRowLength);
                    ptr += bmpData.Stride;
                }
                output.UnlockBits(bmpData);
                return output;
            }
    }
}
