using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfUtility
{
    public class PdfName : PdfObject
    {
        public PdfName(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is PdfName name &&
                   Name == name.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name);
        }

        public static bool operator==(PdfName? x, string? y)
        {
            return x?.Name == y;
        }
        public static bool operator !=(PdfName? x, string? y)
        {
            return x?.Name != y;
        }
        public static bool operator ==(string? y, PdfName? x)
        {
            return x?.Name == y;
        }
        public static bool operator !=(string? y, PdfName? x)
        {
            return x?.Name != y;
        }

        public override string ToString() => Name;
    }
}
