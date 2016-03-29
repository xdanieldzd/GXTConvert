using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

using GXTConvert.Exceptions;
using GXTConvert.Conversion;
using GXTConvert.FileFormat.BUV;

namespace GXTConvert.FileFormat
{
    // http://www.vitadevwiki.com/index.php?title=GXT
    // https://github.com/vitasdk/vita-headers/

    // TODO: is BUV common or only used in SAO?

    public class GxtBinary
    {
        public SceGxtHeader Header { get; private set; }
        public SceGxtTextureInfo[] TextureInfos { get; private set; }

        public BUVChunk BUVChunk { get; private set; }

        public uint[][] P4Palettes { get; private set; }
        public uint[][] P8Palettes { get; private set; }

        public TextureBundle[] TextureBundles { get; private set; }

        public Bitmap[] Textures { get; private set; }
        public Bitmap[] BUVTextures { get; private set; }

        public GxtBinary(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);

            Header = new SceGxtHeader(stream);

            Func<Stream, SceGxtTextureInfo> textureInfoGeneratorFunc;
            switch (Header.Version)
            {
                case 0x10000003: textureInfoGeneratorFunc = new Func<Stream, SceGxtTextureInfo>((s) => { return new SceGxtTextureInfoV301(s); }); break;
                case 0x10000002: textureInfoGeneratorFunc = new Func<Stream, SceGxtTextureInfo>((s) => { return new SceGxtTextureInfoV201(s); }); break;
                case 0x10000001: textureInfoGeneratorFunc = new Func<Stream, SceGxtTextureInfo>((s) => { return new SceGxtTextureInfoV101(s); }); break;
                default: throw new VersionNotImplementedException(Header.Version);
            }

            TextureInfos = new SceGxtTextureInfo[Header.NumTextures];
            for (int i = 0; i < TextureInfos.Length; i++)
                TextureInfos[i] = textureInfoGeneratorFunc(stream);

            // TODO: any other way to detect these?
            if (Encoding.ASCII.GetString(reader.ReadBytes(4)) == BUVChunk.ExpectedMagicNumber)
            {
                stream.Seek(-4, SeekOrigin.Current);
                BUVChunk = new BUVChunk(stream);
            }

            ReadAllBasePalettes(reader);
            ReadAllTextures(reader);

            if (BUVChunk != null)
            {
                // TODO: is it always texture 0?
                TextureBundle bundle = TextureBundles[0];

                BUVTextures = new Bitmap[BUVChunk.Entries.Length];
                for (int i = 0; i < BUVTextures.Length; i++)
                {
                    BUVEntry entry = BUVChunk.Entries[i];
                    using (Bitmap sourceImage = bundle.CreateTexture(FetchPalette(bundle.TextureFormat, entry.PaletteIndex)))
                    {
                        BUVTextures[i] = sourceImage.Clone(new Rectangle(entry.X, entry.Y, entry.Width, entry.Height), sourceImage.PixelFormat);
                    }
                }
            }
        }

        private void ReadAllBasePalettes(BinaryReader reader)
        {
            long paletteOffset = (Header.TextureDataOffset + Header.TextureDataSize) - (((Header.NumP8Palettes * 256) * 4) + ((Header.NumP4Palettes * 16) * 4));
            reader.BaseStream.Seek(paletteOffset, SeekOrigin.Begin);

            P4Palettes = new uint[Header.NumP4Palettes][];
            for (int i = 0; i < P4Palettes.Length; i++) P4Palettes[i] = ReadBasePalette(reader, 16);

            P8Palettes = new uint[Header.NumP8Palettes][];
            for (int i = 0; i < P8Palettes.Length; i++) P8Palettes[i] = ReadBasePalette(reader, 256);
        }

        private void ReadAllTextures(BinaryReader reader)
        {
            TextureBundles = new TextureBundle[TextureInfos.Length];
            Textures = new Bitmap[TextureBundles.Length];

            for (int i = 0; i < TextureInfos.Length; i++)
            {
                TextureBundle bundle = (TextureBundles[i] = new TextureBundle(reader, Header, TextureInfos[i]));
                Textures[i] = bundle.CreateTexture(FetchPalette(bundle.TextureFormat, bundle.PaletteIndex));
            }
        }

        private uint[] ReadBasePalette(BinaryReader reader, int numColor)
        {
            uint[] palette = new uint[numColor];
            for (int i = 0; i < palette.Length; i++) palette[i] = reader.ReadUInt32();
            return palette;
        }

