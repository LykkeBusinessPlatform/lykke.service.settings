using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Core.Token;
using Newtonsoft.Json.Linq;

namespace Services.Extensions
{
    public static class ObjectExstention
    {
        private static readonly Regex _regEx = new Regex("(?i)[$]{(.+?)}", RegexOptions.Compiled);

        public static Dictionary<string, List<string>> GetKeysDict(this object obj)
        {
            var result = new Dictionary<string, List<string>>();

            AddKeys(obj as JToken, null, result);

            return result;
        }

        public static Dictionary<string, List<string>> GetAccessDict(this Dictionary<string, List<string>> keyDics, List<IToken> tokens)
        {
            var result = new Dictionary<string, List<string>>();
            var tokenDic = new Dictionary<string, List<string>>();

            foreach (var token in tokens)
            {
                foreach (var access in token.AccessList.Split(";", StringSplitOptions.RemoveEmptyEntries))
                {

                    var forCheck = access.Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries)[0].Trim().TrimEnd('*');
                    if (!tokenDic.ContainsKey(forCheck))
                    {
                        tokenDic.Add(forCheck, new List<string>());
                    }
                    tokenDic[forCheck].Add(token.RowKey);
                }
            }

            foreach (var key in keyDics.Keys)
            {
                result.Add(key, GetTokens(keyDics[key], tokenDic));
            }

            return result;
        }

        private static List<string> GetTokens(List<string> keyDic, Dictionary<string, List<string>> tokens)
        {
            var result = new List<string>();

            foreach (var tKey in tokens.Keys)
            {
                if (keyDic.Any(kd => kd.StartsWith(tKey, StringComparison.InvariantCultureIgnoreCase)))
                {
                    result.AddRange(tokens[tKey]);
                }
            }

            return result.Distinct().OrderBy(r => r).ToList();
        }

        private static void AddKeys(JToken obj, string path, Dictionary<string, List<string>> keysDict)
        {
            if (obj == null)
                return;

            if (obj.Type == JTokenType.Array)
            {
                foreach (var prop in (JArray)obj)
                {
                    AddKeys(prop, path, keysDict);
                }
            }
            else if (obj.Type == JTokenType.Object)
            {
                foreach (var prop in ((JObject)obj).Properties())
                {
                    string currentPath = path == null ? prop.Name : path + "." + prop.Name;
                    if (prop.Value.Type == JTokenType.String)
                    {
                        AddMatches(keysDict, prop.Value.ToString(), currentPath);
                    }
                    else
                    {
                        AddKeys(prop.Value, currentPath, keysDict);
                    }
                }
            }
            else if (obj.Type == JTokenType.String)
            {
                AddMatches(keysDict, ((JValue)obj).ToString(), path);
            }
        }

        private static void AddMatches(Dictionary<string, List<string>> keysDict, string val, string currentPath)
        {
            var matches = _regEx.Matches(val);
            foreach (Match match in matches)
            {
                if (match.Success)
                    AddKey(match.Groups[1].Value, currentPath, keysDict);
            }
        }

        private static void AddKey(string key, string path, Dictionary<string, List<string>> keysDict)
        {
            if (keysDict.ContainsKey(key))
                keysDict[key].Add(path);
            else
                keysDict.Add(key, new List<string> { path });
        }
    }
}
