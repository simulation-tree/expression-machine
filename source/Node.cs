using System;
using Unmanaged;

namespace ExpressionMachine
{
    /// <summary>
    /// Represents a node in the expression tree.
    /// </summary>
    public unsafe struct Node : IDisposable, IEquatable<Node>
    {
        private Implementation* node;

        /// <summary>
        /// Type of the node.
        /// </summary>
        public readonly ref NodeType Type
        {
            get
            {
                MemoryAddress.ThrowIfDefault(node);

                return ref node->type;
            }
        }

        /// <summary>
        /// First value of the node.
        /// </summary>
        public readonly ref nint A
        {
            get
            {
                MemoryAddress.ThrowIfDefault(node);

                return ref node->a;
            }
        }

        /// <summary>
        /// Second value of the node.
        /// </summary>
        public readonly ref nint B
        {
            get
            {
                MemoryAddress.ThrowIfDefault(node);

                return ref node->b;
            }
        }

        /// <summary>
        /// Third value of the node.
        /// </summary>
        public readonly ref nint C
        {
            get
            {
                MemoryAddress.ThrowIfDefault(node);

                return ref node->c;
            }
        }

        /// <summary>
        /// Checks if the node has been disposed.
        /// </summary>
        public readonly bool IsDisposed => node is null;

        /// <summary>
        /// Native address of the node.
        /// </summary>
        public readonly nint Address => (nint)node;

#if NET
        /// <summary>
        /// Creates a new empty node.
        /// </summary>
        public Node()
        {
            node = MemoryAddress.AllocatePointer<Implementation>();
            node[0] = new(default, default, default, default);
        }
#endif

        /// <summary>
        /// Creates a new node with the given <paramref name="type"/>.
        /// </summary>
        public Node(NodeType type, nint a, nint b, nint c)
        {
            node = MemoryAddress.AllocatePointer<Implementation>();
            node[0] = new(type, a, b, c);
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            return Type.ToString();
        }

        /// <summary>
        /// Disposes of the node.
        /// </summary>
        public void Dispose()
        {
            Implementation.Free(ref node);
        }

        /// <summary>
        /// Evaluates the node.
        /// </summary>
        public readonly float Evaluate(Machine vm)
        {
            return Implementation.Evaluate(node, vm);
        }

        /// <summary>
        /// Clears the node.
        /// </summary>
        public readonly void Clear()
        {
            MemoryAddress.ThrowIfDefault(node);

            node->type = default;
            node->a = default;
            node->b = default;
            node->c = default;
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Node node && Equals(node);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Node other)
        {
            return node == other.node;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            unchecked
            {
                return ((nint)node).GetHashCode();
            }
        }

        /// <summary>
        /// Creates an empty node.
        /// </summary>
        public static Node Create()
        {
            return new(default, default, default, default);
        }

        /// <inheritdoc/>
        public static bool operator ==(Node left, Node right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Node left, Node right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Implementation type.
        /// </summary>
        internal struct Implementation
        {
            internal NodeType type;
            internal nint a;
            internal nint b;
            internal nint c;

            public Implementation(NodeType type, nint a, nint b, nint c)
            {
                this.type = type;
                this.a = a;
                this.b = b;
                this.c = c;
            }

            /// <summary>
            /// Frees the given <paramref name="node"/>.
            /// </summary>
            public static void Free(ref Implementation* node)
            {
                MemoryAddress.ThrowIfDefault(node);

                NodeType type = node->type;
                if (type == NodeType.Addition || type == NodeType.Subtraction || type == NodeType.Multiplication || type == NodeType.Division)
                {
                    Implementation* left = (Implementation*)node->a;
                    Implementation* right = (Implementation*)node->b;
                    Free(ref left);
                    Free(ref right);
                }
                else if (type == NodeType.Call)
                {
                    Implementation* argument = (Implementation*)node->c;
                    if (argument != null)
                    {
                        Free(ref argument);
                    }
                }

                MemoryAddress.Free(ref node);
            }

            /// <summary>
            /// Evaluates the given <paramref name="node"/>.
            /// </summary>
            public static float Evaluate(Implementation* node, Machine vm)
            {
                MemoryAddress.ThrowIfDefault(node);

                NodeType type = node->type;
                switch (type)
                {
                    case NodeType.Value:
                        ReadOnlySpan<char> token = vm.GetToken((int)node->a, (int)node->b);
                        if (float.TryParse(token, out float value))
                        {
                            return value;
                        }
                        else
                        {
                            return vm.GetVariable(token);
                        }
                    case NodeType.Addition:
                        return Evaluate((Implementation*)node->a, vm) + Evaluate((Implementation*)node->b, vm);
                    case NodeType.Subtraction:
                        return Evaluate((Implementation*)node->a, vm) - Evaluate((Implementation*)node->b, vm);
                    case NodeType.Multiplication:
                        return Evaluate((Implementation*)node->a, vm) * Evaluate((Implementation*)node->b, vm);
                    case NodeType.Division:
                        return Evaluate((Implementation*)node->a, vm) / Evaluate((Implementation*)node->b, vm);
                    case NodeType.Call:
                        token = vm.GetToken((int)node->a, (int)node->b);
                        Implementation* argument = (Implementation*)node->c;
                        if (argument is null)
                        {
                            //todo: implement handling of more than 1 arguments
                            return vm.InvokeFunction(token, 0);
                        }
                        else
                        {
                            return vm.InvokeFunction(token, Evaluate(argument, vm));
                        }

                    default:
                        throw new InvalidOperationException($"Unknown node type `{type}`");
                }
            }
        }
    }
}