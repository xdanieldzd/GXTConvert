using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace GXTConvert.FileFormat.BUV
{
    // TODO: verify me!
    public class BUVEntry
    {
        public ushort X { get; private set; }
        public ushort Y { get; private set; }
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }
        public short PaletteIndex { get; private set; }
        public ushort Unknown0x0A { get; private set; }

        public BUVEntry(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);

            X = reader.ReadUInt16();
            Y = reader.ReadUInt16();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            PaletteIndex = reader.ReadInt16();
            Unknown0x0A = reader.ReadUInt16();
        }
    }
}
