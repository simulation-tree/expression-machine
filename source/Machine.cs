using Collections;
using System;
using System.Diagnostics;
using Unmanaged;

namespace ExpressionMachine
{
    public unsafe struct Machine : IDisposable
    {
        private Implementation* value;

        public readonly USpan<char> Source => Implementation.GetSource(value);
        public readonly bool IsDisposed => value is null;

#if NET
        public Machine()
        {
            value = Implementation.Allocate();
        }
#endif

        public Machine(Implementation* value)
        {
            this.value = value;
        }

        public Machine(USpan<char> source)
        {
            value = Implementation.Allocate();
            SetSource(source);
        }

        public Machine(FixedString source)
        {
            value = Implementation.Allocate();
            SetSource(source);
        }

        public Machine(string source)
        {
            value = Implementation.Allocate();
            SetSource(source);
        }

        public void Dispose()
        {
            Implementation.Free(ref value);
        }

        public readonly void SetSource(USpan<char> newSource)
        {
            Implementation.SetSource(value, newSource);
        }

        public readonly void SetSource(FixedString newSource)
        {
            USpan<char> buffer = stackalloc char[(int)FixedString.Capacity];
            uint length = newSource.CopyTo(buffer);
            SetSource(buffer.Slice(0, length));
        }

        public readonly void SetSource(string newSource)
        {
            SetSource(newSource.AsSpan());
        }

        public readonly void ClearVariables()
        {
            Implementation.ClearVariables(value);
        }

        public readonly float GetVariable(USpan<char> name)
        {
            return Implementation.GetVariable(value, name);
        }

        public readonly float GetVariable(FixedString name)
        {
            USpan<char> buffer = stackalloc char[(int)FixedString.Capacity];
            uint length = name.CopyTo(buffer);
            return GetVariable(buffer.Slice(0, length));
        }

        public readonly float GetVariable(string name)
        {
            return GetVariable(name.AsSpan());
        }

        public readonly bool ContainsVariable(USpan<char> name)
        {
            return Implementation.ContainsVariable(value, name);
        }

        public readonly bool ContainsVariable(FixedString name)
        {
            USpan<char> buffer = stackalloc char[(int)FixedString.Capacity];
            uint length = name.CopyTo(buffer);
            return ContainsVariable(buffer.Slice(0, length));
        }

        public readonly bool ContainsVariable(string name)
        {
            return ContainsVariable(name.AsSpan());
        }

        public readonly void SetVariable(USpan<char> name, float value)
        {
            Implementation.SetVariable(this.value, name, value);
        }

        public readonly void SetVariable(FixedString name, float value)
        {
            USpan<char> buffer = stackalloc char[(int)FixedString.Capacity];
            uint length = name.CopyTo(buffer);
            SetVariable(buffer.Slice(0, length), value);
        }

        public readonly void SetVariable(string name, float value)
        {
            SetVariable(name.AsSpan(), value);
        }

        public readonly USpan<char> GetToken(Token token)
        {
            return Implementation.GetToken(value, token.start, token.length);
        }

        public readonly USpan<char> GetToken(uint start, uint length)
        {
            return Implementation.GetToken(value, start, length);
        }

        public readonly unsafe void SetFunction(USpan<char> name, delegate* unmanaged<float, float> function)
        {
            Function f = new(function);
            Implementation.SetFunction(value, name, f);
        }

        public readonly unsafe void SetFunction(FixedString name, delegate* unmanaged<float, float> function)
        {
            USpan<char> buffer = stackalloc char[(int)FixedString.Capacity];
            uint length = name.CopyTo(buffer);
            SetFunction(buffer.Slice(0, length), function);
        }

        public readonly unsafe void SetFunction(string name, delegate* unmanaged<float, float> function)
        {
            SetFunction(name.AsSpan(), function);
        }

        public readonly float InvokeFunction(USpan<char> name, float value)
        {
            return Implementation.InvokeFunction(this.value, name, value);
        }

