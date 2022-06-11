using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfUtility
{
    public class CMap
    {
        private class CodeSpace
        {
            public readonly int SrcCodeLo;
            public readonly int SrcCodeHi;
            public readonly int CodeLength;

            public CodeSpace(int srcCodeLo, int srcCodeHi, int codeLength)
            {
                SrcCodeLo = srcCodeLo;
                SrcCodeHi = srcCodeHi;
                CodeLength = codeLength;
            }
        }

        private class CmapData
        {
            public readonly int SrcCodeLo;
            public readonly int SrcCodeHi;
            public readonly int DstCode;

            public CmapData(int srcCodeLo, int srcCodeHi, int dstCode)
            {
                SrcCodeLo = srcCodeLo;
                SrcCodeHi = srcCodeHi;
                DstCode = dstCode;
            }
        }

        private List<CodeSpace> mCodeSpaceList = new();
        private List<CmapData> mCMapDataList = new();

        private enum CidState
        {
            None,
            BfChar,
            BfRange,
            CidChar,
            CidRange,
            CodeSpaceRange,
        };


       
        public string Convert(byte[] bytes)
        {
            var pos = 0;
            var sb = new StringBuilder();
            while (pos < bytes.Length)
            {
                var f = false;
                var codeLength = 1;
                for (var i = 1; i < 4; i++)
                {
                    if (i + pos > bytes.Length) break;
                    var c = ConvertInt(bytes, pos, i);
                    for (var j = 0; j < mCodeSpaceList.Count; j++)
                    {
                        var codeSpace = mCodeSpaceList[j];
                        if (codeSpace.CodeLength == i && c >= codeSpace.SrcCodeLo && c <= codeSpace.SrcCodeHi)
                        {
                            foreach(var d in mCMapDataList)
                            {
                                if(c >= d.SrcCodeLo && c <= d.SrcCodeHi)
                                {
                                    var code = c - d.SrcCodeLo + d.DstCode;
                                    sb.Append((Char)code);
                                    f = true;
                                    codeLength = i;
                                    break;
                                }
                            }
                        }
                        if(f) break;    
                    }
                    if (f) break;
                }
                if (!f) sb.Append('?');
                pos += codeLength;
            }
            return sb.ToString();
        }



        /// <summary>
        /// cidをunicodeに変換するマップの初期化
        /// ただし、サロゲートペアは対応しない
        /// </summary>
        /// <param name="parser"></param>
        internal void ParserCid(PdfParser parser)
        {
            CidState state;

            var stack = new List<PdfObject>();
            while (true)
            {
                var obj = parser.ParseObject();
                if (obj == null) break;
                stack.Add(obj);
                //                Debug.WriteLine(obj);
                if (obj is PdfIdentifier id)
                {
                    switch (id.Identifier)
                    {
                        case "begincodespacerange":
                            state = CidState.CodeSpaceRange;
                            stack.Clear();
                            break;
                        case "endcodespacerange":
                            {
                                for (var i = 0; i < stack.Count-1; i += 2)
                                {
                                    var srcCodeLo = GetIntFromCode(stack[i]);
                                    var srcCodeHi = GetIntFromCode(stack[i + 1]);
                                    var scl = stack[i] as PdfHexString;
                                    var n = scl?.Bytes.Length ?? 0;
                                    if (n > 4 || n == 0) break;
                                    var cs = new CodeSpace(srcCodeLo, srcCodeHi, n);
                                    mCodeSpaceList.Add(cs);
                                }
                            }
                            state = CidState.None;
                            stack.Clear();
                            break;
                        case "beginbfrange":
                            state = CidState.BfRange;
                            stack.Clear();
                            break;
                        case "endbfrange":
                            for (var i = 0; i < stack.Count - 1; i += 3)
                            {
                                var srcCode0 = GetIntFromCode(stack[i]);// as PdfHexString)!.ConvertToInt();
                                var srcCode1 = GetIntFromCode(stack[i + 1]);// as PdfHexString)!.ConvertToInt();
                                var dstCode = GetIntFromCode(stack[i + 2]);//.ConvertToInt();

                                if (dstCode >= 0 && dstCode < 65536)
                                {
                                    mCMapDataList.Add(new CmapData(srcCode0, srcCode1, dstCode));
                                }
                                //for (var sc = srcCode0; sc <= srcCode1; sc++)
                                //{
                                //    if (dstCode >= 0 && dstCode < 65536)
                                //    {
                                //        mCMap.Add(sc, (Char)dstCode);
                                //    }
                                //    dstCode++;
                                //}
                            }
                            state = CidState.None;
                            stack.Clear();
                            break;


                        case "beginbfchar":
                            state = CidState.BfChar;
                            stack.Clear();
                            break;
                        case "endbfchar":
                            for (var i = 0; i < stack.Count - 1; i += 2)
                            {
                                var srcCode = GetIntFromCode(stack[i]);// as PdfHexString)!.ConvertToInt();
                                var dstCode = GetIntFromCode(stack[i + 1]);// dh.ConvertToInt();
                                if (dstCode >= 0 && dstCode < 65536)
                                {
                                    mCMapDataList.Add(new CmapData(srcCode, srcCode, dstCode));
                                }
                            }
                            state = CidState.None;
                            stack.Clear();
                            break;
                        case "begincidchar":
                            state = CidState.CidChar;
                            stack.Clear();
                            break;
                        case "endcidchar":
                            for (var i = 0; i < stack.Count - 1; i += 2)
                            {
                                var dstCode = GetIntFromCode(stack[i]);// as PdfHexString)!.ConvertToInt();
                                var srcCode = GetIntFromCode(stack[i + 1]);// dh.ConvertToInt();
                                if (dstCode >= 0 && dstCode < 65536)
                                {
                                    mCMapDataList.Add(new CmapData(srcCode, srcCode, dstCode));
                                }
                            }
                            state = CidState.None;
                            stack.Clear();
                            break;
                        case "begincidrange":
                            state = CidState.CidRange;
                            stack.Clear();
                            break;
                        case "endcidrange":
                            for (var i = 0; i < stack.Count - 1; i += 3)
                            {
                                var dstCode0 = GetIntFromCode(stack[i]);// as PdfHexString)!.ConvertToInt();
                                var dstCode1 = GetIntFromCode(stack[i + 1]);// as PdfHexString)!.ConvertToInt();
                                var srcCode0 = GetIntFromCode(stack[i + 2]);// dh.ConvertToInt();

                                var srcCode1 = srcCode0 + dstCode1 - dstCode0;
                                if (dstCode0 >= 0 && dstCode0 < 65536)
                                {
                                    mCMapDataList.Add(new CmapData(srcCode0, srcCode1, dstCode0));
                                }

                                //for (var dst = dstCode0; dst <= dstCode1; dst++)
                                //{
                                //    if (dst >= 0 && dst < 65536)
                                //    {
                                //        mCMap[srcCode] = (Char)dst;
                                //    }
                                //    srcCode++;
                                //}
                            }
                            state = CidState.None;
                            stack.Clear();
                            break;

                    }
                }
            }
        }

        private int ConvertInt(byte[] buf, int pos, int size)
        {
            var x = 0;
            for (var j = 0; j < size; j++)
            {
                x = x * 256 + buf[pos + j];
            }
            return x;
        }

        private int GetIntFromCode(PdfObject obj)
        {
            if (obj is PdfHexString dh) return dh.ConvertToInt();
            if (obj is PdfNumber num) return num.IntValue;
            throw new Exception("GetIntFromCode() cannot parse to int.");
        }

    }
}
