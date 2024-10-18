using System.Text;
using System.Text.RegularExpressions;

namespace GenAI.Server.Jinja
{
    public static class TemplateProcessor
    {
        public static string Preprocess(string template, PreprocessOptions options)
        {
            if (template.EndsWith("\n"))
            {
                template = template[..^1];
            }

            template = Regex.Replace(template, "{#.*?#}", "{##}", RegexOptions.Singleline | RegexOptions.Compiled);

            if (options.LstripBlocks)
            {
                template = Regex.Replace(template, @"^[ \t]*({[#%])", "$1", RegexOptions.Multiline | RegexOptions.Compiled);
            }

            if (options.TrimBlocks)
            {
                template = Regex.Replace(template, @"([#%]})\n", "$1", RegexOptions.Compiled);
            }

            return template.Replace("{##}", "")
                           .Replace(@"-%}\s*", "%}")
                           .Replace(@"\s*{%-", "{%")
                           .Replace(@"-}}", "}}")
                           .Replace(@"-%}", "%}")
                           .Replace(@"\s*{{-", "{{");
        }

        public static List<Token> Tokenize(string source, PreprocessOptions? options = null)
        {
            List<Token> tokens = new();
            string src = Preprocess(source, options ?? new PreprocessOptions());
            int cursorPosition = 0;
            int currentLine = 1;
            int currentColumn = 1;

            string ConsumeWhile(Func<char, bool> predicate)
            {
                StringBuilder str = new();
                while (cursorPosition < src.Length && predicate(src[cursorPosition]))
                {
                    if (src[cursorPosition] == '\\')
                    {
                        cursorPosition++;
                        if (cursorPosition >= src.Length)
                        {
                            throw new SyntaxError("Unexpected end of input");
                        }

                        char escaped = src[cursorPosition];
                        cursorPosition++;
                        currentColumn += 2;

                        if (!EscapeCharacters.Mappings.TryGetValue(escaped.ToString(), out string? unescaped))
                        {
                            throw new SyntaxError($"Unexpected escaped character: {escaped}");
                        }

                        _ = str.Append(unescaped);
                        continue;
                    }

                    if (src[cursorPosition] == '\n')
                    {
                        currentLine++;
                        currentColumn = 1; // Reset column for a new line
                    }
                    else
                    {
                        currentColumn++;
                    }

                    _ = str.Append(src[cursorPosition]);
                    cursorPosition++;
                }
                return str.ToString();
            }

        main:
            while (cursorPosition < src.Length)
            {
                string? lastTokenType = tokens.Count > 0 ? tokens[^1].Type : null;

                if (lastTokenType is null or TokenTypes.CloseStatement or TokenTypes.CloseExpression)
                {
                    StringBuilder text = new();
                    while (cursorPosition < src.Length && !(src[cursorPosition] == '{' && (src[cursorPosition + 1] == '%' || src[cursorPosition + 1] == '{')))
                    {
                        if (src[cursorPosition] == '\n')
                        {
                            currentLine++;
                            currentColumn = 1;
                        }
                        else
                        {
                            currentColumn++;
                        }

                        _ = text.Append(src[cursorPosition]);
                        cursorPosition++;
                    }

                    if (text.Length > 0)
                    {
                        tokens.Add(new Token(text.ToString(), TokenTypes.Text, currentLine, currentColumn));
                        continue;
                    }
                }

                while (cursorPosition < src.Length && char.IsWhiteSpace(src[cursorPosition]))
                {
                    if (src[cursorPosition] == '\n')
                    {
                        currentLine++;
                        currentColumn = 1;
                    }
                    else
                    {
                        currentColumn++;
                    }

                    cursorPosition++;
                }

                char currentChar = src[cursorPosition];
                int tokenStartLine = currentLine;
                int tokenStartColumn = currentColumn;

                if (currentChar is '-' or '+')
                {
                    string? lastTokenType2 = tokens.Count > 0 ? tokens[^1].Type : null;
                    if (lastTokenType2 is TokenTypes.Text or null)
                    {
                        throw new SyntaxError($"Unexpected character: {currentChar}");
                    }

                    switch (lastTokenType2)
                    {
                        case TokenTypes.Identifier:
                        case TokenTypes.NumericLiteral:
                        case TokenTypes.BooleanLiteral:
                        case TokenTypes.NullLiteral:
                        case TokenTypes.StringLiteral:
                        case TokenTypes.CloseParen:
                        case TokenTypes.CloseSquareBracket:
                            break;

                        default:
                            cursorPosition++;
                            currentColumn++;
                            string num = ConsumeWhile(char.IsDigit);
                            tokens.Add(new Token($"{currentChar}{num}", num.Length > 0 ? TokenTypes.NumericLiteral : TokenTypes.UnaryOperator, tokenStartLine, tokenStartColumn));
                            continue;
                    }
                }

                foreach (Tuple<string, string> mapping in OrderedMappingTable.Mappings)
                {
                    string slice = src.Substring(cursorPosition, Math.Min(mapping.Item1.Length, src.Length - cursorPosition));
                    if (slice == mapping.Item1)
                    {
                        tokens.Add(new Token(mapping.Item1, mapping.Item2, tokenStartLine, tokenStartColumn));
                        cursorPosition += mapping.Item1.Length;
                        currentColumn += mapping.Item1.Length;
                        goto main;
                    }
                }

                if (currentChar is '\'' or '"')
                {
                    cursorPosition++;
                    currentColumn++;
                    string str = ConsumeWhile(c => c != currentChar);
                    tokens.Add(new Token(str, TokenTypes.StringLiteral, tokenStartLine, tokenStartColumn));
                    cursorPosition++;
                    currentColumn++;
                    continue;
                }

                if (char.IsDigit(currentChar))
                {
                    string num = ConsumeWhile(char.IsDigit);
                    tokens.Add(new Token(num, TokenTypes.NumericLiteral, tokenStartLine, tokenStartColumn));
                    continue;
                }

                if (Utils.IsWord(currentChar))
                {
                    string word = ConsumeWhile(Utils.IsWord);
                    if (Keywords.Mapping.TryGetValue(word, out string? keyword))
                    {
                        tokens.Add(new Token(word, keyword, tokenStartLine, tokenStartColumn));
                    }
                    else
                    {
                        tokens.Add(new Token(word, TokenTypes.Identifier, tokenStartLine, tokenStartColumn));
                    }
                    continue;
                }

                throw new SyntaxError($"Unexpected character: {currentChar} at [{currentLine}:{currentColumn}]");
            }

            Console.WriteLine($"Returning tokens length is {tokens.Count}");
            return tokens;
        }
    }
}
