using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Core.Entities;
using Core.KeyValue;
using Core.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Services
{
    public static class SrvPlaceholdersHandler
    {
        private const string StartKey = "${";
        private const string EndKey = "}";
        private const string TypeEndKey = "]";
        private const string CommentStartKey = "#";
        private const string CommentEndKey = "\n";
        private const char _backSlash = '\\';
        private const char _slash = '/';
        private const char _dblQuote = '"';

        private const string YamlPlaceholder = "settings-key";
        private const string YamlTypes = "types";
        private const string YamlDefaultValue = "default-value";

        private static readonly string[] _yamlKeys = { YamlPlaceholder, YamlTypes, YamlDefaultValue };
        private static readonly List<KeyValue> KeyValues = new List<KeyValue>();
        private static readonly string[] _noQuotesTypes = { KeyValueTypes.Json, KeyValueTypes.JsonArray };

        private static bool _removeLastBacket = false;
        private static KeyValue TempKeyValue = new KeyValue();
        private static StringBuilder JsonData = new StringBuilder();

        public static string SubstituteServiceTokens(this string json, IEnumerable<IServiceTokenEntity> serviceTokens)
        {
            var regEx = new Regex("(?i)['\"]?[$]{{(.+?)}}['\"]?", RegexOptions.Compiled);
            return regEx.Replace(json, delegate (Match m)
            {
                var keyNum = m.Groups[1].Value?.Split(":".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                IServiceTokenEntity tok;
                switch (keyNum?.Length ?? 0)
                {
                    case 1:
                        tok =
                            serviceTokens.FirstOrDefault(
                                st => st.Token.Equals(keyNum[0], StringComparison.CurrentCultureIgnoreCase));
                        if (tok == null)
                        {
                            return m.Value;
                        }
                        return $@"[""{tok.SecurityKeyOne}"", ""{tok.SecurityKeyTwo}""]";
                    case 2:
                        tok =
                           serviceTokens.FirstOrDefault(
                               st => st.Token.Equals(keyNum[0], StringComparison.CurrentCultureIgnoreCase));
                        if (tok == null)
                            return m.Value;
                        return $@"""{(int.Parse(keyNum[1]) == 1 ? tok.SecurityKeyOne : tok.SecurityKeyTwo)}""";
                    default:
                        return m.Value;
                }
            });
        }

        public static string Substitute(
            this string json,
            Dictionary<string, IKeyValueEntity> keyValues,
            string networkId = null,
            string repositoryVersion = "")
        {
            var result = new StringBuilder();
            var hasError = false;

            var index = 0;
            var indexLast = 0;
            index = json.IndexOf(StartKey, index, StringComparison.Ordinal);

            // if there are no StartKey return json back
            if (index < 0)
                result.Append(json);

            var propertyRemoved = false;

            while (index > 0)
            {
                bool needToRemoveQuotes = false;
                var part = json.Substring(indexLast, index - indexLast);

                if (propertyRemoved)
                {
                    var bracketIndex = part.LastIndexOf('}');
                    var commaIndex = part.LastIndexOf(',');
                    if (bracketIndex > 0 && (commaIndex < 0 || commaIndex > bracketIndex))
                    {
                        result.Remove(result.Length - 1, 1);
                        part = part.Remove(0, 1);
                    }
                    else
                    {
                        part = part.Remove(0, 2);
                    }
                }

                result.Append(part);

                var endIndex = json.IndexOf(EndKey, index, StringComparison.Ordinal);
                if (json[endIndex + 1] == ':' && json[endIndex + 2] == '[')
                {
                    var typeEndIndex = json.IndexOf(TypeEndKey, endIndex + 2, StringComparison.Ordinal);
                    json = json.Remove(endIndex + 1, typeEndIndex - endIndex);
                }

                if (endIndex < 0)
                {
                    result.Append(json.Substring(indexLast));
                    break;
                }

                var key = repositoryVersion + (json.Substring(index + StartKey.Length, endIndex - index - StartKey.Length));

                if (!keyValues.ContainsKey(key))
                    key = key.Substring(repositoryVersion.Length);

                if (keyValues.ContainsKey(key))
                {
                    var overridedValue = keyValues[key]?.Override?
                        .FirstOrDefault(item => item.NetworkId == networkId)
                        ?.Value;

                    var valueEncoded = string.IsNullOrEmpty(overridedValue)
                        ? keyValues[key].Value?.EscapeJson()
                        : overridedValue.EscapeJson();

                    if (bool.TryParse(valueEncoded, out var boolVal))
                        valueEncoded = boolVal.ToString().ToLower();
                    var types = keyValues[key].Types;
                    if (string.IsNullOrEmpty(valueEncoded) && types != null && types.Contains(KeyValueTypes.Optional))
                    {
                        var emptyValueType = keyValues[key].EmptyValueType;

                        if (emptyValueType == "empty")
                        {
                            propertyRemoved = false;
                            result.Append(valueEncoded);
                        }
                        else if (emptyValueType == "null" || emptyValueType == null)
                        {
                            var currentResult = result.ToString();
                            var bracketIndex = currentResult.LastIndexOf('{');
                            var commaIndex = currentResult.LastIndexOf(',');

                            var indexToRemove = bracketIndex > commaIndex ? bracketIndex : commaIndex;

                            result.Remove(indexToRemove + 1, result.Length - indexToRemove - 1);
                            propertyRemoved = true;
                        }
                    }
                    else if (string.IsNullOrEmpty(valueEncoded) && (types == null || (types != null && !types.Contains(KeyValueTypes.Optional))))
                    {
                        hasError = true;
                        propertyRemoved = false;
                        var value = string.Format($"Value for Key \"{key}\" is missing");
                        result.Append(value);
                    }
                    else
                    {
                        propertyRemoved = false;
                        bool isForcedQuotesType = types != null && types.Contains(KeyValueTypes.String);
                        bool isJsonType = types != null && types.Any(t => _noQuotesTypes.Contains(t));
                        bool noQuutesValue = bool.TryParse(valueEncoded, out bool _)
                            || int.TryParse(valueEncoded, out int _)
                            || double.TryParse(valueEncoded, NumberStyles.Any, CultureInfo.InvariantCulture, out double _)
                            || isJsonType
                            || valueEncoded.StartsWith("{") && valueEncoded.EndsWith("}")
                            || valueEncoded.StartsWith("[") && valueEncoded.EndsWith("]");

                        var isQuotesAround = part.EndsWith("\"") && json.Substring(endIndex + EndKey.Length, 1) == "\"";

                        needToRemoveQuotes = isQuotesAround && !isForcedQuotesType && noQuutesValue;

                        if (needToRemoveQuotes)
                            result.Remove(result.Length - 1, 1);

                        result.Append(isJsonType ? valueEncoded?.Replace("\\", "") : valueEncoded);
                    }
                }
                else
                {
                    result.Append($"{StartKey}{key}{EndKey}");
                }

                indexLast = endIndex + EndKey.Length + (needToRemoveQuotes ? 1 : 0);
                index = json.IndexOf(StartKey, indexLast, StringComparison.Ordinal);

                if (index < 0)
                {
                    var lastPart = json.Substring(indexLast);
                    if (propertyRemoved && lastPart.Contains('"'))
                        lastPart = lastPart.Remove(lastPart.IndexOf('"'), 1);

                    result.Append(lastPart);
                    break;
                }
            }

            if (hasError)
                result.Insert(0, "Error(s) occured: \n\r");
            return result.ToString();
        }

        public static string ReleaseFromComments(this string json)
        {
            var result = new StringBuilder();

            var index = 0;
            var indexLast = 0;
            index = json.IndexOf(CommentStartKey, index, StringComparison.Ordinal);

            //check if there are no '#' index will be '-1' return the same json
            if (index < 0)
            {
                return json;
            }
            while (index > 0)
            {
                var part = json.Substring(indexLast, index - indexLast);

                result.Append(part);

                var endIndex = json.IndexOf(CommentEndKey, index, StringComparison.Ordinal);

                indexLast = endIndex + CommentEndKey.Length;
                //indexLast = endIndex;
                index = json.IndexOf(CommentStartKey, indexLast, StringComparison.Ordinal);

                if (index < 0)
                {
                    result.Append(json.Substring(indexLast));
                    break;
                }
            }

            var finalResult = result.ToString();

            return finalResult;
        }

        public static string SubstituteNew(this string json, Dictionary<string, IKeyValueEntity> keyValues, string networkId = null)
        {
            var result = new StringBuilder(json, json.Length * 2);
            var tokensRegExp = new Regex("[\'|\"]\\${\\w+}[\'|\"]", RegexOptions.Multiline | RegexOptions.Compiled);
            var tokenNameRegExp = new Regex("[\'|\"]\\${(\\w+)}[\'|\"]", RegexOptions.Compiled);
            var quotesRegExp = new Regex("[\'|\"]", RegexOptions.Compiled | RegexOptions.Singleline);

            var tokens = tokensRegExp.Matches(json);

            foreach (Match token in tokens)
            {
                var key = tokenNameRegExp.Replace(token.Value, "$1");

                if (keyValues.ContainsKey(key))
                {
                    var overridedValue = keyValues[key].Override?
                        .FirstOrDefault(item => item.NetworkId == networkId)
                        ?.Value;

                    var valueEncoded = string.IsNullOrEmpty(overridedValue)
                        ? keyValues[key].Value?.EscapeJson()
                        : overridedValue.EscapeJson();

                    bool removeQuotes = bool.TryParse(valueEncoded, out bool _) ||
                                        int.TryParse(valueEncoded, out int _) ||
                                        double.TryParse(valueEncoded, out double _);

                    valueEncoded = valueEncoded?.ToLower();

                    var tokenToReplace = removeQuotes
                        ? token.Value
                        : quotesRegExp.Replace(token.Value, string.Empty);

                    if (!string.IsNullOrEmpty(valueEncoded))
                    {
                        result.Replace(tokenToReplace, valueEncoded);
                    }
                }
            }

            return result.ToString();
        }

        public static List<KeyValue> PlaceholderList(this string json)
        {
            var placeholders = new List<KeyValue>();
            var regExForKeyTypes = new Regex("(?i)[$]{(.+?)]", RegexOptions.Compiled);
            var regExForKeys = new Regex("(?i)[$]{(.+?)}", RegexOptions.Compiled);

            var keyTypes = (from m in regExForKeyTypes.Matches(json).Cast<Match>()
                            let val = m.Groups[1].Value
                            where val.IndexOf('{') < 0
                            select val).Distinct().ToList();

            var keys = (from m in regExForKeys.Matches(json).Cast<Match>()
                        let val = m.Groups[1].Value
                        where val.IndexOf('{') < 0
                        select val).Distinct().ToList();

            if (keyTypes.Count > 0)
            {
                keyTypes.ForEach(item =>
                {
                    if (item.IndexOf(':') > 0)
                    {
                        var items = item.Split(':');
                        if (items.Length == 2 && !string.IsNullOrWhiteSpace(items[0]) && !string.IsNullOrWhiteSpace(items[1]))
                        {
                            var rowKey = items[0].Replace("}", string.Empty).Trim();
                            var typesString = items[1].Replace("[", string.Empty).Trim();
                            var types = typesString.Split(',');
                            var keyItem = placeholders.FirstOrDefault(x => x.RowKey == rowKey);
                            if (keyItem == null)
                            {
                                var keyValue = new KeyValue
                                {
                                    RowKey = rowKey,
                                    Types = types
                                };
                                placeholders.Add(keyValue);
                            }
                        }
                    }
                });
            }

            if (keys.Count > 0)
            {
                keys.ForEach(item =>
                {
                    var keyItem = placeholders.FirstOrDefault(x => x.RowKey == item);
                    if (keyItem == null)
                    {
                        placeholders.Add(new KeyValue { RowKey = item });
                    }
                });
            }
            return placeholders;
        }

        public static ServiceResult GetSettingsDataFromYaml(this string yaml)
        {
            try
            {
                var inputYaml = new StringReader(yaml);

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(new CamelCaseNamingConvention())
                    .Build();

                var outputYaml = deserializer.Deserialize<Dictionary<object, object>>(inputYaml);

                KeyValues.Clear();
                TempKeyValue = new KeyValue();
                JsonData = new StringBuilder();

                _removeLastBacket = false;

                GetData(outputYaml);

                return new ServiceResult
                {
                    Success = true,
                    Data = new DataFromYaml
                    {
                        Placeholders = KeyValues,
                        Json = JsonHelper.FormatJson(JsonData.ToString())
                    }
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        private static void GetData(Dictionary<object, object> dict)
        {
            JsonData.Append("{");

            foreach (var item in dict)
            {
                if (item.Value is Dictionary<object, object> innerDict)
                {
                    JsonData.Append("\"" + item.Key + "\":    ");
                    GetData(innerDict);
                    if (dict.Keys.Count > 1 && item.Key != dict.Keys.Last())
                        JsonData.Append(",");
                }
                else
                {
                    if (item.Key.ToString() == YamlPlaceholder)
                    {
                        JsonData.Length -= 3;
                        _removeLastBacket = true;
                        JsonData.Append("\"${" + item.Value + "}\"");

                        if (item.Value == null || string.IsNullOrEmpty(item.Value.ToString()))
                            TempKeyValue = new KeyValue();
                        else
                            TempKeyValue.RowKey = item.Value.ToString();
                    }
                    else if (item.Key.ToString() == YamlTypes)
                    {
                        List<object> innerList = (List<object>)item.Value;
                        if (innerList != null && innerList.Count > 0)
                            TempKeyValue.Types = innerList.Select(x => x.ToString()).ToArray();
                    }
                    else if (item.Key.ToString() == YamlDefaultValue)
                    {
                        if (item.Value != null && !string.IsNullOrEmpty(item.Value.ToString()))
                            TempKeyValue.Value = item.Value.ToString();
                    }
                    else
                    {
                        JsonData.Append("\"" + item.Key + "\":    ");
                        if (item.Value != null && Boolean.TryParse(item.Value.ToString(), out var boolValue))
                            JsonData.Append(boolValue.ToString().ToLower());
                        else if (item.Value != null && Int32.TryParse(item.Value.ToString(), out var intValue))
                            JsonData.Append(intValue);
                        else if (item.Value != null && Double.TryParse(item.Value.ToString(), out var doubleValue))
                            JsonData.Append(doubleValue);
                        else
                            JsonData.Append($"\"{item.Value}\"");

                        _removeLastBacket = true;

                        JsonData.Append(dict.Keys.Count > 1 && item.Key != dict.Keys.Last() ? "," : "}");
                    }
                }
            }

            if (_yamlKeys.Contains(dict.Last().Key.ToString()))
            {
                KeyValues.Add(TempKeyValue);
                TempKeyValue = new KeyValue();
            }

            if (_removeLastBacket)
                _removeLastBacket = false;
            else
                JsonData.Append("}");
        }

        private static string EscapeJson(this string value)
        {
            var output = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                switch (c)
                {
                    case _slash:
                        output.AppendFormat("{0}{1}", _backSlash, _slash);
                        break;

                    case _backSlash:
                        output.AppendFormat("{0}{0}", _backSlash);
                        break;

                    case _dblQuote:
                        output.AppendFormat("{0}{1}", _backSlash, _dblQuote);
                        break;

                    default:
                        output.Append(c);
                        break;
                }
            }

            return output.ToString();
        }
    }
}
