using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfUtility
{
    public class PdfIdentifier : PdfObject
    {
        public string Identifier;

        public PdfIdentifier(string identifier)
        {
            Identifier = identifier;
        }

        public override bool Equals(object? obj)
        {
            return obj is PdfIdentifier id &&
                   Identifier == id.Identifier;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Identifier);
        }

        public override string ToString() => Identifier;


    }
}