        private Color[] CreatePalette(uint[] inputPalette, Func<byte, byte, byte, byte, Color> arrangerAbgr)
        {
            Color[] outputPalette = new Color[inputPalette.Length];
            for (int i = 0; i < outputPalette.Length; i++)
                outputPalette[i] = arrangerAbgr((byte)(inputPalette[i] >> 24), (byte)(inputPalette[i] >> 0), (byte)(inputPalette[i] >> 8), (byte)(inputPalette[i] >> 16));
            return outputPalette;
        }

        private Color[] FetchPalette(SceGxmTextureFormat textureFormat, int paletteIndex)
        {
            if (paletteIndex == -1 || (paletteIndex >= P4Palettes.Length && paletteIndex >= P8Palettes.Length)) return null;

            Color[] palette;
            switch (textureFormat)
            {
                case SceGxmTextureFormat.P4_ABGR: palette = CreatePalette(P4Palettes[paletteIndex], ((a, b, g, r) => { return Color.FromArgb(a, b, g, r); })); break;
                case SceGxmTextureFormat.P4_ARGB: palette = CreatePalette(P4Palettes[paletteIndex], ((a, b, g, r) => { return Color.FromArgb(a, r, g, b); })); break;
                case SceGxmTextureFormat.P4_RGBA: palette = CreatePalette(P4Palettes[paletteIndex], ((a, b, g, r) => { return Color.FromArgb(r, g, b, a); })); break;
                case SceGxmTextureFormat.P4_BGRA: palette = CreatePalette(P4Palettes[paletteIndex], ((a, b, g, r) => { return Color.FromArgb(b, g, r, a); })); break;
                case SceGxmTextureFormat.P4_1BGR: palette = CreatePalette(P4Palettes[paletteIndex], ((a, b, g, r) => { return Color.FromArgb(0xFF, b, g, r); })); break;
                case SceGxmTextureFormat.P4_1RGB: palette = CreatePalette(P4Palettes[paletteIndex], ((a, b, g, r) => { return Color.FromArgb(0xFF, r, g, b); })); break;
                case SceGxmTextureFormat.P4_RGB1: palette = CreatePalette(P4Palettes[paletteIndex], ((a, b, g, r) => { return Color.FromArgb(r, g, b, 0xFF); })); break;
                case SceGxmTextureFormat.P4_BGR1: palette = CreatePalette(P4Palettes[paletteIndex], ((a, b, g, r) => { return Color.FromArgb(b, g, r, 0xFF); })); break;

                case SceGxmTextureFormat.P8_ABGR: palette = CreatePalette(P8Palettes[paletteIndex], ((a, b, g, r) => { return Color.FromArgb(a, b, g, r); })); break;
                case SceGxmTextureFormat.P8_ARGB: palette = CreatePalette(P8Palettes[paletteIndex], ((a, b, g, r) => { return Color.FromArgb(a, r, g, b); })); break;
                case SceGxmTextureFormat.P8_RGBA: palette = CreatePalette(P8Palettes[paletteIndex], ((a, b, g, r) => { return Color.FromArgb(r, g, b, a); })); break;
                case SceGxmTextureFormat.P8_BGRA: palette = CreatePalette(P8Palettes[paletteIndex], ((a, b, g, r) => { return Color.FromArgb(b, g, r, a); })); break;
                case SceGxmTextureFormat.P8_1BGR: palette = CreatePalette(P8Palettes[paletteIndex], ((a, b, g, r) => { return Color.FromArgb(0xFF, b, g, r); })); break;
                case SceGxmTextureFormat.P8_1RGB: palette = CreatePalette(P8Palettes[paletteIndex], ((a, b, g, r) => { return Color.FromArgb(0xFF, r, g, b); })); break;
                case SceGxmTextureFormat.P8_RGB1: palette = CreatePalette(P8Palettes[paletteIndex], ((a, b, g, r) => { return Color.FromArgb(r, g, b, 0xFF); })); break;
                case SceGxmTextureFormat.P8_BGR1: palette = CreatePalette(P8Palettes[paletteIndex], ((a, b, g, r) => { return Color.FromArgb(b, g, r, 0xFF); })); break;

                default: throw new PaletteNotImplementedException(textureFormat);
            }

            return palette;
        }
    }
}
