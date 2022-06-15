using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfUtility
{
    public static class UnicodeHelper
    {

        /// <summary>
        /// 漢字コードで康煕部首をCJK統合漢字など通常（？）の漢字に変換する
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string UnifiedKanjiConverter(string text)
        {
            if (mEquivalentUnifiedIdeographMap == null)
            {
                mEquivalentUnifiedIdeographMap = ParseEquivalentUnifiedIdeographFile();
            }
            var sb = new StringBuilder();
            foreach (var c in text)
            {
                sb.Append((Char)mEquivalentUnifiedIdeographMap.GetValueOrDefault(c, c));
            }
            return sb.ToString();
        }
        static Dictionary<int, int>? mEquivalentUnifiedIdeographMap = null;

        static Dictionary<int, int> ParseEquivalentUnifiedIdeographFile()
        {
            Dictionary<int, int> conv = new();
            var tokens = new List<string>();
            var sb = new StringBuilder();
            using var mem = new StringReader(Properties.Resources.EquivalentUnifiedIdeograph);
            while (true)
            {
                var line = mem.ReadLine();
                if (line == null) break;
                tokens.Clear();
                sb.Clear();
                foreach (var c in line)
                {
                    if (c == '#')
                    {
                        AddToken(tokens, sb);
                        break;
                    }
                    if (Char.IsWhiteSpace(c) || c == '.' || c == ';')
                    {
                        AddToken(tokens, sb);
                        continue;
                    }
                    sb.Append(c);
                }
                AddToken(tokens, sb);
                if (tokens.Count == 2)
                {
                    var from = Convert.ToInt32(tokens[0], 16);
                    var to = Convert.ToInt32(tokens[1], 16);
                    conv[from] = to;
                }else if (tokens.Count == 3)
                {
                    var from1 = Convert.ToInt32(tokens[0], 16);
                    var from2 = Convert.ToInt32(tokens[1], 16);
                    var to = Convert.ToInt32(tokens[2], 16);
                    for (var from = from1; from <= from2; from++)
                    {
                        conv[from] = to++;
                    }
                }
            }
            return conv;
        }

        static void AddToken(List<string> tokens, StringBuilder sb)
        {
            if (sb.Length > 0)
            {
                tokens.Add(sb.ToString());
                sb.Clear();
            }
        }

    }
}
