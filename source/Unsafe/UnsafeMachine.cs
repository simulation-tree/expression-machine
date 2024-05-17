using System;
using System.Collections.Generic;
using Unmanaged;
using Unmanaged.Collections;

namespace ExpressionMachine.Unsafe
{
    public unsafe struct UnsafeMachine
    {
        private TokenMap map;
        private UnmanagedArray<char> source;
        private UnmanagedDictionary<int, float> variables;
        private UnmanagedDictionary<int, Function> functions;
        private UnmanagedList<Token> tokens;
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
            return Allocations.IsNull(machine) || machine->tree.IsDisposed || machine->source.IsDisposed;
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

        public static float GetVariable(UnsafeMachine* machine, ReadOnlySpan<char> name)
        {
            Allocations.ThrowIfNull(machine);
            int hash = Djb2Hash.GetDjb2HashCode(name);
            if (machine->variables.TryGetValue(hash, out float value))
            {
                return value;
            }
            else
            {
                throw new KeyNotFoundException($"Variable '{name.ToString()}' not found.");
            }
        }

        public static bool ContainsVariable(UnsafeMachine* machine, ReadOnlySpan<char> name)
        {
            Allocations.ThrowIfNull(machine);
            int hash = Djb2Hash.GetDjb2HashCode(name);
            return machine->variables.ContainsKey(hash);
        }

        public static void SetVariable(UnsafeMachine* machine, ReadOnlySpan<char> name, float value)
        {
            Allocations.ThrowIfNull(machine);
            int hash = Djb2Hash.GetDjb2HashCode(name);
            machine->variables[hash] = value;
        }

        public static void SetFunction(UnsafeMachine* machine, ReadOnlySpan<char> name, Function function)
        {
            Allocations.ThrowIfNull(machine);
            int hash = Djb2Hash.GetDjb2HashCode(name);
            machine->functions[hash] = function;
        }

        public static float InvokeFunction(UnsafeMachine* machine, ReadOnlySpan<char> name, float value)
        {
            Allocations.ThrowIfNull(machine);
            int hash = Djb2Hash.GetDjb2HashCode(name);
            if (machine->functions.TryGetValue(hash, out Function function))
            {
                return function.Invoke(value);
            }
            else
            {
                throw new KeyNotFoundException($"Function '{name.ToString()}' not found.");
            }
        }

        public static void SetSource(UnsafeMachine* machine, ReadOnlySpan<char> newSource)
        {
            Allocations.ThrowIfNull(machine);
            if (machine->source.Length < newSource.Length)
            {
                machine->source.Resize((uint)newSource.Length);
            }

            machine->source.Clear();
            Span<char> span = machine->source.AsSpan();
            newSource.CopyTo(span);

            machine->tokens.Clear();
            Parsing.GetTokens(newSource, machine->map, machine->tokens);

            machine->tree.Dispose();
            machine->tree = Parsing.GetTree(machine->tokens.AsSpan());
        }

        public static ReadOnlySpan<char> GetSource(UnsafeMachine* machine)
        {
            Allocations.ThrowIfNull(machine);
            return machine->source.AsSpan();
        }

        public static ReadOnlySpan<char> GetToken(UnsafeMachine* machine, uint start, uint length)
        {
            Allocations.ThrowIfNull(machine);
            return machine->source.AsSpan().Slice((int)start, (int)length);
        }

        public static float Evaluate(UnsafeMachine* machine)
        {
            Allocations.ThrowIfNull(machine);
            return machine->tree.Evaluate(new(machine));
        }
    }
}