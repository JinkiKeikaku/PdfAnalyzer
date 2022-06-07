using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfUtility
{
    public class PdfNumber : PdfObject
    {
        public double Number;
        public double DoubleValue
        {
            get { return Number; }
            set { Number = value; }
        }
        public int IntValue
        {
            get { return (int)Number; }
            set { Number = value; }
        }


        public PdfNumber(double number)
        {
            Number = number;
        }
        public PdfNumber(int number)
        {
            Number = number;
        }

        public override string ToString()
        {
            return Number.ToString();
        }

        public override bool Equals(object? obj)
        {
            return obj is PdfNumber number &&
                   Number == number.Number;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Number);
        }
    }
}
