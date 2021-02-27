using System;

namespace Pokespeare.Common
{
    /// <summary>Object that wraps either a result or an Exception</summary>
    public record Monad<T>
    {
        /// <summary>Create a successful instance</summary>
        /// <param name="result">The wrapped result</param>
        public Monad(T result)
        {
            Result = result;
            Exception = default;
        }

        /// <summary>Create a failed instance</summary>
        /// <param name="exception">The wrapped exception</param>
        public Monad(Exception exception)
        {
            Result = default;
            Exception = exception;
        }

        /// <summary>Wrapped result.</summary>
        public T? Result { get; }

        /// <summary>Wrapped exception</summary>
        public Exception? Exception { get; }
    }
}
