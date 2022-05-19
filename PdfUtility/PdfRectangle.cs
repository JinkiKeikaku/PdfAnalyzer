using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfUtility
{
    public class PdfRectangle
    {
        public double Left;
        public double Bottom;
        public double Right;
        public double Top;

        public PdfRectangle(double left, double bottom, double right, double top)
        {
            Left = left;
            Bottom = bottom;
            Right = right;
            Top = top;
        }

        public override bool Equals(object? obj)
        {
            return obj is PdfRectangle rectangle &&
                   Left == rectangle.Left &&
                   Bottom == rectangle.Bottom &&
                   Right == rectangle.Right &&
                   Top == rectangle.Top;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Left, Bottom, Right, Top);
        }
        public override string ToString()
        {
            return $"[{Left} {Bottom} {Right} {Top}]";
        }
    }
}
