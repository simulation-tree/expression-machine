using Collections.Generic;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ExpressionMachine
{
    /// <summary>
    /// Functions to process text into a span of <see cref="Token"/>s.
    /// </summary>
    public static unsafe class Parsing
    {
        /// <summary>
        /// Retrieves a list of <see cref="Token"/>s from the given <paramref name="expression"/>.
        /// </summary>
        public static List<Token> GetTokens(ReadOnlySpan<char> expression)
        {
            List<Token> tokens = new();
            GetTokens(expression, tokens);
            return tokens;
        }

        /// <summary>
        /// Retrieves a list of <see cref="Token"/>s from the given <paramref name="expression"/>.
        /// </summary>
        public static List<Token> GetTokens(ReadOnlySpan<char> expression, TokenMap map)
        {
            List<Token> tokens = new();
            GetTokens(expression, map, tokens);
            return tokens;
        }

        /// <summary>
        /// Fills the given <paramref name="list"/> list with tokens from the given <paramref name="expression"/>.
        /// </summary>
        public static void GetTokens(ReadOnlySpan<char> expression, List<Token> list)
        {
            using TokenMap map = new();
            GetTokens(expression, map, list);
        }

        /// <summary>
        /// Populates the given <paramref name="list"/> with tokens from the given <paramref name="expression"/>.
        /// </summary>
        public static void GetTokens(ReadOnlySpan<char> expression, TokenMap map, List<Token> list)
        {
            int position = 0;
            int length = expression.Length;
            ReadOnlySpan<char> ignore = map.Ignore;
            ReadOnlySpan<char> tokens = map.Tokens;
            while (position < length)
            {
                char current = expression[position];
                if (ignore.Contains(current))
                {
                    position++;
                    continue;
                }

                if (tokens.TryIndexOf(current, out int tokenIndex))
                {
                    Token.Type type = (Token.Type)tokenIndex;
                    list.Add(new(type, position, 1));
                    position++;
                }
                else
                {
                    int start = position;
                    position++;
                    while (position < length)
                    {
                        char c = expression[position];
                        if (!tokens.Contains(c) && !ignore.Contains(c))
                        {
                            position++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    list.Add(new(Token.Type.Value, start, position - start));
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="Node"/> containing the expression represented
        /// by the given <paramref name="tokens"/>.
        /// </summary>
        public static bool TryGetTree(ReadOnlySpan<Token> tokens, out Node node, [NotNullWhen(false)] out Exception? exception)
        {
            int position = 0;
            return TryParseExpression(ref position, tokens, out node, out exception);
        }

        private static bool TryParseExpression(ref int position, ReadOnlySpan<Token> tokens, out Node node, [NotNullWhen(false)] out Exception? exception)
        {
            //todo: handle control nodes like if, else if, else, do, goto, and while
            if (TryReadFactor(ref position, tokens, out node, out exception))
            {
                if (position == tokens.Length)
                {
                    return true;
                }

                Token current = tokens[position];
                while (node != default && position < tokens.Length && IsTerm(current.type))
                {
                    if (current.type == Token.Type.Add)
                    {
                        position++;
                        if (TryReadFactor(ref position, tokens, out Node right, out exception))
                        {
                            node = new(NodeType.Addition, node.Address, right.Address, default);
                        }
                        else
                        {
                            node.Dispose();
                            node = default;
                            return false;
                        }
                    }
                    else if (current.type == Token.Type.Subtract)
                    {
                        position++;
                        if (TryReadFactor(ref position, tokens, out Node right, out exception))
                        {
                            node = new(NodeType.Subtraction, node.Address, right.Address, default);
                        }
                        else
                        {
                            node.Dispose();
                            node = default;
                            return false;
                        }
                    }

                    if (position == tokens.Length)
                    {
                        break;
                    }

                    current = tokens[position];
                }

                return true;
            }
            else
            {
                node = default;
                return false;
            }
        }

        private static bool TryReadFactor(ref int position, ReadOnlySpan<Token> tokens, out Node node, [NotNullWhen(false)] out Exception? exception)
        {
            if (TryReadTerm(ref position, tokens, out node, out exception))
            {
                if (position == tokens.Length)
                {
                    return true;
                }

                Token current = tokens[position];
                while (node != default && position < tokens.Length && IsFactor(current.type))
                {
                    if (current.type == Token.Type.Multiply)
                    {
                        position++;
                        if (TryReadTerm(ref position, tokens, out Node right, out exception))
                        {
                            node = new(NodeType.Multiplication, node.Address, right.Address, default);
                        }
                        else
                        {
                            node.Dispose();
                            node = default;
                            return false;
                        }
                    }
                    else if (current.type == Token.Type.Divide)
                    {
                        position++;
                        if (TryReadTerm(ref position, tokens, out Node right, out exception))
                        {
                            node = new(NodeType.Division, node.Address, right.Address, default);
                        }
                        else
                        {
                            node.Dispose();
                            node = default;
                            return false;
                        }
                    }

                    if (position == tokens.Length)
                    {
                        break;
                    }

                    current = tokens[position];
                }

                return true;
            }
            else
            {
                node = default;
                return false;
            }
        }

        private static bool TryReadTerm(ref int position, ReadOnlySpan<Token> tokens, out Node node, [NotNullWhen(false)] out Exception? exception)
        {
            if (position == tokens.Length)
            {
                Token lastToken = tokens[position - 1];
                node = default;
                exception = new MissingTokenException();
                return false;
            }

            Token current = tokens[position];
            position++;
            if (current.type == Token.Type.BeginGroup)
            {
                if (TryParseExpression(ref position, tokens, out node, out exception))
                {
                    current = tokens[position];
                    if (current.type != Token.Type.EndGroup)
                    {
                        node.Dispose();
                        node = default;
                        exception = new MissingGroupCloseToken();
                        return false;
                    }

                    position++;
                    return true;
                }
                else
                {
                    node = default;
                    return false;
                }
            }
            else if (current.type == Token.Type.Value)
            {
                int start = current.start;
                int length = current.length;
                if (position < tokens.Length)
                {
                    Token next = tokens[position];
                    if (next.type == Token.Type.BeginGroup)
                    {
                        position++;
                        if (TryParseExpression(ref position, tokens, out Node argument, out exception))
                        {
                            if (position < tokens.Length)
                            {
                                current = tokens[position];
                                if (current.type != Token.Type.EndGroup)
                                {
                                    argument.Dispose();
                                    node = default;
                                    exception = new MissingGroupCloseToken();
                                    return false;
                                }

                                position++;
                            }

                            node = new(NodeType.Call, start, length, argument.Address);
                            exception = null;
                            return true;
                        }
                        else
                        {
                            node = default;
                            return false;
                        }
                    }
                }

                node = new(NodeType.Value, start, length, default);
                exception = null;
                return true;
            }
            else
            {
                node = default;
                exception = null;
                return true;
            }
        }

        private static bool IsFactor(Token.Type type)
        {
            return type == Token.Type.Multiply || type == Token.Type.Divide;
        }

        private static bool IsTerm(Token.Type type)
        {
            return type == Token.Type.Add || type == Token.Type.Subtract;
        }
    }
}