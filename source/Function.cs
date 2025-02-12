using System;
using System.Runtime.InteropServices;

namespace ExpressionMachine
{
    /// <summary>
    /// A function that expects and returns a <see cref="float"/>
    /// </summary>
    public readonly unsafe struct Function : IDisposable
    {
        //todo: implement handling function signatures other than float <- float

        /// <summary>
        /// Flags that describe the function.
        /// </summary>
        public readonly Flags flags;

        private readonly delegate* unmanaged<float, float> function;
        private readonly GCHandle handle;

        /// <summary>
        /// Creates a new unmanaged function.
        /// </summary>
        public Function(delegate* unmanaged<float, float> function)
        {
            this.function = function;
            flags = Flags.None;
        }

        /// <summary>
        /// Creates a new managed function.
        /// </summary>
        public Function(Func<float, float> function)
        {
            this.handle = GCHandle.Alloc(function, GCHandleType.Normal);
            flags = Flags.Managed;
        }

        /// <summary>
        /// Disposes the function.
        /// </summary>
        public readonly void Dispose()
        {
            bool isManaged = (flags & Flags.Managed) == Flags.Managed;
            if (isManaged)
            {
                handle.Free();
            }
        }

        /// <summary>
        /// Invokes the function.
        /// </summary>
        public readonly float Invoke(float value)
        {
            bool isManaged = (flags & Flags.Managed) == Flags.Managed;
            if (isManaged)
            {
                Func<float, float> function = (Func<float, float>)(handle.Target ?? throw new ObjectDisposedException(nameof(Function)));
                return function(value);
            }
            else
            {
                return function(value);
            }
        }

        /// <summary>
        /// Uses to describe a function.
        /// </summary>
        [Flags]
        public enum Flags : byte
        {
            /// <summary>
            /// No flags.
            /// </summary>
            None = 0,

            /// <summary>
            /// Function is a managed delegate.
            /// </summary>
            Managed = 1,
        }
    }
}