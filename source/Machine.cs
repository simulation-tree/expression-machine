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
        public readonly ReadOnlySpan<char> Source
        {
            get
            {
                MemoryAddress.ThrowIfDefault(machine);

                return machine->source.GetSpan<char>(machine->sourceLength);
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
            machine = MemoryAddress.AllocatePointer<Implementation>();
            machine[0] = new(TokenMap.Create());
        }
#endif

        /// <summary>
        /// Creates a new machine initialized with the given <paramref name="source"/>.
        /// </summary>
        public Machine(ReadOnlySpan<char> source)
        {
            machine = MemoryAddress.AllocatePointer<Implementation>();
            machine[0] = new(TokenMap.Create(), source);
        }

        /// <summary>
        /// Creates a new machine initialized with the given <paramref name="source"/>.
        /// </summary>
        public Machine(ASCIIText256 source)
        {
            machine = MemoryAddress.AllocatePointer<Implementation>();
            Span<char> buffer = stackalloc char[source.Length];
            source.CopyTo(buffer);
            machine[0] = new(TokenMap.Create(), buffer);
        }

        /// <summary>
        /// Creates a new machine initialized with the given <paramref name="source"/>.
        /// </summary>
        public Machine(string source)
        {
            machine = MemoryAddress.AllocatePointer<Implementation>();
            machine[0] = new(TokenMap.Create(), source);
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfVariableIsMissing(ReadOnlySpan<char> name)
        {
            if (!ContainsVariable(name))
            {
                throw new InvalidOperationException($"Variable `{name.ToString()}` not found");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfFunctionIsMissing(ReadOnlySpan<char> name)
        {
            if (!ContainsFunction(name))
            {
                throw new InvalidOperationException($"Function `{name.ToString()}` not found");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfFunctionIsMissing(ASCIIText256 name)
        {
            if (!ContainsFunction(name))
            {
                throw new InvalidOperationException($"Function `{name.ToString()}` not found");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTokenIsOutOfRange(int start, int length)
        {
            if (start < 0)
            {
                throw new InvalidOperationException($"Start index `{start}` is out of bounds");
            }

            if (length < 0)
            {
                throw new InvalidOperationException($"Length `{length}` is out of bounds");
            }

            if (start + length > machine->sourceLength)
            {
                throw new InvalidOperationException($"Token starting at `{start}` with length `{length}` is out of bounds");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(machine);

            Implementation.Free(ref machine);
        }

        /// <summary>
        /// Assigns <paramref name="newSource"/> to the machine.
        /// </summary>
        public readonly void SetSource(ReadOnlySpan<char> newSource)
        {
            MemoryAddress.ThrowIfDefault(machine);

            ReadOnlySpan<char> currentSource = machine->source.GetSpan<char>(machine->sourceLength);
            if (!newSource.SequenceEqual(currentSource))
            {
                machine->sourceLength = newSource.Length;
                if (machine->sourceCapacity < machine->sourceLength)
                {
                    machine->sourceCapacity = machine->sourceLength.GetNextPowerOf2();
                    MemoryAddress.Resize(ref machine->source, machine->sourceCapacity * sizeof(char));
                }

                machine->source.CopyFrom(newSource);
                machine->tokens.Clear();
                Parsing.GetTokens(newSource, machine->map, machine->tokens);

                //todo: efficiency: instead of disposing and creating a new instance, reuse it
                machine->tree.Dispose();
                machine->tree = Parsing.GetTree(machine->tokens.AsSpan());
            }
        }

        /// <summary>
        /// Assigns <paramref name="newSource"/> to the machine.
        /// </summary>
        public readonly void SetSource(ASCIIText256 newSource)
        {
            Span<char> nameSpan = stackalloc char[newSource.Length];
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
            MemoryAddress.ThrowIfDefault(machine);

            machine->variableNameHashes.Clear();
            machine->variableValues.Clear();
        }

        /// <summary>
        /// Clears all functions.
        /// </summary>
        public readonly void ClearFunctions()
        {
            MemoryAddress.ThrowIfDefault(machine);

            machine->functionNameHashes.Clear();
            machine->functionValues.Clear();
        }

        /// <summary>
        /// Retrieves the value of the variable with the given <paramref name="name"/>.
        /// </summary>
        public readonly float GetVariable(ReadOnlySpan<char> name)
        {
            MemoryAddress.ThrowIfDefault(machine);
            ThrowIfVariableIsMissing(name);

            long hash = name.GetLongHashCode();
            int index = machine->variableNameHashes.IndexOf(hash);
            return machine->variableValues[index];
        }

        /// <summary>
        /// Retrieves the value of the variable with the given <paramref name="name"/>.
        /// </summary>
        public readonly float GetVariable(ASCIIText256 name)
        {
            Span<char> nameSpan = stackalloc char[name.Length];
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
        public readonly bool ContainsVariable(ReadOnlySpan<char> name)
        {
            MemoryAddress.ThrowIfDefault(machine);

            long hash = name.GetLongHashCode();
            return machine->variableNameHashes.Contains(hash);
        }

        /// <summary>
        /// Checks if the machine contains a variable with the given <paramref name="name"/>.
        /// </summary>
        public readonly bool ContainsVariable(ASCIIText256 name)
        {
            Span<char> nameSpan = stackalloc char[name.Length];
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
        public readonly bool ContainsFunction(ReadOnlySpan<char> name)
        {
            MemoryAddress.ThrowIfDefault(machine);

            long hash = name.GetLongHashCode();
            return machine->functionNameHashes.Contains(hash);
        }

        /// <summary>
        /// Checks if the machine contains a function with the given <paramref name="name"/>.
        /// </summary>
        public readonly bool ContainsFunction(ASCIIText256 name)
        {
            MemoryAddress.ThrowIfDefault(machine);

            long hash = name.GetLongHashCode();
            return machine->functionNameHashes.Contains(hash);
        }

        /// <summary>
        /// Adds or sets <paramref name="value"/> to the variable with the given <paramref name="name"/>.
        /// </summary>
        public readonly void SetVariable(ReadOnlySpan<char> name, float value)
        {
            MemoryAddress.ThrowIfDefault(machine);

            long hash = name.GetLongHashCode();
            if (machine->variableNameHashes.TryIndexOf(hash, out int index))
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
        public readonly void SetVariable(ASCIIText256 name, float value)
        {
            Span<char> nameSpan = stackalloc char[name.Length];
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
        public readonly ReadOnlySpan<char> GetToken(Token token)
        {
            MemoryAddress.ThrowIfDefault(machine);
            ThrowIfTokenIsOutOfRange(token.start, token.length);

            return machine->source.AsSpan<char>(token.start, token.length);
        }

        /// <summary>
        /// Retrieves the text for the token starting at <paramref name="start"/> with the given <paramref name="length"/>.
        /// </summary>
        public readonly ReadOnlySpan<char> GetToken(int start, int length)
        {
            MemoryAddress.ThrowIfDefault(machine);
            ThrowIfTokenIsOutOfRange(start, length);

            return machine->source.AsSpan<char>(start, length);
        }

        /// <summary>
        /// Adds or sets the function with the given <paramref name="name"/>.
        /// </summary>
        public readonly void SetFunction(ASCIIText256 name, Function function)
        {
            MemoryAddress.ThrowIfDefault(machine);

            long hash = name.GetLongHashCode();
            if (machine->functionNameHashes.TryIndexOf(hash, out int index))
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
        public readonly void SetFunction(ReadOnlySpan<char> name, delegate* unmanaged<float, float> function)
        {
            MemoryAddress.ThrowIfDefault(machine);

            Function f = new(function);
            long hash = name.GetLongHashCode();
            if (machine->functionNameHashes.TryIndexOf(hash, out int index))
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
        public readonly void SetFunction(ASCIIText256 name, delegate* unmanaged<float, float> function)
        {
            Span<char> nameSpan = stackalloc char[name.Length];
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
        public readonly void SetFunction(ReadOnlySpan<char> name, Func<float, float> function)
        {
            MemoryAddress.ThrowIfDefault(machine);

            Function f = new(function);
            long hash = name.GetLongHashCode();
            if (machine->functionNameHashes.TryIndexOf(hash, out int index))
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
        public readonly void SetFunction(ASCIIText256 name, Func<float, float> function)
        {
            Span<char> nameSpan = stackalloc char[name.Length];
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
        public readonly float InvokeFunction(ReadOnlySpan<char> name, float value)
        {
            MemoryAddress.ThrowIfDefault(machine);
            ThrowIfFunctionIsMissing(name);

            long hash = name.GetLongHashCode();
            int index = machine->functionNameHashes.IndexOf(hash);
            Function function = machine->functionValues[index];
            return function.Invoke(value);
        }

        /// <summary>
        /// Invokes the function with the given <paramref name="name"/> with <paramref name="value"/>
        /// as the input parameter.
        /// </summary>
        public readonly float InvokeFunction(ASCIIText256 name, float value)
        {
            MemoryAddress.ThrowIfDefault(machine);
            ThrowIfFunctionIsMissing(name);

            long hash = name.GetLongHashCode();
            int index = machine->functionNameHashes.IndexOf(hash);
            Function function = machine->functionValues[index];
            return function.Invoke(value);
        }

        /// <summary>
        /// Invokes the function with the given <paramref name="name"/> with <paramref name="value"/>
        /// as the input parameter.
        /// </summary>
        public readonly float InvokeFunction(string name, float value)
        {
            MemoryAddress.ThrowIfDefault(machine);
            ThrowIfFunctionIsMissing(name);

            long hash = name.GetLongHashCode();
            int index = machine->functionNameHashes.IndexOf(hash);
            Function function = machine->functionValues[index];
            return function.Invoke(value);
        }

        /// <summary>
        /// Evaluates the source code in the machine.
        /// </summary>
        public readonly float Evaluate()
        {
            MemoryAddress.ThrowIfDefault(machine);

            ReadOnlySpan<char> source = machine->source.GetSpan<char>(machine->sourceLength);
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
        internal struct Implementation
        {
            public Node tree;
            public MemoryAddress source;
            public int sourceLength;
            public int sourceCapacity;
            public readonly TokenMap map;
            public readonly List<long> variableNameHashes;
            public readonly List<float> variableValues;
            public readonly List<long> functionNameHashes;
            public readonly List<Function> functionValues;
            public readonly List<Token> tokens;

            public Implementation(TokenMap map)
            {
                this.map = map;
                sourceCapacity = 4;
                sourceLength = 0;
                source = MemoryAddress.Allocate(sizeof(char) * sourceCapacity);
                variableNameHashes = new(4);
                variableValues = new(4);
                functionNameHashes = new(4);
                functionValues = new(4);
                tokens = new(32);
                tree = Node.Create();
            }

            public Implementation(TokenMap map, ReadOnlySpan<char> source)
            {
                this.map = map;
                sourceLength = source.Length;
                sourceCapacity = Math.Max(4, sourceLength.GetNextPowerOf2());
                this.source = MemoryAddress.Allocate(sizeof(char) * sourceCapacity);
                this.source.CopyFrom(source);
                variableNameHashes = new(4);
                variableValues = new(4);
                functionNameHashes = new(4);
                functionValues = new(4);
                tokens = Parsing.GetTokens(source, map);
                tree = Parsing.GetTree(tokens.AsSpan());
            }

            /// <summary>
            /// Frees the given <paramref name="machine"/>.
            /// </summary>
            public static void Free(ref Implementation* machine)
            {
                MemoryAddress.ThrowIfDefault(machine);

                for (int i = 0; i < machine->functionValues.Count; i++)
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
                MemoryAddress.Free(ref machine);
            }
        }
    }
}