using System;
using Unmanaged;

namespace ExpressionMachine
{
    public readonly struct TokenMap : IDisposable
    {
        private readonly Text tokens;
        private readonly Text ignore;

        /// <summary>
        /// Individual characters that correspond to the <see cref="Token.Type"/>.
        /// </summary>
        public readonly USpan<char> Tokens => tokens.AsSpan();

        /// <summary>
        /// Characters to ignore when parsing (whitespace).
        /// </summary>
        public readonly USpan<char> Ignore => ignore.AsSpan();

        public readonly bool IsDisposed => tokens.IsDisposed;

        public readonly ref char this[Token.Type index] => ref tokens[(uint)index];

        public TokenMap()
        {
            Token.Type[] options = Enum.GetValues<Token.Type>();
            tokens = new((uint)options.Length);
            tokens[(uint)Token.Type.Value] = default;
            tokens[(uint)Token.Type.Add] = '+';
            tokens[(uint)Token.Type.Subtract] = '-';
            tokens[(uint)Token.Type.Multiply] = '*';
            tokens[(uint)Token.Type.Divide] = '/';
            tokens[(uint)Token.Type.OpenParenthesis] = '(';
            tokens[(uint)Token.Type.CloseParenthesis] = ')';

            ignore = new(0);
            ignore.Append(' ');
            ignore.Append('\t');
        }

        public readonly void Dispose()
        {
            ignore.Dispose();
            tokens.Dispose();
        }
    }
}