using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using GXTConvert.Exceptions;

namespace GXTConvert.FileFormat
{
    public class SceGxtHeader
    {
        public const string ExpectedMagicNumber = "GXT\0";

        public string MagicNumber { get; private set; }
        public uint Version { get; private set; } // TODO: 0x10000003 == 3.01 ???
        public uint NumTextures { get; private set; }
        public uint TextureDataOffset { get; private set; }
        public uint TextureDataSize { get; private set; }
        public uint NumP4Palettes { get; private set; }
        public uint NumP8Palettes { get; private set; }
        public uint Padding { get; private set; }

        public SceGxtHeader(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);

            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (MagicNumber != ExpectedMagicNumber)
                throw new UnknownMagicException(string.Format("Unexpected '{0}' for GTX", new string(MagicNumber.Where(x => !Char.IsControl(x)).ToArray()).TrimEnd('\0')));

            Version = reader.ReadUInt32();
            NumTextures = reader.ReadUInt32();
            TextureDataOffset = reader.ReadUInt32();
            TextureDataSize = reader.ReadUInt32();
            NumP4Palettes = reader.ReadUInt32();
            NumP8Palettes = reader.ReadUInt32();
            Padding = reader.ReadUInt32();
        }
    }
}
