using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;

namespace Services.JsonShaper
{
    public static class JsonShaperService
    {
        private static readonly Regex _substituteSpaces = new Regex(@"[ ]{4}", RegexOptions.Multiline);
        private static readonly char[] _trimmChars = new[] { ' ', '\n', '\t' };
        public static readonly string Root = "@root";

        private static bool HasAccessByIp(this string ip, string mask)
        {
            int[] srcIp;

            try
            {
                srcIp = ip.Split('.').Select(int.Parse).ToArray();
            }
            catch
            {
                srcIp = new int[0];
            }

            var maskIp = mask.Split('.');

            try
            {
                for (var i = 0; i < srcIp.Length; i++)
                {
                    if (maskIp[i] == "*")
                        return true;

                    if (srcIp[i] != int.Parse(maskIp[i]))
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool HasAccessByIp(this string src, params string[] masks)
        {
            if (masks == null)
                return true;

            if (masks.Length == 0)
                return true;

            if (masks.Any(itm => itm == "*"))
                return true;

            return masks.Any(src.HasAccessByIp);
        }

        private static bool HasAccessByMask(this string ip, string mask)
        {
            var srcIp = ip.Split('.');
            var maskIp = mask.Split('.').Select(m => m.Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries)[0]).ToArray();

            try
            {
                for (var i = 0; i < srcIp.Length; i++)
                {
                    if (maskIp[i] == "*")
                        return true;

                    if (srcIp[i] != maskIp[i])
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool HasPathAccessByMask(this string path, params string[] masks)
        {
            if (masks == null)
                return true;

            if (masks.Length == 0)
                return true;

            if (masks.Any(itm => itm == "*"))
                return true;

            if (string.IsNullOrEmpty(path))
                return true;

            return masks.Any(path.HasAccessByMask);
        }

        private static void AddSpaces(this StringBuilder sb, int level)
        {
            sb.Append(new string(' ', level * 2));
        }

        public static string CutJson(this string json, string[] rights, string[] accessIps, string requestIp)
        {
            var sb = new StringBuilder();
            var toBeProcesses = new List<string>(rights);
            bool haveToPutComma = false;
            sb.AppendLine("{");
            do
            {
                var result = json.CutPrivJson(toBeProcesses.ToArray(), accessIps, requestIp);
                if (!haveToPutComma)
                {
                    haveToPutComma = true;
                }
                else if (!string.IsNullOrEmpty(result))
                {
                    sb.AppendLine(",");
                }

                var notProcessed = new List<string>();
                var toCut = (from p in toBeProcesses
                             let token = p.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0]?
                                 .Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries)[0]
                             where !string.IsNullOrEmpty(token)
                             group token by token into g
                             select g.Key).ToList();
                var cutted = new List<string>();
                foreach (var t in toCut)
                {
                    var lst = toBeProcesses
                        .Where(p => t.Equals(p.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0]?
                                .Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries)[0],
                            StringComparison.CurrentCultureIgnoreCase)).ToList();
                    if (cutted.All(c => !c.Equals(t)) && lst.Count > 0)
                    {
                        cutted.Add(t);
                        var tok = lst.First();
                        lst.RemoveAt(0);
                        foreach (var path in tok.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                        {
                            bool mergeCurrentInRoot = false;
                            var aliases = path.Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries);
                            if (aliases.Length == 2 && !string.IsNullOrEmpty(result))
                            {
                                mergeCurrentInRoot = Root == aliases[1];
                                var regex = new Regex($@"(?is)(?:^|,)\s*""({aliases[0]})""");
                                var match = regex.Match(result);
                                if (match.Success)
                                {
                                    if (!mergeCurrentInRoot)
                                    {
                                        var firstPart = result.Substring(0, match.Groups[1].Index);
                                        var secondPart = result.Substring(match.Groups[1].Index + match.Groups[1].Length);
                                        result = firstPart + aliases[1] + secondPart;
                                    }
                                    else
                                    {
                                        #region Processing @root token

                                        var bracketCount = 1;
                                        var firstBracketIndex = result.IndexOf('{', match.Groups[0].Index) + 1;
                                        var rootedJson = result.Skip(firstBracketIndex).TakeWhile(x =>
                                        {
                                            switch (x)
                                            {
                                                case '{':
                                                    bracketCount++;
                                                    break;
                                                case '}':
                                                    bracketCount--;
                                                    break;
                                                default:
                                                    break;
                                            }
                                            return bracketCount != 0;
                                        });

                                        var rooted = new string(rootedJson.SkipLast(1).ToArray());
                                        string rest;
                                        var firstPart = result.Substring(0, match.Groups[0].Index);
                                        var lastOffset = rooted.Length + firstBracketIndex + 2;
                                        if (result.Length > lastOffset)
                                        {
                                            rest = result.Substring(rooted.Length + firstBracketIndex + 2).TrimStart(',');
                                        }
                                        else
                                        {
                                            rest = "";
                                        }

                                        if (string.IsNullOrEmpty(firstPart))
                                        {
                                            rooted = rooted.TrimStart('\n');
                                        }
                                        else
                                        {
                                            firstPart = firstPart.TrimEnd(_trimmChars);
                                            firstPart += ", ";
                                        }

                                        if (!string.IsNullOrEmpty(rest))
                                        {
                                            rooted = rooted.TrimEnd(_trimmChars);
                                            rooted += ", ";
                                            rest = rest.TrimEnd(' ');
                                        }
                                        else
                                        {
                                            rooted = rooted.TrimEnd(' ');
                                        }

                                        rooted = _substituteSpaces.Replace(rooted, m => new string(' ', 2));

                                        result = firstPart + rooted + rest;

                                        #endregion
                                    }
                                }
                            }
                        }
                    }
                    notProcessed.AddRange(lst);
                }
                sb.Append(result);
                toBeProcesses = new List<string>(notProcessed);
            } while (toBeProcesses.Count > 0);
            if (sb[sb.Length - 1] == '}')
                sb.AppendLine();
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string CutPrivJson(this string json, string[] rights, string[] accessIps, string requestIp)
        {
            if (!requestIp.HasAccessByIp(accessIps))
                return null;

            var dict = new Dictionary<string, StringBuilder>();

            var level = 0;
            var haveToPutComma = false;

            using (var reader = new JsonTextReader(new StringReader(json)))
            {
                while (reader.Read())
                {
                    if (!reader.Path.HasPathAccessByMask(rights))
                        continue;

                    StringBuilder sb;
                    var pathParts = reader.Path.Split('.');
                    if (dict.ContainsKey(pathParts[0]))
                    {
                        sb = dict[pathParts[0]];
                    }
                    else
                    {
                        sb = new StringBuilder();
                        dict.Add(pathParts[0], sb);
                    }

                    switch (reader.TokenType)
                    {
                        case JsonToken.StartObject:
                            if (!haveToPutComma)
                                haveToPutComma = true;
                            else
                                sb.Append(',');

                            sb.Append("\n");
                            sb.AddSpaces(level++);
                            sb.Append("{");
                            haveToPutComma = false;
                            break;

                        case JsonToken.EndObject:
                            sb.Append("\n");
                            sb.AddSpaces(--level);
                            sb.Append("}");
                            haveToPutComma = true;
                            break;

                        case JsonToken.StartArray:
                            sb.Append("\n");
                            sb.AddSpaces(level++);
                            sb.Append("[");
                            haveToPutComma = false;
                            break;

                        case JsonToken.EndArray:
                            sb.Append("\n");
                            sb.AddSpaces(--level);
                            sb.Append("]");
                            haveToPutComma = true;
                            break;

                        case JsonToken.PropertyName:
                            if (sb.Length > 0)
                            {
                                if (!haveToPutComma)
                                    haveToPutComma = true;
                                else
                                    sb.Append(',');
                                sb.Append("\n");
                                sb.AddSpaces(level);
                            }
                            sb.Append(JsonConvert.ToString(reader.Value.ToString()) + ": ");
                            haveToPutComma = false;
                            break;

                        case JsonToken.Boolean:
                        case JsonToken.Bytes:
                        case JsonToken.Float:
                        case JsonToken.Integer:
                        case JsonToken.String:
                            if (!haveToPutComma)
                                haveToPutComma = true;
                            else
                                sb.Append(',');
                            sb.RenderValue(reader);
                            break;
                    }
                }
            }

            var resultSb = new StringBuilder();
            var keys = dict.Keys.Where(i => i != string.Empty).OrderBy(i => i).ToList();
            for (int i = 0; i < keys.Count; ++i)
            {
                if (i > 0)
                    resultSb.AppendLine(",");
                resultSb.Append("  ");
                resultSb.Append(dict[keys[i]].ToString());
            }
            return resultSb.ToString();
        }

        private static void RenderValue(this StringBuilder sb, JsonTextReader reader)
        {
            if (reader.TokenType == JsonToken.String)
            {
                var value = JsonConvert.ToString(reader.Value) ?? string.Empty;

                sb.Append(value);
                return;
            }

            if (reader.ValueType == typeof(double))
            {
                sb.Append(((double)reader.Value).ToString("0.##########"));
                return;
            }

            sb.Append(JsonConvert.ToString(reader.Value));
        }
    }
}