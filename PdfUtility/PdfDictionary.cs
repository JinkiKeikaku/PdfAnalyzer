using PdfUtility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfUtility
{
    /// <summary>
    /// PDFの辞書オブジェクト
    /// </summary>
    public class PdfDictionary : PdfObject, IEnumerable<KeyValuePair<string, PdfObject>>
    {

        public PdfDictionary() { }

        public void Add(string key, PdfObject value)
        {
            mDict[key] = value;
        }

        public void Add(PdfName key, PdfObject value)
        {
            mDict[key.Name]= value;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<<");
            foreach(var d in mDict)
            {
                sb.Append(d.Key);
                sb.Append(" ");
                sb.AppendLine(d.Value.ToString());
            }
            sb.AppendLine(">>");
            return sb.ToString();
        }

        public int Count=>mDict.Count;
        public KeyValuePair<string, PdfObject> GetAt(int i) => mDict.ElementAt(i);

        public bool ContainsKey(PdfName name) => mDict.ContainsKey(name.Name);
        public bool ContainsKey(string key) => mDict.ContainsKey(key);

        public PdfObject? GetValue(PdfName name) => GetValue(name.Name);
        public PdfObject? GetValue(string name)
        {
            if (mDict.TryGetValue(name, out var v)) return v;
            return null;
        }

        public T? GetValue<T>(PdfName name) where T : PdfObject => GetValue<T>(name.Name);
        public T? GetValue<T>(string name) where T : PdfObject => GetValue(name) as T;

        public int GetInt(string name) => (int)GetDouble(name);
        public double GetDouble(PdfName name) => GetDouble(name.Name);
        public double GetDouble(string name)
        {
            var obj = GetValue<PdfNumber>(name);
            if(obj == null) throw new Exception($"GetDouble:{name} is not PdfNumber.");
            return obj.DoubleValue;
        }


        public ByteString GetString(PdfName name) => GetString(name.Name);
        public ByteString GetString(string name)
        {
            var obj = GetValue<PdfString>(name);
            if (obj == null) throw new Exception($"GetString:{name} is not PdfString.");
            return obj.Text;
        }

        public List<PdfObject> GetArray(PdfName name) => GetArray(name.Name);
        public List<PdfObject> GetArray(string name)
        {
            var obj = GetValue<PdfArray>(name);
            if (obj == null) throw new Exception($"GetArray:{name} is not PdfArray.");
            return obj.Elements;
        }

        public PdfReference GetReference(PdfName name) => GetReference(name.Name);
        public PdfReference GetReference(string name)
        {
            var obj = GetValue<PdfReference>(name);
            if (obj == null) throw new Exception($"GetReference:{name} is not PdfReference.");
            return obj;
        }

        public PdfDictionary GetDictionary(PdfName name) => GetDictionary(name.Name);
        public PdfDictionary GetDictionary(string name)
        {
            var obj = GetValue<PdfDictionary>(name);
            if (obj == null) throw new Exception($"GetDictionary:{name} is not PdfDictionary.");
            return obj;
        }
        IEnumerator<KeyValuePair<string, PdfObject>> IEnumerable<KeyValuePair<string, PdfObject>>.GetEnumerator()
        {
            return mDict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return mDict.GetEnumerator();
        }
        private Dictionary<string, PdfObject> mDict = new();

    }
}
