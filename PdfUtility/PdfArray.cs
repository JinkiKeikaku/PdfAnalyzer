using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfUtility
{
    /// <summary>
    /// PDFの配列オブジェクト
    /// </summary>
    public class PdfArray : PdfObject, IEnumerable<PdfObject>
    {

        public List<PdfObject> Elements => mElements;

        public PdfArray()
        {
        }

        public void Add(PdfObject obj)
        {
            mElements.Add(obj);
        }

        public int Count => mElements.Count;

        public PdfObject this[int index] => mElements[index];

        public T? GetAt<T>(int index) where T : PdfObject => mElements[index] as T;

        public PdfRectangle GetRectangle()
        {
            var x1 = GetAt<PdfNumber>(0)!.DoubleValue;
            var y1 = GetAt<PdfNumber>(1)!.DoubleValue;
            var x2 = GetAt<PdfNumber>(2)!.DoubleValue;
            var y2 = GetAt<PdfNumber>(3)!.DoubleValue;
            return new PdfRectangle(x1, y1, x2, y2);
        }


        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is PdfArray array &&
                   EqualityComparer<List<PdfObject>>.Default.Equals(Elements, array.Elements);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Elements);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append('[');
            foreach (var element in Elements)
            {
                sb.Append(element.ToString());
                sb.Append(' ');
            }
            sb.Append(']');
            return sb.ToString();
        }

        IEnumerator<PdfObject> IEnumerable<PdfObject>.GetEnumerator()
        {
            return mElements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return mElements.GetEnumerator();
        }
        private List<PdfObject> mElements = new();
    }
}
