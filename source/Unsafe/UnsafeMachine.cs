using Collections;
using System.Collections.Generic;
using Unmanaged;

namespace ExpressionMachine.Unsafe
{
    public unsafe struct UnsafeMachine
    {
        private TokenMap map;
        private Array<char> source;
        private Collections.Dictionary<int, float> variables;
        private Collections.Dictionary<int, Function> functions;
        private Collections.List<Token> tokens;
        private Node tree;

        public static UnsafeMachine* Allocate()
        {
            UnsafeMachine* machine = Allocations.Allocate<UnsafeMachine>();
            machine->map = new();
            machine->source = new();
            machine->variables = new();
            machine->functions = new();
            machine->tokens = new();
            machine->tree = new();
            return machine;
        }

        public static bool IsDisposed(UnsafeMachine* machine)
        {
            return machine is null;
        }

        public static void Free(ref UnsafeMachine* machine)
        {
            Allocations.ThrowIfNull(machine);

            machine->tokens.Dispose();
            machine->map.Dispose();
            machine->functions.Dispose();
            machine->variables.Dispose();
            machine->source.Dispose();
            machine->tree.Dispose();
            Allocations.Free(ref machine);
        }

        public static void ClearFunctions(UnsafeMachine* machine)
        {
            Allocations.ThrowIfNull(machine);

            machine->functions.Clear();
        }

        public static void ClearVariables(UnsafeMachine* machine)
        {
            Allocations.ThrowIfNull(machine);

            machine->variables.Clear();
        }

        public static float GetVariable(UnsafeMachine* machine, USpan<char> name)
        {
            Allocations.ThrowIfNull(machine);

            int hash = new FixedString(name).GetHashCode();
            if (machine->variables.TryGetValue(hash, out float value))
            {
                return value;
            }
            else
            {
                throw new KeyNotFoundException($"Variable '{name.ToString()}' not found");
            }
        }

        public static bool ContainsVariable(UnsafeMachine* machine, USpan<char> name)
        {
            Allocations.ThrowIfNull(machine);

            int hash = new FixedString(name).GetHashCode();
            return machine->variables.ContainsKey(hash);
        }

        public static void SetVariable(UnsafeMachine* machine, USpan<char> name, float value)
        {
            Allocations.ThrowIfNull(machine);

            int hash = new FixedString(name).GetHashCode();
            machine->variables.AddOrSet(hash, value);
        }

        public static void SetFunction(UnsafeMachine* machine, USpan<char> name, Function function)
        {
            Allocations.ThrowIfNull(machine);

            int hash = new FixedString(name).GetHashCode();
            machine->functions.AddOrSet(hash, function);
        }

        public static float InvokeFunction(UnsafeMachine* machine, USpan<char> name, float value)
        {
            Allocations.ThrowIfNull(machine);

            int hash = new FixedString(name).GetHashCode();
            if (machine->functions.TryGetValue(hash, out Function function))
            {
                return function.Invoke(value);
            }
            else
            {
                throw new KeyNotFoundException($"Function '{name.ToString()}' not found");
            }
        }

        public static void SetSource(UnsafeMachine* machine, USpan<char> newSource)
        {
            Allocations.ThrowIfNull(machine);

            USpan<char> currentSource = machine->source.AsSpan();
            if (!newSource.SequenceEqual(currentSource))
            {
                machine->source.Length = newSource.Length;
                USpan<char> span = machine->source.AsSpan();
                newSource.CopyTo(span);

                machine->tokens.Clear();
                Parsing.GetTokens(newSource, machine->map, machine->tokens);

                machine->tree.Dispose();
                machine->tree = Parsing.GetTree(machine->tokens.AsSpan());
            }
        }

        public static USpan<char> GetSource(UnsafeMachine* machine)
        {
            Allocations.ThrowIfNull(machine);

            return machine->source.AsSpan();
        }

        public static USpan<char> GetToken(UnsafeMachine* machine, uint start, uint length)
        {
            Allocations.ThrowIfNull(machine);

            return machine->source.AsSpan().Slice(start, length);
        }

        public static float Evaluate(UnsafeMachine* machine)
        {
            Allocations.ThrowIfNull(machine);

            USpan<char> source = machine->source.AsSpan();
            if (float.TryParse(source, out float result))
            {
                return result;
            }
            else
            {
                return machine->tree.Evaluate(new(machine));
            }
        }
    }
}