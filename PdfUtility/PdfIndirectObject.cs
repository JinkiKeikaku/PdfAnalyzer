using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfUtility
{
    public class PdfIndirectObject : PdfObject
    {
        public int ObjectNumber;
        public int GenerationNumber;
        public PdfObject IndirectObject;

        public PdfIndirectObject(PdfObject obj, int objectNumber, int generationNumber)
        {
            IndirectObject = obj;
            ObjectNumber = objectNumber;
            GenerationNumber = generationNumber;
        }

        public override string ToString()
        {
            return $"({ObjectNumber} {GenerationNumber} obj)";
        }

    }
}
