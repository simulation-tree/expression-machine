# Expression Machine

Library for evaluating logic expressions at runtime.

### Features

- Basic arithmetic operations (addition, subtraction, multiplication, division)
- Parentheses for grouping operations
- Injectable `float` variables
- Injectable functions accepting one or no input arguments

### Example

Below is an example that fills a destination span with coordinates for the points of a circle,
with either a radius or a diameter as input. While reusing the same machine instance by modifying
its source and variables, and re-evaluating the expression.
```cs
public unsafe void GetCirclePositions(float value, bool isDiameter, USpan<Vector2> positions)
{
    using Machine vm = new();
    vm.SetVariable("value", value);
    vm.SetVariable("multiplier", isDiameter ? 2 : 1);
    vm.SetFunction("cos", &Cos);
    vm.SetFunction("sin", &Sin);

    uint length = positions.Length;
    for (int i = 0; i < length; i++)
    {
        float t = i * MathF.PI / (length * 0.5f);
        vm.SetVariable("t", t);
        vm.SetSource("cos(t) * (value * multiplier)");
        float x = vm.Evaluate();    
        vm.SetSource("sin(t) * (value * multiplier)");
        float y = vm.Evaluate();
        positions[i] = new Vector2(x, y);
    }

    [UnmanagedCallersOnly]
    static float Cos(float value)
    {
        return MathF.Cos(value);
    }

    [UnmanagedCallersOnly]
    static float Sin(float value)
    {
        return MathF.Sin(value);
    }
}
```

### Contributions and direction

This library is small and isn't mean to substitute things like Lua or other languages within interpreters.
Instead, it's more fitting as a base to extend upon and branch away. And it should remain as unmanaged as it is.
Without extending it, it's most useful fruit is allowing your code to express different values, all through a
single C# variable via different expressions.

Contributions that align with this are welcome.