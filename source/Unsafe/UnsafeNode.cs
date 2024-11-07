using System;
using Unmanaged;

namespace ExpressionMachine.Unsafe
{
    public unsafe struct UnsafeNode
    {
        private NodeType type;
        private nint a;
        private nint b;
        private nint c;

        public static NodeType GetType(UnsafeNode* node)
        {
            Allocations.ThrowIfNull(node);
            return node->type;
        }

        public static nint GetA(UnsafeNode* node)
        {
            Allocations.ThrowIfNull(node);
            return node->a;
        }

        public static nint GetB(UnsafeNode* node)
        {
            Allocations.ThrowIfNull(node);
            return node->b;
        }

        public static nint GetC(UnsafeNode* node)
        {
            Allocations.ThrowIfNull(node);
            return node->c;
        }

        public static bool IsDisposed(UnsafeNode* node)
        {
            return node is null;
        }

        public static void Free(ref UnsafeNode* node)
        {
            Allocations.ThrowIfNull(node);
            NodeType type = node->type;
            if (type == NodeType.Addition || type == NodeType.Subtraction || type == NodeType.Multiplication || type == NodeType.Division)
            {
                UnsafeNode* left = (UnsafeNode*)node->a;
                UnsafeNode* right = (UnsafeNode*)node->b;
                Free(ref left);
                Free(ref right);
            }
            else if (type == NodeType.Call)
            {
                UnsafeNode* argument = (UnsafeNode*)node->c;
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

        public static float Evaluate(UnsafeNode* node, Machine vm)
        {
            Allocations.ThrowIfNull(node);
            NodeType type = node->type;
            switch (type)
            {
                case NodeType.Value:
                    USpan<char> token = vm.GetToken((uint)node->a, (uint)node->b);
                    if (float.TryParse(token.AsSystemSpan(), out float value))
                    {
                        return value;
                    }
                    else
                    {
                        return vm.GetVariable(token);
                    }
                case NodeType.Addition:
                    return Evaluate((UnsafeNode*)node->a, vm) + Evaluate((UnsafeNode*)node->b, vm);
                case NodeType.Subtraction:
                    return Evaluate((UnsafeNode*)node->a, vm) - Evaluate((UnsafeNode*)node->b, vm);
                case NodeType.Multiplication:
                    return Evaluate((UnsafeNode*)node->a, vm) * Evaluate((UnsafeNode*)node->b, vm);
                case NodeType.Division:
                    return Evaluate((UnsafeNode*)node->a, vm) / Evaluate((UnsafeNode*)node->b, vm);
                case NodeType.Call:
                    token = vm.GetToken((uint)node->a, (uint)node->b);
                    UnsafeNode* argument = (UnsafeNode*)node->c;
                    if (IsDisposed(argument))
                    {
                        return vm.InvokeFunction(token, 0);
                    }
                    else
                    {
                        return vm.InvokeFunction(token, Evaluate(argument, vm));
                    }

                default:
                    throw new InvalidOperationException($"Unknown node type: {type}");
            }
        }

        public static UnsafeNode* Allocate(uint start, uint length)
        {
            void* node = Allocations.Allocate(TypeInfo<nint>.size * 4);
            nint* span = (nint*)node;
            span[0] = (nint)NodeType.Value;
            span[1] = (nint)start;
            span[2] = (nint)length;
            span[3] = default;
            return (UnsafeNode*)node;
        }

        public static UnsafeNode* Allocate(NodeType type, UnsafeNode* left, UnsafeNode* right)
        {
            void* node = Allocations.Allocate(TypeInfo<nint>.size * 4);
            nint* span = (nint*)node;
            span[0] = (nint)type;
            span[1] = (nint)left;
            span[2] = (nint)right;
            span[3] = default;
            return (UnsafeNode*)node;
        }

        public static UnsafeNode* Allocate(uint start, uint length, UnsafeNode* argument)
        {
            void* node = Allocations.Allocate(TypeInfo<nint>.size * 4);
            nint* span = (nint*)node;
            span[0] = (nint)NodeType.Call;
            span[1] = (nint)start;
            span[2] = (nint)length;
            span[3] = (nint)argument;
            return (UnsafeNode*)node;
        }
    }
}