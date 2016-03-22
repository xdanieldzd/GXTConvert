GXTConvert
==========
__GXTConvert__ is a somewhat rudimentary converter for GXT-format textures used by games for the PlayStation Vita. It is written in C# and based on the .NET Framework.

Requirements
============
* General
 * [.NET Framework 4](http://www.microsoft.com/en-US/download/details.aspx?id=17718)
* Compilation
 * Visual Studio Community 2013 (or higher)
* Usage
 * Compatible files to convert

Usage
=====
Syntax: `GXTConvert.exe <inputs ...> [options]`
* `<inputs ...>`: Any number of files or directories to be converted, separated by spaces
* `[options]`:
 * `--output | -o`: Specify output directory
 * `--keep | -k`: Do not overwrite existing output files

Example: `GXTConvert.exe "C:\Temp\GXT\files\" "C:\Temp\GXT\testfile.gxt" --output "C:\Temp\GXT\output\"`

Acknowledgements
================
* PVRTC texture decompression code ported from [PowerVR Graphics Native SDK](https://github.com/powervr-graphics/Native_SDK), Copyright (c) Imagination Technologies Ltd.
 * For details, see *\GXTConvert\Compression\PVRTC.cs* and *LICENSE.md*
* Texture swizzle logic reverse-engineering and original C implementation by [FireyFly](https://github.com/FireyFly)
* Testing and moral support by [Ehm2k](https://twitter.com/Ehm2k)
