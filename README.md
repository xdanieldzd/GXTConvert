GXTConvert
==========
__GXTConvert__ is a somewhat rudimentary converter for GXT-format textures used by games for the PlayStation Vita. It is written in C# and based on the .NET Framework.

Maintenance Note
================
GXTConvert is currently only receiving limited maintenance. Please see the [Scarlet project](https://github.com/xdanieldzd/Scarlet) for a more generic set of libraries and a sample converter application to convert various game or console image formats to PNG, including GXT.

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
* Danganronpa: Trigger Happy Havoc _(*.gxt)_ <sup>(1)</sup>
* Danganronpa 2: Goodbye Despair _(*.gxt)_ <sup>(1)</sup>
* Danganronpa Another Episode: Ultra Despair Girls _(*.btx)_ <sup>(1)</sup>
* Digimon Story: Cyber Sleuth _(*.pvr)_
* Dragon's Crown <sup>(2)</sup>
* Gravity Rush _(*.gxt)_
* IA/VT Colorful _(*.gxt; *.mxt)_ <sup>(3)</sup>
* Muramasa Rebirth <sup>(2)</sup>
* Sword Art Online: Hollow Fragment _(no extension)_ <sup>(4)</sup>
* Senran Kagura: Shinovi Versus _(*.gxt)_
* Soul Sacrifice Delta _(*.gxt)_ <sup>(5)</sup>
* Toro's Friend Network _(*.gxt)_
* Steins;Gate _(*.gxt)_

<sup>(1)</sup> Require [dr_dec decompression script by BlackDragonHunt](https://github.com/BlackDragonHunt/Danganronpa-Tools) for most files; some .btx files are _not_ GXT  
<sup>(2)</sup> Packed in _*.ftx_ containers  
<sup>(3)</sup> Packed in _archive.pk_ container; requires [QuickBMS script by chrrox](http://zenhax.com/viewtopic.php?f=9&t=2183) to unpack  
<sup>(4)</sup> Packed in _OFS3_ containers; requires QuickBMS script to unpack  
<sup>(5)</sup> Packed in containers; requires [QuickBMS script by chrrox](http://zenhax.com/viewtopic.php?f=9&t=2183) to unpack  

Acknowledgements
================
* PVRTC texture decompression code ported from [PowerVR Graphics Native SDK](https://github.com/powervr-graphics/Native_SDK), Copyright (c) Imagination Technologies Ltd.
 * For details, see *\GXTConvert\Compression\PVRTC.cs* and *LICENSE.md*
* Texture swizzle logic reverse-engineering and original C implementation by [FireyFly](https://github.com/FireyFly)
* Testing and moral support by [Ehm2k](https://twitter.com/Ehm2k)
