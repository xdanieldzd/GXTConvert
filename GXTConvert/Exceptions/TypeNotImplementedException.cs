using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GXTConvert.Exceptions
{
    public class TypeNotImplementedException : Exception
    {
        public SceGxmTextureType Type { get; private set; }

        public TypeNotImplementedException(SceGxmTextureType type) : base() { this.Type = type; }
    }
}
