using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfUtility
{
    class PdfIndirectObject : PdfObject
    {
        public int ObjectNumber;
        public int GenerationNumber;
        public object IndirectObject;

        public PdfIndirectObject(object obj, int objectNumber, int generationNumber)
        {
            IndirectObject = obj;
            ObjectNumber = objectNumber;
            GenerationNumber = generationNumber;
        }
    }
}
