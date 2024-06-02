using ExpressionMachine.Unsafe;
using System;

namespace ExpressionMachine
{
    public unsafe struct Node : IDisposable
    {
        private UnsafeNode* value;

        public readonly NodeType Type => UnsafeNode.GetType(value);
        public readonly nint A => UnsafeNode.GetA(value);
        public readonly nint B => UnsafeNode.GetB(value);
        public readonly nint C => UnsafeNode.GetC(value);
        public readonly bool IsDisposed => UnsafeNode.IsDisposed(value);
        public readonly nint Address => (nint)value;

        public Node()
        {
            value = UnsafeNode.Allocate(0, 0);
        }

        public Node(NodeType type, Node left, Node right)
        {
            value = UnsafeNode.Allocate(type, (UnsafeNode*)left.Address, (UnsafeNode*)right.Address);
        }

        public Node(uint start, uint length)
        {
            value = UnsafeNode.Allocate(start, length);
        }

        public Node(uint start, uint length, Node argument)
        {
            value = UnsafeNode.Allocate(start, length, (UnsafeNode*)argument.Address);
        }

        public Node(UnsafeNode* value)
        {
            this.value = value;
        }

        public void Dispose()
        {
            UnsafeNode.Free(ref value);
        }

        public readonly float Evaluate(Machine vm)
        {
            return UnsafeNode.Evaluate(value, vm);
        }
    }
}