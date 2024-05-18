# Expression Machine
Unmanaged library for evaluating simple expressions at runtime.

### Exceptions and memory Leaks
This library is unmanaged, and currently doesn't have tools to handle compilation
or parsing errors, which will cause memory leaks. This means all given expressions
must be perfect when given to a virtual machine.

### Characteristics
- Basic arithmetic operations (addition, subtraction, multiplication, division)
- Parentheses for grouping operations
- Injectable variables as floats
- Injectable functions accepting 1 or 0 input arguments

### Usage example
Below is an example that returns coordinates for 360 points of a circle with either
a radius or a diameter as input. While reusing the same machine instance by modifying
its source and variables.
```csharp
public List<Vector2> GetCirclePositions(float value, bool isDiameter)
{
    List<Vector2> positions = new();
    using Machine vm = new();
    vm.SetVariable("value", value);
    vm.SetVariable("multiplier", isDiameter ? 2 : 1);

    unsafe
    {
        vm.SetFunction("cos", &Cos);
        vm.SetFunction("sin", &Sin);
    }

    for (int i = 0; i < 360; i++)
    {
        float t = i * MathF.PI / 180;
        vm.SetVariable("t", t);
        vm.SetSource("cos(t) * (value * multiplier)");
        float x = vm.Evaluate();    
        vm.SetSource("sin(t) * (value * multiplier)");
        float y = vm.Evaluate();
        positions.Add(new Vector2(x, y));
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

    return positions;
}
```

### Contributions and direction
This library is small and isn't mean to substitute things like Lua or other
languages within interpreters. Instead, it's more fitting as a base to extend upon and
branch away. And it should remain as unmanaged as it is. Without extending it, it's
most useful fruit is allowing your code to express different values, all through a single C# variable via different expressions.

Contributions that align with this are welcome.
