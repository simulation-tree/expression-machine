using System;
using Unmanaged;

namespace ExpressionMachine
{
    public unsafe struct Node : IDisposable
    {
        private Implementation* value;

        public readonly NodeType Type => Implementation.GetType(value);
        public readonly nint A => Implementation.GetA(value);
        public readonly nint B => Implementation.GetB(value);
        public readonly nint C => Implementation.GetC(value);
        public readonly bool IsDisposed => value is null;
        public readonly nint Address => (nint)value;

#if NET
        public Node()
        {
            value = Implementation.Allocate(0, 0);
        }
#endif

        public Node(NodeType type, Node left, Node right)
        {
            value = Implementation.Allocate(type, (Implementation*)left.Address, (Implementation*)right.Address);
        }

        public Node(uint start, uint length)
        {
            value = Implementation.Allocate(start, length);
        }

        public Node(uint start, uint length, Node argument)
        {
            value = Implementation.Allocate(start, length, (Implementation*)argument.Address);
        }

        public Node(Implementation* value)
        {
            this.value = value;
        }

        public void Dispose()
        {
            Implementation.Free(ref value);
        }

        public readonly float Evaluate(Machine vm)
        {
            return Implementation.Evaluate(value, vm);
        }

        public struct Implementation
        {
            private NodeType type;
            private nint a;
            private nint b;
            private nint c;

            private Implementation(NodeType type, nint a, nint b, nint c)
            {
                this.type = type;
                this.a = a;
                this.b = b;
                this.c = c;
            }

            public static NodeType GetType(Implementation* node)
            {
                Allocations.ThrowIfNull(node);

                return node->type;
            }

            public static nint GetA(Implementation* node)
            {
                Allocations.ThrowIfNull(node);

                return node->a;
            }

            public static nint GetB(Implementation* node)
            {
                Allocations.ThrowIfNull(node);

                return node->b;
            }

            public static nint GetC(Implementation* node)
            {
                Allocations.ThrowIfNull(node);

                return node->c;
            }

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

                node->type = NodeType.Unknown;
                node->a = default;
                node->b = default;
                node->c = default;
                Allocations.Free(ref node);
            }

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

            public static Implementation* Allocate(uint start, uint length)
            {
                ref Implementation node = ref Allocations.Allocate<Implementation>();
                node = new Implementation(NodeType.Value, (nint)start, (nint)length, default);
                fixed (Implementation* pointer = &node)
                {
                    return pointer;
                }
            }

            public static Implementation* Allocate(NodeType type, Implementation* left, Implementation* right)
            {
                ref Implementation node = ref Allocations.Allocate<Implementation>();
                node = new Implementation(type, (nint)left, (nint)right, default);
                fixed (Implementation* pointer = &node)
                {
                    return pointer;
                }
            }

            public static Implementation* Allocate(uint start, uint length, Implementation* argument)
            {
                ref Implementation node = ref Allocations.Allocate<Implementation>();
                node = new Implementation(NodeType.Call, (nint)start, (nint)length, (nint)argument);
                fixed (Implementation* pointer = &node)
                {
                    return pointer;
                }
            }
        }
    }
}