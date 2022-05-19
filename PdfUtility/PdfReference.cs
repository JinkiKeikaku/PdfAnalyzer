using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfUtility
{
    public class PdfReference : PdfObject
    {
        public int ObjectNumber;
        public int GenerationNumber;

        public PdfReference(int objectNumber, int generationNumber)
        {
            ObjectNumber = objectNumber;
            GenerationNumber = generationNumber;
        }

        public override bool Equals(object? obj)
        {
            return obj is PdfReference reference &&
                   ObjectNumber == reference.ObjectNumber &&
                   GenerationNumber == reference.GenerationNumber;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ObjectNumber, GenerationNumber);
        }

        public override string ToString()
        {
            return $"{ObjectNumber} {GenerationNumber} R";
        }
    }
}
