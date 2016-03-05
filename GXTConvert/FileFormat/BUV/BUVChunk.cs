using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using GXTConvert.Exceptions;

namespace GXTConvert.FileFormat.BUV
{
    // TODO: verify me!
    public class BUVChunk
    {
        public const string ExpectedMagicNumber = "BUV\0";

        public string MagicNumber { get; private set; }
        public uint NumEntries { get; private set; }

        public BUVEntry[] Entries { get; private set; }

        public BUVChunk(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);

            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (MagicNumber != ExpectedMagicNumber)
                throw new UnknownMagicException(string.Format("Unexpected '{0}' for BUV", new string(MagicNumber.Where(x => !Char.IsControl(x)).ToArray()).TrimEnd('\0')));

            NumEntries = reader.ReadUInt32();

            Entries = new BUVEntry[NumEntries];
            for (int i = 0; i < Entries.Length; i++) Entries[i] = new BUVEntry(stream);
        }
    }
}
