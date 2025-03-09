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
        public readonly ReadOnlySpan<char> Tokens
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
        public readonly ReadOnlySpan<char> Ignore
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
        public readonly ref char this[Token.Type index] => ref tokens[(int)index];

        /// <summary>
        /// Creates a new token map.
        /// </summary>
        public TokenMap()
        {
            Token.Type[] options = Enum.GetValues<Token.Type>();
            tokens = new((int)options.Length);
            tokens[(int)Token.Type.Value] = default;
            tokens[(int)Token.Type.Add] = '+';
            tokens[(int)Token.Type.Subtract] = '-';
            tokens[(int)Token.Type.Multiply] = '*';
            tokens[(int)Token.Type.Divide] = '/';
            tokens[(int)Token.Type.BeginGroup] = '(';
            tokens[(int)Token.Type.EndGroup] = ')';

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