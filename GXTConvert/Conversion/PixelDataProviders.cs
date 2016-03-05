using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing.Imaging;

using GXTConvert.FileFormat;

namespace GXTConvert.Conversion
{
    public static class PixelDataProviders
    {
        public static readonly Dictionary<SceGxmTextureFormat, PixelFormat> PixelFormatMap = new Dictionary<SceGxmTextureFormat, PixelFormat>()
        {
            /* L8       */ { SceGxmTextureFormat.U8_1RRR, PixelFormat.Format32bppArgb },
            /* A8       */ { SceGxmTextureFormat.U8_R000, PixelFormat.Format32bppArgb },
            /* LA88     */ { SceGxmTextureFormat.U8U8_RGGG, PixelFormat.Format32bppArgb },
            /* RG88     */ { SceGxmTextureFormat.U8U8_00GR, PixelFormat.Format32bppArgb },
            /* ARGB1555 */ { SceGxmTextureFormat.U1U5U5U5_ARGB, PixelFormat.Format16bppArgb1555 },
            /* ARGB4444 */ { SceGxmTextureFormat.U4U4U4U4_ARGB, PixelFormat.Format32bppArgb },
            /* RGB565   */ { SceGxmTextureFormat.U5U6U5_RGB, PixelFormat.Format16bppRgb565 },
            // RGBA8888 */ { SceGxmTextureFormat.U8U8U8U8_ABGR, PixelFormat.Undefined },
            /* ARGB8888 */ { SceGxmTextureFormat.U8U8U8U8_ARGB, PixelFormat.Format32bppArgb },
            /* DXT1     */ { SceGxmTextureFormat.UBC1_ABGR, PixelFormat.Format32bppArgb },
            /* DXT3     */ { SceGxmTextureFormat.UBC2_ABGR, PixelFormat.Format32bppArgb },
            /* DXT5     */ { SceGxmTextureFormat.UBC3_ABGR, PixelFormat.Format32bppArgb },
            /* PVRT2    */ { SceGxmTextureFormat.PVRT2BPP_ABGR, PixelFormat.Format32bppArgb },
            // PVRT2    */ { SceGxmTextureFormat.PVRT2BPP_1BGR, PixelFormat.Undefined },
            /* PVRT4    */ { SceGxmTextureFormat.PVRT4BPP_ABGR, PixelFormat.Format32bppArgb },
            // PVRT4    */ { SceGxmTextureFormat.PVRT4BPP_1BGR, PixelFormat.Undefined },
            // PVRTII2  */ { SceGxmTextureFormat.PVRTII2BPP_ABGR, PixelFormat.Undefined },
            // PVRTII2  */ { SceGxmTextureFormat.PVRTII2BPP_1BGR, PixelFormat.Undefined },
            // PVRTII4  */ { SceGxmTextureFormat.PVRTII4BPP_ABGR, PixelFormat.Undefined },
            // PVRTII4  */ { SceGxmTextureFormat.PVRTII4BPP_1BGR, PixelFormat.Undefined },
            /* RGB888   */ { SceGxmTextureFormat.U8U8U8_RGB, PixelFormat.Format24bppRgb },
            /* RGB888X  */ { SceGxmTextureFormat.U8U8U8X8_RGB1, PixelFormat.Format32bppRgb },
            /* P4       */ { SceGxmTextureFormat.P4_ABGR, PixelFormat.Format4bppIndexed },
            /* P8       */ { SceGxmTextureFormat.P8_ABGR, PixelFormat.Format8bppIndexed },
        };

        public delegate byte[] ProviderFunctionDelegate(BinaryReader reader, SceGxtTextureInfo info);

