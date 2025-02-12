using Collections;
using System;
using Unmanaged;

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
        public static List<Token> GetTokens(USpan<char> expression)
        {
            List<Token> tokens = new();
            GetTokens(expression, tokens);
            return tokens;
        }

        /// <summary>
        /// Retrieves a list of <see cref="Token"/>s from the given <paramref name="expression"/>.
        /// </summary>
        public static List<Token> GetTokens(USpan<char> expression, TokenMap map)
        {
            List<Token> tokens = new();
            GetTokens(expression, map, tokens);
            return tokens;
        }

        /// <summary>
        /// Fills the given <paramref name="list"/> list with tokens from the given <paramref name="expression"/>.
        /// </summary>
        public static void GetTokens(USpan<char> expression, List<Token> list)
        {
            using TokenMap map = new();
            GetTokens(expression, map, list);
        }

        /// <summary>
        /// Populates the given <paramref name="list"/> with tokens from the given <paramref name="expression"/>.
        /// </summary>
        public static void GetTokens(USpan<char> expression, TokenMap map, List<Token> list)
        {
            uint position = 0;
            uint length = expression.Length;
            while (position < length)
            {
                char current = expression[position];
                if (map.Ignore.Contains(current))
                {
                    position++;
                    continue;
                }

                if (map.Tokens.TryIndexOf(current, out uint tokenIndex))
                {
                    Token.Type type = (Token.Type)tokenIndex;
                    list.Add(new(type, position, 1));
                    position++;
                }
                else
                {
                    uint start = position;
                    position++;
                    while (position < length)
                    {
                        char c = expression[position];
                        if (!map.Tokens.Contains(c) && !map.Ignore.Contains(c))
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
        public static Node GetTree(USpan<Token> tokens)
        {
            uint position = 0;
            return new(TryParseExpression(ref position, tokens));
        }

        private static Node.Implementation* TryParseExpression(ref uint position, USpan<Token> tokens)
        {
            //todo: handle control nodes like if, else if, else, do, goto, and while
            Node.Implementation* result = TryReadFactor(ref position, tokens);
            if (position == tokens.Length)
            {
                return result;
            }

            Token current = tokens[position];
            while (result is not null && position < tokens.Length && IsTerm(current.type))
            {
                if (current.type == Token.Type.Add)
                {
                    position++;
                    Node.Implementation* right = TryReadFactor(ref position, tokens);
                    result = Node.Implementation.Allocate(NodeType.Addition, (nint)result, (nint)right, default);
                }
                else if (current.type == Token.Type.Subtract)
                {
                    position++;
                    Node.Implementation* right = TryReadFactor(ref position, tokens);
                    result = Node.Implementation.Allocate(NodeType.Subtraction, (nint)result, (nint)right, default);
                }

                if (position == tokens.Length)
                {
                    break;
                }

                current = tokens[position];
            }

            return result;
        }

        private static Node.Implementation* TryReadFactor(ref uint position, USpan<Token> tokens)
        {
            Node.Implementation* factor = TryReadTerm(ref position, tokens);
            if (position == tokens.Length)
            {
                return factor;
            }

            Token current = tokens[position];
            while (factor is not null && position < tokens.Length && IsFactor(current.type))
            {
                if (current.type == Token.Type.Multiply)
                {
                    position++;
                    Node.Implementation* right = TryReadTerm(ref position, tokens);
                    factor = Node.Implementation.Allocate(NodeType.Multiplication, (nint)factor, (nint)right, default);
                }
                else if (current.type == Token.Type.Divide)
                {
                    position++;
                    Node.Implementation* right = TryReadTerm(ref position, tokens);
                    factor = Node.Implementation.Allocate(NodeType.Division, (nint)factor, (nint)right, default);
                }

                if (position == tokens.Length)
                {
                    break;
                }

                current = tokens[position];
            }

            return factor;
        }

        private static Node.Implementation* TryReadTerm(ref uint position, USpan<Token> tokens)
        {
            if (position == tokens.Length)
            {
                Token lastToken = tokens[position - 1];
                throw new FormatException($"Expected a token after {lastToken}");
            }

            Token current = tokens[position];
            position++;
            if (current.type == Token.Type.BeginGroup)
            {
                Node.Implementation* term = TryParseExpression(ref position, tokens);
                current = tokens[position];
                if (current.type != Token.Type.EndGroup)
                {
                    throw new FormatException("Expected closing parenthesis");
                }

                position++;
                return term;
            }
            else if (current.type == Token.Type.Value)
            {
                uint start = current.start;
                uint length = current.length;
                if (position < tokens.Length)
                {
                    var next = tokens[position];
                    if (next.type == Token.Type.BeginGroup)
                    {
                        position++;
                        Node.Implementation* argument = TryParseExpression(ref position, tokens);
                        if (position < tokens.Length)
                        {
                            current = tokens[position];
                            if (current.type != Token.Type.EndGroup)
                            {
                                throw new FormatException("Expected closing parenthesis");
                            }

                            position++;
                        }

                        return Node.Implementation.Allocate(NodeType.Call, (nint)start, (nint)length, (nint)argument);
                    }
                }

                return Node.Implementation.Allocate(NodeType.Value, (nint)start, (nint)length, default);
            }
            else
            {
                return null;
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