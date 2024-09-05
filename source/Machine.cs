using ExpressionMachine.Unsafe;
using System;
using Unmanaged;

namespace ExpressionMachine
{
    public unsafe struct Machine : IDisposable
    {
        private UnsafeMachine* value;

        public readonly USpan<char> Source => UnsafeMachine.GetSource(value);
        public readonly bool IsDisposed => UnsafeMachine.IsDisposed(value);

        public Machine()
        {
            value = UnsafeMachine.Allocate();
        }

        public Machine(UnsafeMachine* value)
        {
            this.value = value;
        }

        public Machine(USpan<char> source)
        {
            value = UnsafeMachine.Allocate();
            SetSource(source);
        }

        public Machine(FixedString source)
        {
            value = UnsafeMachine.Allocate();
            SetSource(source);
        }

        public Machine(string source)
        {
            value = UnsafeMachine.Allocate();
            SetSource(source);
        }

        public void Dispose()
        {
            UnsafeMachine.Free(ref value);
        }

        public readonly void SetSource(USpan<char> newSource)
        {
            UnsafeMachine.SetSource(value, newSource);
        }

        public readonly void SetSource(FixedString newSource)
        {
            USpan<char> buffer = stackalloc char[(int)FixedString.MaxLength];
            uint length = newSource.CopyTo(buffer);
            SetSource(buffer.Slice(0, length));
        }

        public readonly void SetSource(string newSource)
        {
            SetSource(newSource.AsSpan());
        }

        public readonly void ClearVariables()
        {
            UnsafeMachine.ClearVariables(value);
        }

        public readonly float GetVariable(USpan<char> name)
        {
            return UnsafeMachine.GetVariable(value, name);
        }

        public readonly float GetVariable(FixedString name)
        {
            USpan<char> buffer = stackalloc char[(int)FixedString.MaxLength];
            uint length = name.CopyTo(buffer);
            return GetVariable(buffer.Slice(0, length));
        }

        public readonly float GetVariable(string name)
        {
            return GetVariable(name.AsSpan());
        }

        public readonly bool ContainsVariable(USpan<char> name)
        {
            return UnsafeMachine.ContainsVariable(value, name);
        }

        public readonly bool ContainsVariable(FixedString name)
        {
            USpan<char> buffer = stackalloc char[(int)FixedString.MaxLength];
            uint length = name.CopyTo(buffer);
            return ContainsVariable(buffer.Slice(0, length));
        }

        public readonly bool ContainsVariable(string name)
        {
            return ContainsVariable(name.AsSpan());
        }

        public readonly void SetVariable(USpan<char> name, float value)
        {
            UnsafeMachine.SetVariable(this.value, name, value);
        }

        public readonly void SetVariable(FixedString name, float value)
        {
            USpan<char> buffer = stackalloc char[(int)FixedString.MaxLength];
            uint length = name.CopyTo(buffer);
            SetVariable(buffer.Slice(0, length), value);
        }

        public readonly void SetVariable(string name, float value)
        {
            SetVariable(name.AsSpan(), value);
        }

        public readonly USpan<char> GetToken(Token token)
        {
            return UnsafeMachine.GetToken(value, token.start, token.length);
        }

        public readonly USpan<char> GetToken(uint start, uint length)
        {
            return UnsafeMachine.GetToken(value, start, length);
        }

        public readonly unsafe void SetFunction(USpan<char> name, delegate* unmanaged<float, float> function)
        {
            Function f = new(function);
            UnsafeMachine.SetFunction(value, name, f);
        }

        public readonly unsafe void SetFunction(FixedString name, delegate* unmanaged<float, float> function)
        {
            USpan<char> buffer = stackalloc char[(int)FixedString.MaxLength];
            uint length = name.CopyTo(buffer);
            SetFunction(buffer.Slice(0, length), function);
        }

        public readonly unsafe void SetFunction(string name, delegate* unmanaged<float, float> function)
        {
            SetFunction(name.AsSpan(), function);
        }

        public readonly float InvokeFunction(USpan<char> name, float value)
        {
            return UnsafeMachine.InvokeFunction(this.value, name, value);
        }

        public readonly float Evaluate()
        {
            return UnsafeMachine.Evaluate(value);
        }
    }
}