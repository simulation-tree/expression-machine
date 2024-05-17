namespace ExpressionMachine
{
    public readonly unsafe struct Function(delegate* unmanaged<float, float> function)
    {
        private readonly delegate* unmanaged<float, float> function = function;

        public readonly float Invoke(float value)
        {
            return function(value);
        }
    }
}