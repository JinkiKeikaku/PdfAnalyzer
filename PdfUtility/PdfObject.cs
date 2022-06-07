using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfUtility
{
    public class PdfObject
    {
        public override string ToString()
        {
            return "PdfObject";
        }
    }

    public class PdfNull : PdfObject
    {
        public PdfNull()
        {
        }
        public override string ToString()
        {
            return "null";
        }
    }

}
