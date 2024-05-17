namespace ExpressionMachine
{
    public readonly struct Token(Token.Type type, uint start, uint length)
    {
        public readonly Type type = type;
        public readonly uint start = start;
        public readonly uint length = length;

        public readonly override string ToString()
        {
            return $"{type} (start: {start}, length: {length})";
        }

        public enum Type : byte
        {
            Value,
            Add,
            Subtract,
            Multiply,
            Divide,
            OpenParenthesis,
            CloseParenthesis,
            Length
        }
    }
}