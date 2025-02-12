namespace ExpressionMachine
{
    /// <summary>
    /// A token in an expression.
    /// </summary>
    public readonly struct Token
    {
        /// <summary>
        /// Type of the token.
        /// </summary>
        public readonly Type type;

        /// <summary>
        /// Starting position of the token in the expression.
        /// </summary>
        public readonly uint start;

        /// <summary>
        /// Length of the token.
        /// </summary>
        public readonly uint length;

        /// <summary>
        /// Creates a new token.
        /// </summary>
        public Token(Type type, uint start, uint length)
        {
            this.type = type;
            this.start = start;
            this.length = length;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            return $"{type} (start: {start}, length: {length})";
        }

        /// <summary>
        /// Type of a <see cref="Token"/>.
        /// </summary>
        public enum Type : byte
        {
            /// <summary>
            /// A value.
            /// </summary>
            Value,

            /// <summary>
            /// Addition.
            /// </summary>
            Add,

            /// <summary>
            /// Subtraction.
            /// </summary>
            Subtract,

            /// <summary>
            /// Multiplication.
            /// </summary>
            Multiply,

            /// <summary>
            /// Division.
            /// </summary>
            Divide,

            /// <summary>
            /// Begin a group.
            /// </summary>
            BeginGroup,

            /// <summary>
            /// End a group.
            /// </summary>
            EndGroup
        }
    }
}