using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Cuphead3PCoop
{
    /// <summary>
    /// Pure C# lightweight JSON serializer and parser.
    /// Completely bypasses Unity's JsonUtility C++ limitation where dynamically loaded types are omitted.
    /// Supports nested classes, lists, dictionaries, strings, primitives, and nulls.
    /// </summary>
    internal static class SimpleJson
    {
        public static string Serialize(object obj)
        {
            var sb = new StringBuilder();
            SerializeValue(obj, sb, 0);
            return sb.ToString();
        }

        private static void Indent(StringBuilder sb, int level)
        {
            sb.Append(new string(' ', level * 2));
        }

        private static void SerializeValue(object val, StringBuilder sb, int level)
        {
            if (val == null)
            {
                sb.Append("null");
            }
            else if (val is string)
            {
                sb.Append("\"").Append(EscapeString((string)val)).Append("\"");
            }
            else if (val is bool)
            {
                sb.Append((bool)val ? "true" : "false");
            }
            else if (val is float)
            {
                sb.Append(((float)val).ToString("0.0", CultureInfo.InvariantCulture));
            }
            else if (val is int || val is long)
            {
                sb.Append(val.ToString());
            }
            else if (val is IDictionary)
            {
                var dict = (IDictionary)val;
                sb.AppendLine("{");
                int count = dict.Count;
                int idx = 0;
                foreach (DictionaryEntry kv in dict)
                {
                    Indent(sb, level + 1);
                    sb.Append("\"").Append(EscapeString(kv.Key.ToString())).Append("\": ");
                    SerializeValue(kv.Value, sb, level + 1);
                    if (++idx < count) sb.Append(",");
                    sb.AppendLine();
                }
                Indent(sb, level);
                sb.Append("}");
            }
            else if (val is IEnumerable)
            {
                var list = (IEnumerable)val;
                sb.AppendLine("[");
                var items = new List<object>();
                foreach (var item in list) items.Add(item);
                for (int i = 0; i < items.Count; i++)
                {
                    Indent(sb, level + 1);
                    SerializeValue(items[i], sb, level + 1);
                    if (i < items.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                Indent(sb, level);
                sb.Append("]");
            }
            else
            {
                // Plain class reflection
                sb.AppendLine("{");
                var fields = val.GetType().GetFields();
                for (int i = 0; i < fields.Length; i++)
                {
                    Indent(sb, level + 1);
                    sb.Append("\"").Append(fields[i].Name).Append("\": ");
                    SerializeValue(fields[i].GetValue(val), sb, level + 1);
                    if (i < fields.Length - 1) sb.Append(",");
                    sb.AppendLine();
                }
                Indent(sb, level);
                sb.Append("}");
            }
        }

        private static string EscapeString(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        public static object Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            int index = 0;
            return ParseValue(json, ref index);
        }

        private static void SkipWhitespace(string json, ref int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index])) index++;
        }

        private static object ParseValue(string json, ref int index)
        {
            SkipWhitespace(json, ref index);
            if (index >= json.Length) return null;

            char c = json[index];
            if (c == '{') return ParseObject(json, ref index);
            if (c == '[') return ParseArray(json, ref index);
            if (c == '"') return ParseString(json, ref index);
            if (c == 't' || c == 'f') return ParseBool(json, ref index);
            if (c == 'n') return ParseNull(json, ref index);
            if (char.IsDigit(c) || c == '-') return ParseNumber(json, ref index);

            index++;
            return null;
        }

        private static Dictionary<string, object> ParseObject(string json, ref int index)
        {
            var dict = new Dictionary<string, object>();
            index++; // skip '{'

            while (index < json.Length)
            {
                SkipWhitespace(json, ref index);
                if (index >= json.Length) break;
                if (json[index] == '}') { index++; break; }

                string key = ParseString(json, ref index);
                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ':') index++; // skip ':'

                object val = ParseValue(json, ref index);
                dict[key] = val;

                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ',') index++;
            }
            return dict;
        }

        private static List<object> ParseArray(string json, ref int index)
        {
            var list = new List<object>();
            index++; // skip '['

            while (index < json.Length)
            {
                SkipWhitespace(json, ref index);
                if (index >= json.Length) break;
                if (json[index] == ']') { index++; break; }

                object val = ParseValue(json, ref index);
                list.Add(val);

                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ',') index++;
            }
            return list;
        }

        private static string ParseString(string json, ref int index)
        {
            index++; // skip initial '"'
            var sb = new StringBuilder();
            while (index < json.Length)
            {
                char c = json[index++];
                if (c == '"') break;
                if (c == '\\' && index < json.Length)
                {
                    char next = json[index++];
                    if (next == 'n') sb.Append('\n');
                    else if (next == 'r') sb.Append('\r');
                    else if (next == 't') sb.Append('\t');
                    else sb.Append(next);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static bool ParseBool(string json, ref int index)
        {
            if (json.Substring(index).StartsWith("true")) { index += 4; return true; }
            if (json.Substring(index).StartsWith("false")) { index += 5; return false; }
            return false;
        }

        private static object ParseNull(string json, ref int index)
        {
            if (json.Substring(index).StartsWith("null")) { index += 4; return null; }
            return null;
        }

        private static object ParseNumber(string json, ref int index)
        {
            int start = index;
            while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == '-' || json[index] == 'e' || json[index] == 'E' || json[index] == '+'))
            {
                index++;
            }
            string s = json.Substring(start, index - start);
            if (s.Contains("."))
            {
                float f;
                if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out f)) return f;
            }
            long l;
            if (long.TryParse(s, out l))
            {
                if (l <= int.MaxValue && l >= int.MinValue) return (int)l;
                return l;
            }
            return 0;
        }
    }
}
