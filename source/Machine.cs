using ExpressionMachine.Unsafe;
using System;
using Unmanaged;

namespace ExpressionMachine
{
    public unsafe struct Machine : IDisposable
    {
        private UnsafeMachine* value;

        public readonly ReadOnlySpan<char> Source => UnsafeMachine.GetSource(value);
        public readonly bool IsDisposed => UnsafeMachine.IsDisposed(value);

        public Machine()
        {
            value = UnsafeMachine.Allocate();
        }

        public Machine(UnsafeMachine* value)
        {
            this.value = value;
        }

        public Machine(ReadOnlySpan<char> source)
        {
            value = UnsafeMachine.Allocate();
            SetSource(source);
        }

        public Machine(FixedString source)
        {
            value = UnsafeMachine.Allocate();
            SetSource(source);
        }

        public void Dispose()
        {
            UnsafeMachine.Free(ref value);
        }

        public readonly void SetSource(ReadOnlySpan<char> newSource)
        {
            UnsafeMachine.SetSource(value, newSource);
        }

        public readonly void SetSource(FixedString newSource)
        {
            Span<char> buffer = stackalloc char[newSource.Length];
            newSource.ToString(buffer);
            SetSource(buffer);
        }

        public readonly void ClearVariables()
        {
            UnsafeMachine.ClearVariables(value);
        }

        public readonly float GetVariable(ReadOnlySpan<char> name)
        {
            return UnsafeMachine.GetVariable(value, name);
        }

        public readonly bool ContainsVariable(ReadOnlySpan<char> name)
        {
            return UnsafeMachine.ContainsVariable(value, name);
        }

        public readonly void SetVariable(ReadOnlySpan<char> name, float value)
        {
            UnsafeMachine.SetVariable(this.value, name, value);
        }

        public readonly void SetVariable(FixedString name, float value)
        {
            Span<char> buffer = stackalloc char[name.Length];
            name.ToString(buffer);
            SetVariable(buffer, value);
        }

        public readonly ReadOnlySpan<char> GetToken(Token token)
        {
            return UnsafeMachine.GetToken(value, token.start, token.length);
        }

        public readonly ReadOnlySpan<char> GetToken(uint start, uint length)
        {
            return UnsafeMachine.GetToken(value, start, length);
        }

        public readonly unsafe void SetFunction(ReadOnlySpan<char> name, delegate* unmanaged<float, float> function)
        {
            Function f = new(function);
            UnsafeMachine.SetFunction(value, name, f);
        }

        public readonly float InvokeFunction(ReadOnlySpan<char> name, float value)
        {
            return UnsafeMachine.InvokeFunction(this.value, name, value);
        }

        public readonly float Evaluate()
        {
            return UnsafeMachine.Evaluate(value);
        }
    }
}