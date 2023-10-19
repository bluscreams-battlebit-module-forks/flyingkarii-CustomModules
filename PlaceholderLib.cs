using BBRAPIModules;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BattleBitAPI.Features {
    [Module("A library for placeholders. Supports multiple elements and formats rich text.", "1.2.3")]
    public class PlaceholderLib : BattleBitModule {
        private readonly Regex re = new Regex(@"\{([^\}]+)\}", RegexOptions.Compiled);

        public string Text { get; set; }
        public Dictionary<string, object> Parameters;

        public PlaceholderLib() {
            Text = "";
            Parameters = new();
        }

        public PlaceholderLib(string text) {
            Text = text;
            Parameters = new Dictionary<string, object>();
        }

        public PlaceholderLib(string text, params object[] values) {
            Text = text;
            Parameters = new();

            if (values.Length > 1) {
                for (int i = 0; i < values.Length; i++) {
                    if ((i + 1) % 2 != 0)
                        continue;

                    string key = (string)values[i - 1];
                    object obj = values[i];

                    Parameters.Add(key, obj);
                }
            }
        }

        public PlaceholderLib AddParam(string key, object value) {
            if (key == null || value == null) {
                return this;
            }

            Parameters.Add(key, value);
            return this;
        }

        public string GetSurroundedValue(string str) {
            List<string> split = str.Split(" ").ToList();
            string newString = "";

            bool limited = str.StartsWith("!");

            if (limited) {
                str = str.Substring(1);

                if (Parameters.ContainsKey(str))
                    return Parameters[str].ToString()!;
                else
                    return "{!" + str + "}";
            }

            foreach (string value in split) {
                string newValue = GetValueOf(value);
                newString += newValue;
            }

            return newString;
        }

        public string GetValueOf(string str) {
            string[] equalsSplit = str.Split("=");
            bool limited = str.StartsWith("!");

            if (str.StartsWith("#"))
                return $"<color={str}>";
            else if (str.Equals("/"))
                return "</color>";
            else if (str.StartsWith("/"))
                return "<" + str + ">";
            else if (Parameters.ContainsKey(str))
                return Parameters[str].ToString();
            else if (equalsSplit.Length > 1) {
                return "<" + str + ">";
            }

            switch (str) {
                case "b":
                case "i":
                case "lowercase":
                case "uppercase":
                case "smallcaps":
                case "noparse":
                case "nobr":
                case "sup":
                case "sub":
                    return "<" + str + ">";
                default:
                    return "{" + str + "}";
            }
        }

        public string Run() {
            return re.Replace(Text, delegate (Match match) {
                return GetSurroundedValue(match.Groups[1].Value);
            });
        }
    }
}
