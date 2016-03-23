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

Games
=====
Games known to use the GXT format include:
* Danganronpa: Trigger Happy Havoc <sup>(1)</sup>
* Danganronpa 2: Goodbye Despair <sup>(1)</sup>
* Danganronpa Another Episode: Ultra Despair Girls (*.btx) <sup>(1)</sup>
* Digimon Story: Cyber Sleuth (*.pvr)
* Sword Art Online: Hollow Fragment <sup>(2)</sup>
* Senran Kagura: Shinovi Versus (*.gxt)
* Soul Sacrifice Delta

<sup>(1)</sup> Might require [dr_dec decompression script by BlackDragonHunt](https://github.com/BlackDragonHunt/Danganronpa-Tools)  
<sup>(2)</sup> Requires QuickBMS script to unpack OFS3 containers

Acknowledgements
================
* PVRTC texture decompression code ported from [PowerVR Graphics Native SDK](https://github.com/powervr-graphics/Native_SDK), Copyright (c) Imagination Technologies Ltd.
 * For details, see *\GXTConvert\Compression\PVRTC.cs* and *LICENSE.md*
* Texture swizzle logic reverse-engineering and original C implementation by [FireyFly](https://github.com/FireyFly)
* Testing and moral support by [Ehm2k](https://twitter.com/Ehm2k)
