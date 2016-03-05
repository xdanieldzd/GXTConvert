using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace GXTConvert.FileFormat
{
    public abstract class SceGxtTextureInfo
    {
        public uint DataOffset { get; private set; }
        public uint DataSize { get; private set; }
        public int PaletteIndex { get; private set; }
        public uint Flags { get; private set; }
        public uint[] ControlWords { get; private set; }

        public abstract SceGxmTextureType GetTextureType();
        public abstract SceGxmTextureFormat GetTextureFormat();
        public abstract ushort GetWidth();
        public abstract ushort GetHeight();

        //TODO: where's byteStride for texture type LinearStrided?

        public SceGxmTextureBaseFormat GetTextureBaseFormat()
        {
            return (SceGxmTextureBaseFormat)((uint)GetTextureFormat() & 0xFFFF0000);
        }

        public SceGxtTextureInfo(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);

            DataOffset = reader.ReadUInt32();
            DataSize = reader.ReadUInt32();
            PaletteIndex = reader.ReadInt32();
            Flags = reader.ReadUInt32();
            ControlWords = new uint[4];
            for (int i = 0; i < ControlWords.Length; i++) ControlWords[i] = reader.ReadUInt32();
        }
    }

    public class SceGxtTextureInfoV301 : SceGxtTextureInfo
    {
        public SceGxtTextureInfoV301(Stream stream) : base(stream) { }

        public override SceGxmTextureType GetTextureType() { return (SceGxmTextureType)ControlWords[0]; }
        public override SceGxmTextureFormat GetTextureFormat() { return (SceGxmTextureFormat)ControlWords[1]; }
        public override ushort GetWidth() { return (ushort)(ControlWords[2] & 0xFFFF); }
        public override ushort GetHeight() { return (ushort)(ControlWords[2] >> 16); }
    }

    // TODO: verify me! width and height seem wrong!
    public class SceGxtTextureInfoV201 : SceGxtTextureInfo
    {
        public SceGxtTextureInfoV201(Stream stream) : base(stream) { }

        public override SceGxmTextureType GetTextureType() { return (SceGxmTextureType)ControlWords[2]; }
        public override SceGxmTextureFormat GetTextureFormat() { return (SceGxmTextureFormat)ControlWords[3]; }
        public override ushort GetWidth() { return (ushort)(ControlWords[0] & 0xFFFF); }
        public override ushort GetHeight() { return (ushort)(ControlWords[1] & 0xFFFF); }
    }
}
