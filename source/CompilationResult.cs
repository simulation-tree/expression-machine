using System;
using Unmanaged;

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
        public static CompilationResult Success => new(Type.Success);

        /// <summary>
        /// Result type.
        /// </summary>
        public readonly Type type;

        /// <summary>
        /// Error message if compilation wasn't successful.
        /// </summary>
        public readonly ASCIIText256 errorMessage;

        /// <summary>
        /// Checks if the compilation was successful.
        /// </summary>
        public readonly bool IsSuccess => type == Type.Success;

        internal CompilationResult(CompilationError error)
        {
            type = error.type;
            errorMessage = error.errorMessage;
        }

        private CompilationResult(Type type)
        {
            this.type = type;
            errorMessage = default;
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is CompilationResult result && Equals(result);
        }

        /// <inheritdoc/>
        public readonly bool Equals(CompilationResult other)
        {
            return type == other.type && errorMessage.Equals(other.errorMessage);
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return HashCode.Combine(type, errorMessage);
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

        /// <summary>
        /// Defines all built-in compilation error types.
        /// </summary>
        public enum Type
        {
            /// <summary>
            /// Default value.
            /// </summary>
            None,

            /// <summary>
            /// Compilation successful.
            /// </summary>
            Success,

            /// <summary>
            /// An additional token was expected but is missing.
            /// </summary>
            ExpectedAdditionalToken,

            /// <summary>
            /// A group closing token was epxected but is missing.
            /// </summary>
            ExpectedGroupCloseToken
        }
    }
}