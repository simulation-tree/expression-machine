using Collections;
using System;
using Unmanaged;

namespace ExpressionMachine
{
    public readonly struct TokenMap : IDisposable
    {
        private readonly Array<char> tokens;
        private readonly List<char> ignore;

        /// <summary>
        /// Individual characters that correspond to the <see cref="Token.Type"/>.
        /// </summary>
        public readonly USpan<char> Tokens => tokens.AsSpan();

        /// <summary>
        /// Characters to ignore when parsing (whitespace).
        /// </summary>
        public readonly USpan<char> Ignore => ignore.AsSpan();

        public readonly bool IsDisposed => tokens.IsDisposed;

        public readonly char this[Token.Type index]
        {
            get => tokens[(uint)index];
            set => tokens[(uint)index] = value;
        }

        public TokenMap()
        {
            tokens = new((uint)Token.Type.Length);
            tokens[(uint)Token.Type.Value] = default;
            tokens[(uint)Token.Type.Add] = '+';
            tokens[(uint)Token.Type.Subtract] = '-';
            tokens[(uint)Token.Type.Multiply] = '*';
            tokens[(uint)Token.Type.Divide] = '/';
            tokens[(uint)Token.Type.OpenParenthesis] = '(';
            tokens[(uint)Token.Type.CloseParenthesis] = ')';

            ignore = new(2);
            ignore.Add(' ');
            ignore.Add('\t');
        }

        public readonly void Dispose()
        {
            ignore.Dispose();
            tokens.Dispose();
        }
    }
}