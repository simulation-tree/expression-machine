using System;
using Unmanaged;

namespace ExpressionMachine
{
    /// <summary>
    /// Represents a compilation error in the code.
    /// </summary>
    public readonly struct CompilationError
    {
        /// <summary>
        /// Type of compilation error.
        /// </summary>
        public readonly Type type;

        /// <summary>
        /// Additional error message.
        /// </summary>
        public readonly ASCIIText256 message;

        /// <summary>
        /// Creates an instance.
        /// </summary>
        public CompilationError(Type type, ReadOnlySpan<char> message)
        {
            this.type = type;
            this.message = message;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            return $"{type}: {message}";
        }

        /// <summary>
        /// Retrieves this error as an exception.
        /// </summary>
        public readonly Exception GetException()
        {
            return new(ToString());
        }

        /// <summary>
        /// Defines all built-in compilation error types.
        /// </summary>
        public enum Type
        {
            /// <summary>
            /// Not an error.
            /// </summary>
            None,

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