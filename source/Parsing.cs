using ExpressionMachine.Unsafe;
using System;
using Unmanaged.Collections;

namespace ExpressionMachine
{
    public static unsafe class Parsing
    {
        public static unsafe UnmanagedList<Token> GetTokens(ReadOnlySpan<char> expression)
        {
            UnmanagedList<Token> tokens = new();
            GetTokens(expression, tokens);
            return tokens;
        }

        public static unsafe UnmanagedList<Token> GetTokens(ReadOnlySpan<char> expression, TokenMap map)
        {
            UnmanagedList<Token> tokens = new();
            GetTokens(expression, map, tokens);
            return tokens;
        }

        public static unsafe void GetTokens(ReadOnlySpan<char> expression, UnmanagedList<Token> tokens)
        {
            using TokenMap map = new();
            GetTokens(expression, map, tokens);
        }

        /// <summary>
        /// Populates the given list with tokens from the given expression.
        /// </summary>
        public static unsafe void GetTokens(ReadOnlySpan<char> expression, TokenMap map, UnmanagedList<Token> tokens)
        {
            uint position = 0;
            uint length = (uint)expression.Length;
            while (position < length)
            {
                char current = expression[(int)position];
                if (map.Ignore.Contains(current))
                {
                    position++;
                    continue;
                }

                int tokenIndex = map.Tokens.IndexOf(current);
                if (tokenIndex != -1)
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
                        char c = expression[(int)position];
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

        public static Node GetTree(ReadOnlySpan<Token> tokens)
        {
            uint position = 0;
            return new(TryParseExpression(ref position, tokens));
        }

        private static UnsafeNode* TryParseExpression(ref uint position, ReadOnlySpan<Token> tokens)
        {
            UnsafeNode* result = TryReadFactor(ref position, tokens);
            if (position == tokens.Length)
            {
                return result;
            }

            Token current = tokens[(int)position];
            while (result is not null && position < tokens.Length && IsTerm(current.type))
            {
                if (current.type == Token.Type.Add)
                {
                    position++;
                    UnsafeNode* right = TryReadFactor(ref position, tokens);
                    result = UnsafeNode.Allocate(NodeType.Addition, result, right);
                }
                else if (current.type == Token.Type.Subtract)
                {
                    position++;
                    UnsafeNode* right = TryReadFactor(ref position, tokens);
                    result = UnsafeNode.Allocate(NodeType.Subtraction, result, right);
                }

                if (position == tokens.Length)
                {
                    break;
                }

                current = tokens[(int)position];
            }

            return result;
        }

        private static UnsafeNode* TryReadFactor(ref uint position, ReadOnlySpan<Token> tokens)
        {
            UnsafeNode* factor = TryReadTerm(ref position, tokens);
            if (position == tokens.Length)
            {
                return factor;
            }

            Token current = tokens[(int)position];
            while (factor is not null && position < tokens.Length && IsFactor(current.type))
            {
                if (current.type == Token.Type.Multiply)
                {
                    position++;
                    UnsafeNode* right = TryReadTerm(ref position, tokens);
                    factor = UnsafeNode.Allocate(NodeType.Multiplication, factor, right);
                }
                else if (current.type == Token.Type.Divide)
                {
                    position++;
                    UnsafeNode* right = TryReadTerm(ref position, tokens);
                    factor = UnsafeNode.Allocate(NodeType.Division, factor, right);
                }

                if (position == tokens.Length)
                {
                    break;
                }

                current = tokens[(int)position];
            }

            return factor;
        }

        private static UnsafeNode* TryReadTerm(ref uint position, ReadOnlySpan<Token> tokens)
        {
            if (position == tokens.Length)
            {
                Token lastToken = tokens[(int)position - 1];
                throw new FormatException($"Expected a token after {lastToken}.");
            }

            Token current = tokens[(int)position];
            position++;
            if (current.type == Token.Type.OpenParenthesis)
            {
                UnsafeNode* term = TryParseExpression(ref position, tokens);
                current = tokens[(int)position];
                if (current.type != Token.Type.CloseParenthesis)
                {
                    throw new FormatException("Expected closing parenthesis.");
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
                    var next = tokens[(int)position];
                    if (next.type == Token.Type.OpenParenthesis)
                    {
                        position++;
                        UnsafeNode* argument = TryParseExpression(ref position, tokens);
                        if (position < tokens.Length)
                        {
                            current = tokens[(int)position];
                            if (current.type != Token.Type.CloseParenthesis)
                            {
                                throw new FormatException("Expected closing parenthesis.");
                            }

                            position++;
                        }

                        return UnsafeNode.Allocate(start, length, argument);
                    }
                }

                return UnsafeNode.Allocate(start, length);
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