using System;
using Unmanaged;

namespace ExpressionMachine.Unsafe
{
    public unsafe struct UnsafeNode
    {
        private Type type;
        private nint a;
        private nint b;
        private nint c;

        public static Type GetType(UnsafeNode* node)
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
            return Allocations.IsNull(node) || node->type == Type.Unknown;
        }

        public static void Free(ref UnsafeNode* node)
        {
            Allocations.ThrowIfNull(node);
            Type type = node->type;
            if (type == Type.Addition || type == Type.Subtraction || type == Type.Multiplication || type == Type.Division)
            {
                UnsafeNode* left = (UnsafeNode*)node->a;
                UnsafeNode* right = (UnsafeNode*)node->b;
                Free(ref left);
                Free(ref right);
            }
            else if (type == Type.Call)
            {
                UnsafeNode* argument = (UnsafeNode*)node->c;
                if (!Allocations.IsNull(argument))
                {
                    Free(ref argument);
                }
            }

            node->type = Type.Unknown;
            node->a = default;
            node->b = default;
            node->c = default;
            Allocations.Free(ref node);
        }

        public static float Evaluate(UnsafeNode* node, Machine vm)
        {
            Allocations.ThrowIfNull(node);
            Type type = node->type;
            switch (type)
            {
                case Type.Value:
                    ReadOnlySpan<char> token = vm.GetToken((uint)node->a, (uint)node->b);
                    if (float.TryParse(token, out float value))
                    {
                        return value;
                    }
                    else
                    {
                        return vm.GetVariable(token);
                    }
                case Type.Addition:
                    return Evaluate((UnsafeNode*)node->a, vm) + Evaluate((UnsafeNode*)node->b, vm);
                case Type.Subtraction:
                    return Evaluate((UnsafeNode*)node->a, vm) - Evaluate((UnsafeNode*)node->b, vm);
                case Type.Multiplication:
                    return Evaluate((UnsafeNode*)node->a, vm) * Evaluate((UnsafeNode*)node->b, vm);
                case Type.Division:
                    return Evaluate((UnsafeNode*)node->a, vm) / Evaluate((UnsafeNode*)node->b, vm);
                case Type.Call:
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
            void* node = Allocations.Allocate((uint)sizeof(nint) * 4);
            nint* span = (nint*)node;
            span[0] = (nint)Type.Value;
            span[1] = (nint)start;
            span[2] = (nint)length;
            span[3] = default;
            return (UnsafeNode*)node;
        }

        public static UnsafeNode* Allocate(Type type, UnsafeNode* left, UnsafeNode* right)
        {
            void* node = Allocations.Allocate((uint)sizeof(nint) * 4);
            nint* span = (nint*)node;
            span[0] = (nint)type;
            span[1] = (nint)left;
            span[2] = (nint)right;
            span[3] = default;
            return (UnsafeNode*)node;
        }

        public static UnsafeNode* Allocate(uint start, uint length, UnsafeNode* argument)
        {
            void* node = Allocations.Allocate((uint)sizeof(nint) * 4);
            nint* span = (nint*)node;
            span[0] = (nint)Type.Call;
            span[1] = (nint)start;
            span[2] = (nint)length;
            span[3] = (nint)argument;
            return (UnsafeNode*)node;
        }

        public enum Type : byte
        {
            Unknown = 0,
            Value = 1,
            Addition = 2,
            Subtraction = 3,
            Multiplication = 4,
            Division = 5,
            Call = 6,
        }
    }
}