        public static readonly Dictionary<SceGxmTextureFormat, ProviderFunctionDelegate> ProviderFunctions = new Dictionary<SceGxmTextureFormat, ProviderFunctionDelegate>()
        {
            { SceGxmTextureFormat.U8_1RRR, new ProviderFunctionDelegate(PixelProviderU8_1RRR) },
            { SceGxmTextureFormat.U8_R000, new ProviderFunctionDelegate(PixelProviderU8_R000) }, // TODO: verify me!
            { SceGxmTextureFormat.U8U8_RGGG, new ProviderFunctionDelegate(PixelProviderU8U8_RGGG) }, // TODO: verify me!
            { SceGxmTextureFormat.U8U8_00GR, new ProviderFunctionDelegate(PixelProviderU8U8_00GR) }, // TODO: verify me!
            { SceGxmTextureFormat.U1U5U5U5_ARGB, new ProviderFunctionDelegate(PixelProviderDirect) },
            { SceGxmTextureFormat.U4U4U4U4_ARGB, new ProviderFunctionDelegate(PixelProviderU4U4U4U4_ARGB) }, // TODO: verify me!
            { SceGxmTextureFormat.U5U6U5_RGB, new ProviderFunctionDelegate(PixelProviderDirect) }, // TODO: verify me!
            { SceGxmTextureFormat.U8U8U8U8_ARGB, new ProviderFunctionDelegate(PixelProviderDirect) },
            { SceGxmTextureFormat.UBC1_ABGR, new ProviderFunctionDelegate(PixelProviderDXTx) },
            { SceGxmTextureFormat.UBC2_ABGR, new ProviderFunctionDelegate(PixelProviderDXTx) },
            { SceGxmTextureFormat.UBC3_ABGR, new ProviderFunctionDelegate(PixelProviderDXTx) },
            { SceGxmTextureFormat.PVRT2BPP_ABGR, new ProviderFunctionDelegate(PixelProviderPVRTC) },
            //{ SceGxmTextureFormat.PVRT2BPP_1BGR, new ProviderFunctionDelegate(PixelProviderPVRTC) },
            { SceGxmTextureFormat.PVRT4BPP_ABGR, new ProviderFunctionDelegate(PixelProviderPVRTC) },
            //{ SceGxmTextureFormat.PVRT4BPP_1BGR, new ProviderFunctionDelegate(PixelProviderPVRTC) },
            { SceGxmTextureFormat.U8U8U8_RGB, new ProviderFunctionDelegate(PixelProviderDirect) },
            { SceGxmTextureFormat.U8U8U8X8_RGB1, new ProviderFunctionDelegate(PixelProviderDirect) },
            { SceGxmTextureFormat.P4_ABGR, new ProviderFunctionDelegate(PixelProviderDirect) }, // TODO: verify me!
            { SceGxmTextureFormat.P8_ABGR, new ProviderFunctionDelegate(PixelProviderDirect) },
        };

        private static byte[] PixelProviderDirect(BinaryReader reader, SceGxtTextureInfo info)
        {
            return reader.ReadBytes((int)info.DataSize);
        }

        private static byte[] PixelProviderDXTx(BinaryReader reader, SceGxtTextureInfo info)
        {
            return Compression.DXTx.Decompress(reader, info);
        }

        private static byte[] PixelProviderPVRTC(BinaryReader reader, SceGxtTextureInfo info)
        {
            return Compression.PVRTC.Decompress(reader, info);
        }

        private static byte[] PixelProviderU8_1RRR(BinaryReader reader, SceGxtTextureInfo info)
        {
            byte[] pixelData = new byte[info.DataSize * 4];
            for (int j = 0; j < pixelData.Length; j += 4)
            {
                pixelData[j + 0] = pixelData[j + 1] = pixelData[j + 2] = reader.ReadByte();
                pixelData[j + 3] = 0xFF;
            };
            return pixelData;
        }

        private static byte[] PixelProviderU8_R000(BinaryReader reader, SceGxtTextureInfo info)
        {
            byte[] pixelData = new byte[info.DataSize * 4];
            for (int j = 0; j < pixelData.Length; j += 4)
            {
                pixelData[j + 0] = pixelData[j + 1] = pixelData[j + 2] = 0x00;
                pixelData[j + 3] = reader.ReadByte();
            };
            return pixelData;
        }

        private static byte[] PixelProviderU8U8_RGGG(BinaryReader reader, SceGxtTextureInfo info)
        {
            byte[] pixelData = new byte[info.DataSize * 4];
            for (int j = 0; j < pixelData.Length; j += 4)
            {
                pixelData[j + 3] = reader.ReadByte();
                pixelData[j + 0] = pixelData[j + 1] = pixelData[j + 2] = reader.ReadByte();
            };
            return pixelData;
        }

        private static byte[] PixelProviderU8U8_00GR(BinaryReader reader, SceGxtTextureInfo info)
        {
            byte[] pixelData = new byte[info.DataSize * 4];
            for (int j = 0; j < pixelData.Length; j += 4)
            {
                pixelData[j + 1] = reader.ReadByte();
                pixelData[j + 2] = reader.ReadByte();
                pixelData[j + 0] = pixelData[j + 3] = 0x00;
            };
            return pixelData;
        }

        private static byte[] PixelProviderU4U4U4U4_ARGB(BinaryReader reader, SceGxtTextureInfo info)
        {
            byte[] pixelData = new byte[info.DataSize * 4];
            for (int j = 0; j < pixelData.Length; j += 4)
            {
                ushort rgba = reader.ReadUInt16();
                pixelData[j + 0] = (byte)(((rgba >> 4) << 4) & 0xFF);
                pixelData[j + 1] = (byte)(((rgba >> 8) << 4) & 0xFF);
                pixelData[j + 2] = (byte)(((rgba >> 12) << 4) & 0xFF);
                pixelData[j + 3] = (byte)((rgba << 4) & 0xFF);
            };
            return pixelData;
        }
    }
}
