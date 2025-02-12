using System;
using System.Diagnostics;
using Unmanaged;

namespace ExpressionMachine
{
    /// <summary>
    /// Stores a map of tokens and characters.
    /// </summary>
    public readonly struct TokenMap : IDisposable
    {
        private readonly Text tokens;
        private readonly Text ignore;

        /// <summary>
        /// Individual characters that correspond to the <see cref="Token.Type"/>.
        /// </summary>
        public readonly USpan<char> Tokens
        {
            get
            {
                ThrowIfDisposed();

                return tokens.AsSpan();
            }
        }

        /// <summary>
        /// Characters to ignore when parsing (whitespace).
        /// </summary>
        public readonly USpan<char> Ignore
        {
            get
            {
                ThrowIfDisposed();

                return ignore.AsSpan();
            }
        }

        /// <summary>
        /// Checks if the <see cref="TokenMap"/> has been disposed.
        /// </summary>
        public readonly bool IsDisposed => tokens.IsDisposed;

        /// <inheritdoc/>
        public readonly ref char this[Token.Type index] => ref tokens[(uint)index];

        /// <summary>
        /// Creates a new token map.
        /// </summary>
        public TokenMap()
        {
            Token.Type[] options = Enum.GetValues<Token.Type>();
            tokens = new((uint)options.Length);
            tokens[(uint)Token.Type.Value] = default;
            tokens[(uint)Token.Type.Add] = '+';
            tokens[(uint)Token.Type.Subtract] = '-';
            tokens[(uint)Token.Type.Multiply] = '*';
            tokens[(uint)Token.Type.Divide] = '/';
            tokens[(uint)Token.Type.BeginGroup] = '(';
            tokens[(uint)Token.Type.EndGroup] = ')';

            ignore = new(0);
            ignore.Append(' ');
            ignore.Append('\t');
        }

        /// <summary>
        /// Disposes the token map.
        /// </summary>
        public readonly void Dispose()
        {
            ThrowIfDisposed();

            ignore.Dispose();
            tokens.Dispose();
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(TokenMap));
            }
        }
    }
}