using Collections;
using System;
using Unmanaged;

namespace ExpressionMachine
{
    public static unsafe class Parsing
    {
        public static List<Token> GetTokens(USpan<char> expression)
        {
            List<Token> tokens = new();
            GetTokens(expression, tokens);
            return tokens;
        }

        public static List<Token> GetTokens(USpan<char> expression, TokenMap map)
        {
            List<Token> tokens = new();
            GetTokens(expression, map, tokens);
            return tokens;
        }

        public static void GetTokens(USpan<char> expression, List<Token> tokens)
        {
            using TokenMap map = new();
            GetTokens(expression, map, tokens);
        }

        /// <summary>
        /// Populates the given list with tokens from the given expression.
        /// </summary>
        public static void GetTokens(USpan<char> expression, TokenMap map, List<Token> tokens)
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
                    tokens.Add(new(type, position, 1));
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

                    tokens.Add(new(Token.Type.Value, start, position - start));
                }
            }
        }

        public static Node GetTree(USpan<Token> tokens)
        {
            uint position = 0;
            return new(TryParseExpression(ref position, tokens));
        }

        private static Node.Implementation* TryParseExpression(ref uint position, USpan<Token> tokens)
        {
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
                    result = Node.Implementation.Allocate(NodeType.Addition, result, right);
                }
                else if (current.type == Token.Type.Subtract)
                {
                    position++;
                    Node.Implementation* right = TryReadFactor(ref position, tokens);
                    result = Node.Implementation.Allocate(NodeType.Subtraction, result, right);
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
                    factor = Node.Implementation.Allocate(NodeType.Multiplication, factor, right);
                }
                else if (current.type == Token.Type.Divide)
                {
                    position++;
                    Node.Implementation* right = TryReadTerm(ref position, tokens);
                    factor = Node.Implementation.Allocate(NodeType.Division, factor, right);
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
            if (current.type == Token.Type.OpenParenthesis)
            {
                Node.Implementation* term = TryParseExpression(ref position, tokens);
                current = tokens[position];
                if (current.type != Token.Type.CloseParenthesis)
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
                    if (next.type == Token.Type.OpenParenthesis)
                    {
                        position++;
                        Node.Implementation* argument = TryParseExpression(ref position, tokens);
                        if (position < tokens.Length)
                        {
                            current = tokens[position];
                            if (current.type != Token.Type.CloseParenthesis)
                            {
                                throw new FormatException("Expected closing parenthesis");
                            }

                            position++;
                        }

                        return Node.Implementation.Allocate(start, length, argument);
                    }
                }

                return Node.Implementation.Allocate(start, length);
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