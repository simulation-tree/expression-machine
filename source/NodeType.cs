namespace ExpressionMachine
{
    /// <summary>
    /// Type of a <see cref="Node"/>.
    /// </summary>
    public enum NodeType : byte
    {
        /// <summary>
        /// Not valid.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Contains a value.
        /// </summary>
        Value = 1,

        /// <summary>
        /// Describes addition between two <see cref="Node"/>s.
        /// </summary>
        Addition = 2,

        /// <summary>
        /// Describes subtraction between two <see cref="Node"/>s.
        /// </summary>
        Subtraction = 3,

        /// <summary>
        /// Describes multiplication between two <see cref="Node"/>s.
        /// </summary>
        Multiplication = 4,

        /// <summary>
        /// Describes division between two <see cref="Node"/>s.
        /// </summary>
        Division = 5,

        /// <summary>
        /// Describes a function call.
        /// </summary>
        Call = 6,
    }
}