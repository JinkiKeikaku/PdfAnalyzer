using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfUtility
{
    static class PngPredictor
    {
        public static byte[] Predict(byte[] src, int numColumn, int colors)
        {
            var numRow = src.Length / (numColumn + 1);
            var dst = new byte[numRow * numColumn];
            for (var y = 0; y < numRow; y++)
            {
                var srcTop = y * (numColumn + 1);
                var dstTop = y * numColumn;
                switch (src[srcTop])
                {
                    case 0:
                        Array.Copy(src, srcTop + 1, dst, dstTop, numColumn);
                        break;
                    case 1:
                        {
                            for (var i = 0; i < colors; i++)
                            {
                                dst[dstTop + i] = src[srcTop + 1 + i];
                            }
                            for (var x = colors; x < numColumn; x += colors)
                            {
                                for (var i = 0; i < colors; i++)
                                {
                                    dst[dstTop + x + i] = (byte)(dst[dstTop + x - colors + i] +
                                    src[srcTop + 1 + x + i]);
                                }
                            }
                        }
                        break;
                    case 2:
                        {
                            if (y == 0)
                            {
                                Array.Copy(src, srcTop + 1, dst, dstTop, numColumn);
                            }
                            else
                            {
                                for (var x = 0; x < numColumn; x++)
                                {
                                    dst[dstTop + x] = (byte)(dst[dstTop + x - numColumn] +
                                        src[srcTop + 1 + x]);
                                }
                            }
                        }
                        break;
                    case 3:
                        {
                            for (var x = 0; x < numColumn; x += colors)
                            {
                                for (var i = 0; i < colors; i++)
                                {
                                    var p = y == 0 ? 0 : dst[dstTop + x - numColumn + i];
                                    var b = x == 0 ? 0 : src[srcTop + x + 1 - colors + i];
                                    dst[dstTop + x + i] = (byte)(src[srcTop + x + 1 + i] +
                                        (p + b) / 2);
                                }
                            }
                        }
                        break;
                    case 4:
                        {
                            for (var x = 0; x < numColumn; x += colors)
                            {
                                for (var i = 0; i < colors; i++)
                                {
                                    var b = x == 0 ? 0 : dst[dstTop + x - colors + i];
                                    var p = y == 0 ? 0 : dst[dstTop + x - numColumn + i];
                                    var pb = (y == 0 || x == 0) ? 0 : dst[dstTop + x - numColumn - colors + i];
                                    dst[dstTop + x + i] = (byte)(src[srcTop + x + 1 + i] +
                                        PaethPredictor(b, p, pb));
                                }
                            }
                        }
                        break;
                }
            }
            return dst;
        }

        private static int PaethPredictor(int a, int b, int c)
        {
            var p = a + b - c;
            var pa = Math.Abs(p - a);
            var pb = Math.Abs(p - b);
            var pc = Math.Abs(p - c);
            if (pa <= pb && pa <= pc) return a;
            else if (pb <= pc) return b;
            else return c;
        }

    }
}
