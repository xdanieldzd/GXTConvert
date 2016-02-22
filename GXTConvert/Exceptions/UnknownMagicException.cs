using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GXTConvert.Exceptions
{
    public class UnknownMagicException : Exception
    {
        public UnknownMagicException() : base() { }
        public UnknownMagicException(string message) : base(message) { }
        public UnknownMagicException(string message, Exception innerException) : base(message, innerException) { }
        public UnknownMagicException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
