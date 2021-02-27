using System;
using System.Runtime.Serialization;

namespace Pokespeare.Exceptions
{
    /// <summary>Use for a limited resource has exceeded limits</summary>
    public class LimitExceededException : Exception
    {
        /// <inheritdoc/>
        public LimitExceededException()
        {
        }

        /// <inheritdoc/>
        public LimitExceededException(string? message) : base(message)
        {
        }

        /// <inheritdoc/>
        public LimitExceededException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        /// <inheritdoc/>
        protected LimitExceededException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
