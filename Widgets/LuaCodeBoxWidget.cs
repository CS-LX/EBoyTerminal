using System.Text.RegularExpressions;
using Engine;

namespace EBoyTerminal {
    /// <summary>
    /// 预配置 Lua 语法高亮的代码编辑框。
    /// </summary>
    public class LuaCodeBoxWidget : CodeBoxWidget {
        public LuaCodeBoxWidget() {
            SyntaxHighlighter = new LuaSyntaxHighlighter();
        }

        sealed class LuaSyntaxHighlighter : ICodeSyntaxHighlighter {
            static readonly Regex LuaKeywordRegex = new(@"\b(and|break|do|else|elseif|end|false|for|function|if|in|local|nil|not|or|repeat|return|then|true|until|while)\b", RegexOptions.Compiled);
            static readonly Regex LuaNumberRegex = new(@"(?<![\w.])(?:0x[0-9A-Fa-f]+|\d+(?:\.\d+)?)(?![\w.])", RegexOptions.Compiled);
            static readonly Regex LuaFunctionRegex = new(@"\b[A-Za-z_][A-Za-z0-9_]*(?=\s*\()", RegexOptions.Compiled);

            public CodeStyle[] HighlightLine(string line) {
                CodeStyle[] styles = new CodeStyle[line.Length];
                MarkLuaStrings(line, styles);
                MarkLuaLineComment(line, styles);
                ApplyLuaRegex(line, styles, LuaNumberRegex, CodeStyle.Number);
                ApplyLuaRegex(line, styles, LuaKeywordRegex, CodeStyle.Keyword);
                ApplyLuaRegex(line, styles, LuaFunctionRegex, CodeStyle.Function);
                return styles;
            }

            public Color GetColor(CodeStyle style, Color defaultColor) {
                return defaultColor;
            }

            static void MarkLuaStrings(string line, CodeStyle[] styles) {
                for (int i = 0; i < line.Length; i++) {
                    if (i + 1 < line.Length && line[i] == '[' && line[i + 1] == '[') {
                        int end = line.IndexOf("]]", i + 2, StringComparison.Ordinal);
                        int finish = end >= 0 ? end + 2 : line.Length;
                        MarkRange(styles, i, finish, CodeStyle.String, overwrite: false);
                        i = finish - 1;
                    }
                    else if (line[i] is '\'' or '"') {
                        char quote = line[i];
                        int j = i + 1;
                        bool escaped = false;
                        for (; j < line.Length; j++) {
                            char c = line[j];
                            if (escaped) {
                                escaped = false;
                                continue;
                            }
                            if (c == '\\') {
                                escaped = true;
                                continue;
                            }
                            if (c == quote) {
                                j++;
                                break;
                            }
                        }
                        MarkRange(styles, i, Math.Min(j, line.Length), CodeStyle.String, overwrite: false);
                        i = Math.Min(j, line.Length) - 1;
                    }
                }
            }

            static void MarkLuaLineComment(string line, CodeStyle[] styles) {
                int commentIndex = -1;
                for (int i = 0; i + 1 < line.Length; i++) {
                    if (line[i] == '-' && line[i + 1] == '-' && styles[i] != CodeStyle.String) {
                        commentIndex = i;
                        break;
                    }
                }
                if (commentIndex >= 0) {
                    MarkRange(styles, commentIndex, line.Length, CodeStyle.Comment, overwrite: true);
                }
            }

            static void ApplyLuaRegex(string line, CodeStyle[] styles, Regex regex, CodeStyle style) {
                foreach (Match match in regex.Matches(line)) {
                    int end = match.Index + match.Length;
                    for (int i = match.Index; i < end && i < styles.Length; i++) {
                        if (styles[i] == CodeStyle.Normal) {
                            styles[i] = style;
                        }
                    }
                }
            }

            static void MarkRange(CodeStyle[] styles, int start, int end, CodeStyle style, bool overwrite) {
                for (int i = Math.Max(0, start); i < end && i < styles.Length; i++) {
                    if (overwrite || styles[i] == CodeStyle.Normal) {
                        styles[i] = style;
                    }
                }
            }
        }
    }
}
