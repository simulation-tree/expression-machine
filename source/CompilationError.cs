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
        public readonly CompilationResult.Type type;

        /// <summary>
        /// Additional error message.
        /// </summary>
        public readonly ASCIIText256 errorMessage;

        /// <summary>
        /// Creates an instance.
        /// </summary>
        public CompilationError(CompilationResult.Type type, ReadOnlySpan<char> errorMessage)
        {
            this.type = type;
            this.errorMessage = errorMessage;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            return $"{type}: {errorMessage}";
        }

        /// <summary>
        /// Retrieves this error as an exception.
        /// </summary>
        public readonly Exception GetException()
        {
            return new(ToString());
        }
    }
}