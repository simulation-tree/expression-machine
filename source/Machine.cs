using Collections;
using Collections.Generic;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace ExpressionMachine
{
    /// <summary>
    /// A machine that evaluates expressions.
    /// </summary>
    [SkipLocalsInit]
    public unsafe struct Machine : IDisposable
    {
        private Implementation* machine;

        /// <summary>
        /// The current source code.
        /// </summary>
        public readonly USpan<char> Source
        {
            get
            {
                Allocations.ThrowIfNull(machine);

                return machine->source.AsSpan();
            }
        }

        /// <summary>
        /// Checks if the <see cref="Machine"/> has been disposed.
        /// </summary>
        public readonly bool IsDisposed => machine is null;

#if NET
        /// <summary>
        /// Creates a new machine.
        /// </summary>
        public Machine()
        {
            machine = Implementation.Allocate();
        }
#endif

        /// <summary>
        /// Initializes an existing machine from the given <paramref name="pointer"/>.
        /// </summary>
        public Machine(Implementation* pointer)
        {
            this.machine = pointer;
        }

        /// <summary>
        /// Creates a new machine initialized with the given <paramref name="source"/>.
        /// </summary>
        public Machine(USpan<char> source)
        {
            machine = Implementation.Allocate();
            SetSource(source);
        }

        /// <summary>
        /// Creates a new machine initialized with the given <paramref name="source"/>.
        /// </summary>
        public Machine(FixedString source)
        {
            machine = Implementation.Allocate();
            SetSource(source);
        }

        /// <summary>
        /// Creates a new machine initialized with the given <paramref name="source"/>.
        /// </summary>
        public Machine(string source)
        {
            machine = Implementation.Allocate();
            SetSource(source);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfVariableIsMissing(USpan<char> name)
        {
            if (!ContainsVariable(name))
            {
                throw new InvalidOperationException($"Variable `{name.ToString()}` not found");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfFunctionIsMissing(USpan<char> name)
        {
            if (!ContainsFunction(name))
            {
                throw new InvalidOperationException($"Function `{name.ToString()}` not found");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfFunctionIsMissing(FixedString name)
        {
            if (!ContainsFunction(name))
            {
                throw new InvalidOperationException($"Function `{name.ToString()}` not found");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Allocations.ThrowIfNull(machine);

            Implementation.Free(ref machine);
        }

        /// <summary>
        /// Assigns <paramref name="newSource"/> to the machine.
        /// </summary>
        public readonly void SetSource(USpan<char> newSource)
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

        /// <summary>
        /// Assigns <paramref name="newSource"/> to the machine.
        /// </summary>
        public readonly void SetSource(FixedString newSource)
        {
            USpan<char> nameSpan = stackalloc char[newSource.Length];
            newSource.CopyTo(nameSpan);
            SetSource(nameSpan);
        }

        /// <summary>
        /// Assigns <paramref name="newSource"/> to the machine.
        /// </summary>
        public readonly void SetSource(string newSource)
        {
            SetSource(newSource.AsSpan());
        }

        /// <summary>
        /// Clears all variables.
        /// </summary>
        public readonly void ClearVariables()
        {
            Allocations.ThrowIfNull(machine);

            machine->variableNameHashes.Clear();
            machine->variableValues.Clear();
        }

        /// <summary>
        /// Clears all functions.
        /// </summary>
        public readonly void ClearFunctions()
        {
            Allocations.ThrowIfNull(machine);

            machine->functionNameHashes.Clear();
            machine->functionValues.Clear();
        }

        /// <summary>
        /// Retrieves the value of the variable with the given <paramref name="name"/>.
        /// </summary>
        public readonly float GetVariable(USpan<char> name)
        {
            Allocations.ThrowIfNull(machine);
            ThrowIfVariableIsMissing(name);

            int hash = new FixedString(name).GetHashCode();
            uint index = machine->variableNameHashes.IndexOf(hash);
            return machine->variableValues[index];
        }

        /// <summary>
        /// Retrieves the value of the variable with the given <paramref name="name"/>.
        /// </summary>
        public readonly float GetVariable(FixedString name)
        {
            USpan<char> nameSpan = stackalloc char[name.Length];
            name.CopyTo(nameSpan);
            return GetVariable(nameSpan);
        }

        /// <summary>
        /// Retrieves the value of the variable with the given <paramref name="name"/>.
        /// </summary>
        public readonly float GetVariable(string name)
        {
            return GetVariable(name.AsSpan());
        }

        /// <summary>
        /// Checks if the machine contains a variable with the given <paramref name="name"/>.
        /// </summary>
        public readonly bool ContainsVariable(USpan<char> name)
        {
            Allocations.ThrowIfNull(machine);

            int hash = new FixedString(name).GetHashCode();
            return machine->variableNameHashes.Contains(hash);
        }

        /// <summary>
        /// Checks if the machine contains a variable with the given <paramref name="name"/>.
        /// </summary>
        public readonly bool ContainsVariable(FixedString name)
        {
            USpan<char> nameSpan = stackalloc char[name.Length];
            name.CopyTo(nameSpan);
            return ContainsVariable(nameSpan);
        }

        /// <summary>
        /// Checks if the machine contains a variable with the given <paramref name="name"/>.
        /// </summary>
        public readonly bool ContainsVariable(string name)
        {
            return ContainsVariable(name.AsSpan());
        }

        /// <summary>
        /// Checks if the machine contains a function with the given <paramref name="name"/>.
        /// </summary>
        public readonly bool ContainsFunction(USpan<char> name)
        {
            Allocations.ThrowIfNull(machine);

            int hash = new FixedString(name).GetHashCode();
            return machine->functionNameHashes.Contains(hash);
        }

        /// <summary>
        /// Checks if the machine contains a function with the given <paramref name="name"/>.
        /// </summary>
        public readonly bool ContainsFunction(FixedString name)
        {
            Allocations.ThrowIfNull(machine);

            int hash = name.GetHashCode();
            return machine->functionNameHashes.Contains(hash);
        }

        /// <summary>
        /// Adds or sets <paramref name="value"/> to the variable with the given <paramref name="name"/>.
        /// </summary>
        public readonly void SetVariable(USpan<char> name, float value)
        {
            Allocations.ThrowIfNull(machine);

            int hash = new FixedString(name).GetHashCode();
            if (machine->variableNameHashes.TryIndexOf(hash, out uint index))
            {
                machine->variableValues[index] = value;
            }
            else
            {
                machine->variableNameHashes.Add(hash);
                machine->variableValues.Add(value);
            }
        }

        /// <summary>
        /// Adds or sets <paramref name="value"/> to the variable with the given <paramref name="name"/>.
        /// </summary>
        public readonly void SetVariable(FixedString name, float value)
        {
            USpan<char> nameSpan = stackalloc char[name.Length];
            name.CopyTo(nameSpan);
            SetVariable(nameSpan, value);
        }

        /// <summary>
        /// Adds or sets <paramref name="value"/> to the variable with the given <paramref name="name"/>.
        /// </summary>
        public readonly void SetVariable(string name, float value)
        {
            SetVariable(name.AsSpan(), value);
        }

        /// <summary>
        /// Retrieves the text for the given <paramref name="token"/>.
        /// </summary>
        public readonly USpan<char> GetToken(Token token)
        {
            Allocations.ThrowIfNull(machine);

            return machine->source.AsSpan().Slice(token.start, token.length);
        }

        /// <summary>
        /// Retrieves the text for the token starting at <paramref name="start"/> with the given <paramref name="length"/>.
        /// </summary>
        public readonly USpan<char> GetToken(uint start, uint length)
        {
            Allocations.ThrowIfNull(machine);

            return machine->source.AsSpan().Slice(start, length);
        }

        /// <summary>
        /// Adds or sets the function with the given <paramref name="name"/>.
        /// </summary>
        public readonly void SetFunction(FixedString name, Function function)
        {
            Allocations.ThrowIfNull(machine);

            int hash = name.GetHashCode();
            if (machine->functionNameHashes.TryIndexOf(hash, out uint index))
            {
                machine->functionValues[index] = function;
            }
            else
            {
                machine->functionNameHashes.Add(hash);
                machine->functionValues.Add(function);
            }
        }

        /// <summary>
        /// Adds or sets the function with the given <paramref name="name"/>.
        /// </summary>
        public readonly void SetFunction(USpan<char> name, delegate* unmanaged<float, float> function)
        {
            Allocations.ThrowIfNull(machine);

            Function f = new(function);
            int hash = new FixedString(name).GetHashCode();
            if (machine->functionNameHashes.TryIndexOf(hash, out uint index))
            {
                machine->functionValues[index] = f;
            }
            else
            {
                machine->functionNameHashes.Add(hash);
                machine->functionValues.Add(f);
            }
        }

        /// <summary>
        /// Adds or sets the function with the given <paramref name="name"/>.
        /// </summary>
        public readonly void SetFunction(FixedString name, delegate* unmanaged<float, float> function)
        {
            USpan<char> nameSpan = stackalloc char[name.Length];
            name.CopyTo(nameSpan);
            SetFunction(nameSpan, function);
        }

        /// <summary>
        /// Adds or sets the function with the given <paramref name="name"/>.
        /// </summary>
        public readonly void SetFunction(string name, delegate* unmanaged<float, float> function)
        {
            SetFunction(name.AsSpan(), function);
        }

        /// <summary>
        /// Adds or sets the function with the given <paramref name="name"/>.
        /// </summary>
        public readonly void SetFunction(USpan<char> name, Func<float, float> function)
        {
            Allocations.ThrowIfNull(machine);

            Function f = new(function);
            int hash = new FixedString(name).GetHashCode();
            if (machine->functionNameHashes.TryIndexOf(hash, out uint index))
            {
                machine->functionValues[index] = f;
            }
            else
            {
                machine->functionNameHashes.Add(hash);
                machine->functionValues.Add(f);
            }
        }

        /// <summary>
        /// Adds or sets the function with the given <paramref name="name"/>.
        /// </summary>
        public readonly void SetFunction(FixedString name, Func<float, float> function)
        {
            USpan<char> nameSpan = stackalloc char[name.Length];
            name.CopyTo(nameSpan);
            SetFunction(nameSpan, function);
        }

        /// <summary>
        /// Adds or sets the function with the given <paramref name="name"/>.
        /// </summary>
        public readonly void SetFunction(string name, Func<float, float> function)
        {
            SetFunction(name.AsSpan(), function);
        }

        /// <summary>
        /// Invokes the function with the given <paramref name="name"/> with <paramref name="value"/>
        /// as the input parameter.
        /// </summary>
        public readonly float InvokeFunction(USpan<char> name, float value)
        {
            Allocations.ThrowIfNull(machine);
            ThrowIfFunctionIsMissing(name);

            int hash = new FixedString(name).GetHashCode();
            uint index = machine->functionNameHashes.IndexOf(hash);
            Function function = machine->functionValues[index];
            return function.Invoke(value);
        }

        /// <summary>
        /// Invokes the function with the given <paramref name="name"/> with <paramref name="value"/>
        /// as the input parameter.
        /// </summary>
        public readonly float InvokeFunction(FixedString name, float value)
        {
            Allocations.ThrowIfNull(machine);
            ThrowIfFunctionIsMissing(name);

            int hash = name.GetHashCode();
            uint index = machine->functionNameHashes.IndexOf(hash);
            Function function = machine->functionValues[index];
            return function.Invoke(value);
        }

        /// <summary>
        /// Invokes the function with the given <paramref name="name"/> with <paramref name="value"/>
        /// as the input parameter.
        /// </summary>
        public readonly float InvokeFunction(string name, float value)
        {
            Allocations.ThrowIfNull(machine);
            ThrowIfFunctionIsMissing(name);

            int hash = new FixedString(name).GetHashCode();
            uint index = machine->functionNameHashes.IndexOf(hash);
            Function function = machine->functionValues[index];
            return function.Invoke(value);
        }

        /// <summary>
        /// Evaluates the source code in the machine.
        /// </summary>
        public readonly float Evaluate()
        {
            Allocations.ThrowIfNull(machine);

            USpan<char> source = machine->source.AsSpan();
            if (float.TryParse(source, out float result))
            {
                return result;
            }
            else
            {
                return machine->tree.Evaluate(this);
            }
        }

        /// <summary>
        /// Native implementation type.
        /// </summary>
        public struct Implementation
        {
            internal readonly TokenMap map;
            internal readonly Text source;
            internal readonly List<int> variableNameHashes;
            internal readonly List<float> variableValues;
            internal readonly List<int> functionNameHashes;
            internal readonly List<Function> functionValues;
            internal readonly List<Token> tokens;
            internal Node tree;

            private Implementation(TokenMap map)
            {
                this.map = map;
                source = new();
                variableNameHashes = new();
                variableValues = new();
                functionNameHashes = new();
                functionValues = new();
                tokens = new();
                tree = new();
            }

            /// <summary>
            /// Allocates a new machine.
            /// </summary>
            public static Implementation* Allocate()
            {
                ref Implementation machine = ref Allocations.Allocate<Implementation>();
                machine = new(new TokenMap());
                fixed (Implementation* pointer = &machine)
                {
                    return pointer;
                }
            }

            /// <summary>
            /// Frees the given <paramref name="machine"/>.
            /// </summary>
            public static void Free(ref Implementation* machine)
            {
                Allocations.ThrowIfNull(machine);

                for (uint i = 0; i < machine->functionValues.Count; i++)
                {
                    machine->functionValues[i].Dispose();
                }

                machine->tree.Dispose();
                machine->tokens.Dispose();
                machine->functionValues.Dispose();
                machine->functionNameHashes.Dispose();
                machine->variableValues.Dispose();
                machine->variableNameHashes.Dispose();
                machine->source.Dispose();
                machine->map.Dispose();
                Allocations.Free(ref machine);
            }
        }
    }
}