        public readonly float Evaluate()
        {
            return Implementation.Evaluate(value);
        }

        public struct Implementation
        {
            private TokenMap map;
            private Text source;
            private Dictionary<int, float> variables;
            private Dictionary<int, Function> functions;
            private List<Token> tokens;
            private Node tree;

            public static Implementation* Allocate()
            {
                ref Implementation machine = ref Allocations.Allocate<Implementation>();
                machine.map = new();
                machine.source = new();
                machine.variables = new();
                machine.functions = new();
                machine.tokens = new();
                machine.tree = new();
                fixed (Implementation* pointer = &machine)
                {
                    return pointer;
                }
            }

            public static void Free(ref Implementation* machine)
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

            public static void ClearFunctions(Implementation* machine)
            {
                Allocations.ThrowIfNull(machine);

                machine->functions.Clear();
            }

            public static void ClearVariables(Implementation* machine)
            {
                Allocations.ThrowIfNull(machine);

                machine->variables.Clear();
            }

            public static float GetVariable(Implementation* machine, USpan<char> name)
            {
                Allocations.ThrowIfNull(machine);
                ThrowIfVariableIsMissing(machine, name);

                int hash = new FixedString(name).GetHashCode();
                machine->variables.TryGetValue(hash, out float value);
                return value;
            }

            public static bool ContainsVariable(Implementation* machine, USpan<char> name)
            {
                Allocations.ThrowIfNull(machine);

                int hash = new FixedString(name).GetHashCode();
                return machine->variables.ContainsKey(hash);
            }

            public static void SetVariable(Implementation* machine, USpan<char> name, float value)
            {
                Allocations.ThrowIfNull(machine);

                int hash = new FixedString(name).GetHashCode();
                machine->variables.AddOrSet(hash, value);
            }

            public static void SetFunction(Implementation* machine, USpan<char> name, Function function)
            {
                Allocations.ThrowIfNull(machine);

                int hash = new FixedString(name).GetHashCode();
                machine->functions.AddOrSet(hash, function);
            }

            public static float InvokeFunction(Implementation* machine, USpan<char> name, float value)
            {
                Allocations.ThrowIfNull(machine);
                ThrowIfFunctionIsMissing(machine, name);

                int hash = new FixedString(name).GetHashCode();
                machine->functions.TryGetValue(hash, out Function function);
                return function.Invoke(value);
            }

            public static void SetSource(Implementation* machine, USpan<char> newSource)
            {
                Allocations.ThrowIfNull(machine);

                USpan<char> currentSource = machine->source.AsSpan();
                if (!newSource.SequenceEqual(currentSource))
                {
                    machine->source.CopyFrom(newSource);

                    machine->tokens.Clear();
                    Parsing.GetTokens(newSource, machine->map, machine->tokens);

                    machine->tree.Dispose();
                    machine->tree = Parsing.GetTree(machine->tokens.AsSpan());
                }
            }

            public static USpan<char> GetSource(Implementation* machine)
            {
                Allocations.ThrowIfNull(machine);

                return machine->source.AsSpan();
            }

            public static USpan<char> GetToken(Implementation* machine, uint start, uint length)
            {
                Allocations.ThrowIfNull(machine);

                return machine->source.AsSpan().Slice(start, length);
            }

            public static float Evaluate(Implementation* machine)
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

            [Conditional("DEBUG")]
            private static void ThrowIfVariableIsMissing(Implementation* machine, USpan<char> name)
            {
                if (!ContainsVariable(machine, name))
                {
                    throw new InvalidOperationException($"Variable `{name.ToString()}` not found");
                }
            }

            [Conditional("DEBUG")]
            private static void ThrowIfFunctionIsMissing(Implementation* machine, USpan<char> name)
            {
                if (!machine->functions.ContainsKey(new FixedString(name).GetHashCode()))
                {
                    throw new InvalidOperationException($"Function `{name.ToString()}` not found");
                }
            }
        }
    }
}