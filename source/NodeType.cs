namespace ExpressionMachine
{
    public enum NodeType : byte
    {
        Unknown = 0,
        Value = 1,
        Addition = 2,
        Subtraction = 3,
        Multiplication = 4,
        Division = 5,
        Call = 6,
    }
}