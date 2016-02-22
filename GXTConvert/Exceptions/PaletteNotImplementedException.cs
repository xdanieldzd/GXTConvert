using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GXTConvert.Exceptions
{
    public class PaletteNotImplementedException : Exception
    {
        public SceGxmTextureBaseFormat Format { get; private set; }

        public PaletteNotImplementedException(SceGxmTextureBaseFormat format) : base() { this.Format = format; }
    }
}
