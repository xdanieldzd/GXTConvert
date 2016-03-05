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

        public Color[][] P4Palettes { get; private set; }
        public Color[][] P8Palettes { get; private set; }

        public TextureBundle[] TextureBundles { get; private set; }

        public Bitmap[] Textures { get; private set; }
        public Bitmap[] BUVTextures { get; private set; }

        public GxtBinary(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);

            Header = new SceGxtHeader(stream);

            TextureInfos = new SceGxtTextureInfo[Header.NumTextures];
            for (int i = 0; i < TextureInfos.Length; i++)
            {
                switch (Header.Version)
                {
                    case 0x10000003: TextureInfos[i] = new SceGxtTextureInfoV301(stream); break;
                    case 0x10000002: TextureInfos[i] = new SceGxtTextureInfoV201(stream); break;
                    default: throw new VersionNotImplementedException(Header.Version);
                }
            }

            // TODO: any other way to detect these?
            if (Encoding.ASCII.GetString(reader.ReadBytes(4)) == BUVChunk.ExpectedMagicNumber)
            {
                stream.Seek(-4, SeekOrigin.Current);
                BUVChunk = new BUVChunk(stream);
            }

            ReadAllPalettes(reader);
            ReadAllTextures(reader);

            if (BUVChunk != null)
            {
                // TODO: is it always texture 0?
                TextureBundle bundle = TextureBundles[0];

                BUVTextures = new Bitmap[BUVChunk.Entries.Length];
                for (int i = 0; i < BUVTextures.Length; i++)
                {
                    BUVEntry entry = BUVChunk.Entries[i];
                    using (Bitmap sourceImage = bundle.CreateTexture(FetchPalette(bundle.TextureBaseFormat, entry.PaletteIndex)))
                    {
                        BUVTextures[i] = sourceImage.Clone(new Rectangle(entry.X, entry.Y, entry.Width, entry.Height), sourceImage.PixelFormat);
                    }
                }
            }
        }

        private void ReadAllPalettes(BinaryReader reader)
        {
            long paletteOffset = reader.BaseStream.Length - (((Header.NumP8Palettes * 256) * 4) + ((Header.NumP4Palettes * 16) * 4));
            reader.BaseStream.Seek(paletteOffset, SeekOrigin.Begin);

            P4Palettes = new Color[Header.NumP4Palettes][];
            for (int i = 0; i < P4Palettes.Length; i++) P4Palettes[i] = ReadPalette(reader, 16);

            P8Palettes = new Color[Header.NumP8Palettes][];
            for (int i = 0; i < P8Palettes.Length; i++) P8Palettes[i] = ReadPalette(reader, 256);
        }

        private void ReadAllTextures(BinaryReader reader)
        {
            TextureBundles = new TextureBundle[TextureInfos.Length];
            Textures = new Bitmap[TextureBundles.Length];

            for (int i = 0; i < TextureInfos.Length; i++)
            {
                TextureBundle bundle = (TextureBundles[i] = new TextureBundle(reader, Header, TextureInfos[i]));
                Textures[i] = bundle.CreateTexture(FetchPalette(bundle.TextureBaseFormat, bundle.PaletteIndex));
            }
        }

        private Color[] ReadPalette(BinaryReader reader, int numColor)
        {
            Color[] palette = new Color[numColor];
            byte r, g, b, a;
            for (int i = 0; i < palette.Length; i++)
            {
                r = reader.ReadByte(); g = reader.ReadByte(); b = reader.ReadByte(); a = reader.ReadByte();
                palette[i] = Color.FromArgb(a, r, g, b);
            }
            return palette;
        }

        private Color[] FetchPalette(SceGxmTextureBaseFormat textureBaseFormat, int paletteIndex)
        {
            if (paletteIndex == -1) return null;

            Color[] palette;
            switch (textureBaseFormat)
            {
                case SceGxmTextureBaseFormat.P4: palette = P4Palettes[paletteIndex]; break;
                case SceGxmTextureBaseFormat.P8: palette = P8Palettes[paletteIndex]; break;
                default: throw new PaletteNotImplementedException(textureBaseFormat);
            }
            return palette;
        }
    }
}
