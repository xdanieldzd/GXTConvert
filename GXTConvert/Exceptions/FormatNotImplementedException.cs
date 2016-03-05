using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using GXTConvert.FileFormat;

namespace GXTConvert.Exceptions
{
    public class FormatNotImplementedException : Exception
    {
        public SceGxmTextureFormat Format { get; private set; }

        public FormatNotImplementedException(SceGxmTextureFormat format) : base() { this.Format = format; }
    }
}
