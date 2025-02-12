using System;
using Unmanaged;

namespace ExpressionMachine
{
    /// <summary>
    /// Represents a node in the expression tree.
    /// </summary>
    public unsafe struct Node : IDisposable
    {
        private Implementation* node;

        /// <summary>
        /// Type of the node.
        /// </summary>
        public readonly ref NodeType Type
        {
            get
            {
                Allocations.ThrowIfNull(node);

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
                Allocations.ThrowIfNull(node);

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
                Allocations.ThrowIfNull(node);

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
                Allocations.ThrowIfNull(node);

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
            node = Implementation.Allocate(default, default, default, default);
        }
#endif

        /// <summary>
        /// Initializes an existing node from the given <paramref name="pointer"/>.
        /// </summary>
        public Node(Implementation* pointer)
        {
            this.node = pointer;
        }

        /// <summary>
        /// Creates a new node with the given <paramref name="type"/>.
        /// </summary>
        public Node(NodeType type, nint a, nint b, nint c)
        {
            node = Implementation.Allocate(type, a, b, c);
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
            Allocations.ThrowIfNull(node);

            node->type = default;
            node->a = default;
            node->b = default;
            node->c = default;
        }

        /// <summary>
        /// Implementation type.
        /// </summary>
        public struct Implementation
        {
            internal NodeType type;
            internal nint a;
            internal nint b;
            internal nint c;

            private Implementation(NodeType type, nint a, nint b, nint c)
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
                Allocations.ThrowIfNull(node);

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

                Allocations.Free(ref node);
            }

            /// <summary>
            /// Evaluates the given <paramref name="node"/>.
            /// </summary>
            public static float Evaluate(Implementation* node, Machine vm)
            {
                Allocations.ThrowIfNull(node);

                NodeType type = node->type;
                switch (type)
                {
                    case NodeType.Value:
                        USpan<char> token = vm.GetToken((uint)node->a, (uint)node->b);
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
                        token = vm.GetToken((uint)node->a, (uint)node->b);
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

            /// <summary>
            /// Allocates a new node.
            /// </summary>
            public static Implementation* Allocate(NodeType type, nint a, nint b, nint c)
            {
                ref Implementation node = ref Allocations.Allocate<Implementation>();
                node = new Implementation(type, a, b, c);
                fixed (Implementation* pointer = &node)
                {
                    return pointer;
                }
            }
        }
    }
}