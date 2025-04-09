using System;

namespace ExpressionMachine
{
    /// <summary>
    /// Represents the result of compilation.
    /// </summary>
    public readonly struct CompilationResult : IEquatable<CompilationResult>
    {
        /// <summary>
        /// Success compilation result.
        /// </summary>
        public static CompilationResult Success => new(null);

        /// <summary>
        /// Compilation exception if there was one.
        /// </summary>
        public readonly Exception? exception;

        /// <summary>
        /// Checks if the compilation was successful.
        /// </summary>
        public readonly bool IsSuccess => exception is null;

        internal CompilationResult(Exception? exception)
        {
            this.exception = exception;
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is CompilationResult result && Equals(result);
        }

        /// <inheritdoc/>
        public readonly bool Equals(CompilationResult other)
        {
            return exception == other.exception;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return exception?.GetHashCode() ?? 0;
        }

        /// <inheritdoc/>
        public static bool operator ==(CompilationResult left, CompilationResult right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(CompilationResult left, CompilationResult right)
        {
            return !(left == right);
        }
    }
}