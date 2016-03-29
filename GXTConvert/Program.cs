using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

using GXTConvert.Exceptions;
using GXTConvert.FileFormat;
using GXTConvert.FileFormat.BUV;

namespace GXTConvert
{
    // Heavily based on NisAnim (DXTx) & UntoldUnpack

    class Program
    {
        static char[] directorySeparators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        static string defaultOutputDir = "(converted)";

        static int indent = 0, baseIndent = 0;
        static bool keepFiles = false;
        static DirectoryInfo globalOutputDir = null;

        static void Main(string[] args)
        {
            //old
            // "E:\[SSD User Data]\Downloads\GXT\GXT" "E:\[SSD User Data]\Downloads\GXT\__output__\ALL" -k
            // "E:\[SSD User Data]\Downloads\GXT\__test__\" "E:\[SSD User Data]\Downloads\GXT\__output__\__test__\" -k

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                var name = (assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false).FirstOrDefault() as AssemblyProductAttribute).Product;
                var version = new Version((assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false).FirstOrDefault() as AssemblyFileVersionAttribute).Version);
                var description = (assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false).FirstOrDefault() as AssemblyDescriptionAttribute).Description;
                var copyright = (assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false).FirstOrDefault() as AssemblyCopyrightAttribute).Copyright;

                IndentWriteLine("------------------------------------------------------------------");
                IndentWriteLine("{0} v{1}.{2} - {3}", name, version.Major, version.Minor, description);
                IndentWriteLine("{0}", copyright);
                IndentWriteLine("------------------------------------------------------------------");
                IndentWriteLine("Original PVRTC decompression code by Imagination Technologies Ltd.");
                IndentWriteLine("Many thanks and greeting to @FireyFly and @Ehm2k!");
                IndentWriteLine("------------------------------------------------------------------");
                IndentWriteLine();

                // "E:\[SSD User Data]\Downloads\GXTSamples(1)\GXTSamples\" "E:\[SSD User Data]\Downloads\GXTSamples(1)\GXTSamples\P4_ARGB\bin_tokuten_l_0000000F.gxt" bad-file-test "E:\[SSD User Data]\Downloads\GXTSamples(1)\GXTSamples\P4_ARGB\bin_tokuten_l_00000010.gxt" --badarg --output "E:\[SSD User Data]\Downloads\GXTSamples(1)\__output__"

                args = CommandLineTools.CreateArgs(Environment.CommandLine);

                if (args.Length < 2)
                    throw new CommandLineArgsException("<input ...> [--keep | --output <directory>]");

                List<DirectoryInfo> inputDirs = new List<DirectoryInfo>();
                List<FileInfo> inputFiles = new List<FileInfo>();

                for (int i = 1; i < args.Length; i++)
                {
                    DirectoryInfo directory = new DirectoryInfo(args[i]);
                    if (directory.Exists)
                    {
                        IEnumerable<FileInfo> files = directory.EnumerateFiles("*", SearchOption.AllDirectories).Where(x => x.Extension != ".png");
                        IndentWriteLine("Adding directory '{0}', {1} file(s) found...", directory.Name, files.Count());
                        inputDirs.Add(directory);
                        continue;
                    }

                    FileInfo file = new FileInfo(args[i]);
                    if (file.Exists)
                    {
                        IndentWriteLine("Adding file '{0}'...", file.Name);
                        inputFiles.Add(file);
                        continue;
                    }

                    if (args[i].StartsWith("-"))
                    {
                        switch (args[i].TrimStart('-'))
                        {
                            case "k":
                            case "keep":
                                keepFiles = true;
                                break;

                            case "o":
                            case "output":
                                globalOutputDir = new DirectoryInfo(args[++i]);
                                break;

                            default:
                                IndentWriteLine("Unknown argument '{0}'.", args[i]);
                                break;
                        }
                        continue;
                    }

                    IndentWriteLine("File or directory '{0}' not found.", args[i]);
                }

                if (inputDirs.Count > 0)
                {
                    foreach (DirectoryInfo inputDir in inputDirs)
                    {
                        IndentWriteLine();
                        IndentWriteLine("Parsing directory '{0}'...", inputDir.Name);
                        baseIndent = indent++;

                        DirectoryInfo outputDir = (globalOutputDir != null ? globalOutputDir : new DirectoryInfo(inputDir.FullName + " " + defaultOutputDir));
                        foreach (FileInfo inputFile in inputDir.EnumerateFiles("*", SearchOption.AllDirectories).Where(x => x.Extension != ".png" && !IsSubdirectory(x.Directory, outputDir)))
                            ProcessInputFile(inputFile, inputDir, outputDir);

                        indent--;
                    }
                }

                if (inputFiles.Count > 0)
                {
                    IndentWriteLine();
                    IndentWriteLine("Parsing files...");
                    baseIndent = indent++;

                    foreach (FileInfo inputFile in inputFiles)
                    {
                        DirectoryInfo outputDir = (globalOutputDir != null ? globalOutputDir : inputFile.Directory);
                        ProcessInputFile(inputFile, inputFile.Directory, outputDir);
                    }
                }
            }
#if !DEBUG
            catch (CommandLineArgsException claEx)
            {
                IndentWriteLine("Invalid arguments; expected: {0}.", claEx.ExpectedArgs);
            }
            catch (Exception ex)
            {
                IndentWriteLine("Exception occured: {0}.", ex.Message);
            }
#endif
            finally
            {
                stopwatch.Stop();

                indent = baseIndent = 0;

                IndentWriteLine();
                IndentWriteLine("Operation completed in {0}.", GetReadableTimespan(stopwatch.Elapsed));
                IndentWriteLine();
                IndentWriteLine("Press any key to exit.");
                Console.ReadKey();
            }
        }

        private static void ProcessInputFile(FileInfo inputFile, DirectoryInfo inputDir, DirectoryInfo outputDir)
        {
            try
            {
                if (!outputDir.Exists) Directory.CreateDirectory(outputDir.FullName);

                string displayPath = inputFile.FullName.Replace(inputDir.FullName, string.Empty).TrimStart(directorySeparators);
                IndentWriteLine("File '{0}'... ", displayPath);
                baseIndent = indent++;

                string relativeDirectory = inputFile.DirectoryName.TrimEnd(directorySeparators).Replace(inputDir.FullName.TrimEnd(directorySeparators), string.Empty).TrimStart(directorySeparators);

                if (keepFiles)
                {
                    string existenceCheckPath = Path.Combine(outputDir.FullName, relativeDirectory);
                    string existenceCheckPattern = Path.GetFileNameWithoutExtension(inputFile.Name) + "*";
                    if (Directory.Exists(existenceCheckPath) && Directory.EnumerateFiles(existenceCheckPath, existenceCheckPattern).Any())
                    {
                        IndentWriteLine("Already exists.");
                        return;
                    }
                }

                using (FileStream fileStream = new FileStream(inputFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    GxtBinary gxtInstance = new GxtBinary(fileStream);

                    for (int i = 0; i < gxtInstance.TextureInfos.Length; i++)
                    {
                        string outputFilename = string.Format("{0} (Texture {1}).png", Path.GetFileNameWithoutExtension(inputFile.Name), i);
                        FileInfo outputFile = new FileInfo(Path.Combine(outputDir.FullName, relativeDirectory, outputFilename));

                        SceGxtTextureInfo info = gxtInstance.TextureInfos[i];

                        IndentWriteLine("Texture #{0}: {1}x{2} ({3}, {4})", (i + 1), info.GetWidth(), info.GetHeight(), info.GetTextureFormat(), info.GetTextureType());
                        indent++;

                        if (!outputFile.Directory.Exists) Directory.CreateDirectory(outputFile.Directory.FullName);
                        gxtInstance.Textures[i].Save(outputFile.FullName, System.Drawing.Imaging.ImageFormat.Png);

                        indent--;
                    }

                    if (gxtInstance.BUVChunk != null)
                    {
                        indent++;
                        for (int i = 0; i < gxtInstance.BUVTextures.Length; i++)
                        {
                            string outputFilename = string.Format("{0} (Block {1}).png", Path.GetFileNameWithoutExtension(inputFile.Name), i);
                            FileInfo outputFile = new FileInfo(Path.Combine(outputDir.FullName, relativeDirectory, outputFilename));

                            BUVEntry entry = gxtInstance.BUVChunk.Entries[i];

                            IndentWriteLine("Block #{0}: {1}x{2} (Origin X:{3}, Y:{4})", (i + 1), entry.Width, entry.Height, entry.X, entry.Y);
                            indent++;

                            if (!outputFile.Directory.Exists) Directory.CreateDirectory(outputFile.Directory.FullName);
                            gxtInstance.BUVTextures[i].Save(outputFile.FullName, System.Drawing.Imaging.ImageFormat.Png);

                            indent--;
                        }
                        indent--;
                    }
                }
            }
#if !DEBUG
            catch (VersionNotImplementedException vniEx)
            {
                IndentWriteLine("GXT version {0:D2} (0x{1:X8}) not implemented.", (vniEx.Version & 0xFFFF), vniEx.Version);
            }
            catch (FormatNotImplementedException fniEx)
            {
                IndentWriteLine("Format '{0}' not implemented.", fniEx.Format);
            }
            catch (PaletteNotImplementedException pniEx)
            {
                IndentWriteLine("Palette '{0}' not implemented.", pniEx.Format);
            }
            catch (TypeNotImplementedException tniEx)
            {
                IndentWriteLine("Type '{0}' not implemented.", tniEx.Type);
            }
            catch (UnknownMagicException umEx)
            {
                IndentWriteLine("Unknown magic number: {0}.", umEx.Message);
            }
            catch (Exception ex)
            {
                IndentWriteLine("Exception occured: {0}.", ex.Message);
            }
#endif
            finally
            {
                indent = baseIndent;
            }
        }

        private static void IndentWriteLine(string format = "", params object[] param)
        {
            Console.WriteLine(format.Insert(0, new string(' ', indent)), param);
        }

        /* Slightly modified from https://stackoverflow.com/a/4423615 */
        private static string GetReadableTimespan(TimeSpan span)
        {
            string formatted = string.Format("{0}{1}{2}{3}{4}",
            span.Duration().Days > 0 ? string.Format("{0:0} day{1}, ", span.Days, span.Days == 1 ? string.Empty : "s") : string.Empty,
            span.Duration().Hours > 0 ? string.Format("{0:0} hour{1}, ", span.Hours, span.Hours == 1 ? string.Empty : "s") : string.Empty,
            span.Duration().Minutes > 0 ? string.Format("{0:0} minute{1}, ", span.Minutes, span.Minutes == 1 ? string.Empty : "s") : string.Empty,
            span.Duration().Seconds > 0 ? string.Format("{0:0} second{1}, ", span.Seconds, span.Seconds == 1 ? string.Empty : "s") : string.Empty,
            span.Duration().Milliseconds > 0 ? string.Format("{0:0} millisecond{1}", span.Milliseconds, span.Milliseconds == 1 ? string.Empty : "s") : string.Empty);
            if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);
            if (string.IsNullOrEmpty(formatted)) formatted = "0 seconds";
            return formatted;
        }

        private static bool IsSubdirectory(DirectoryInfo childDir, DirectoryInfo parentDir)
        {
            if (parentDir.FullName == childDir.FullName) return true;

            DirectoryInfo child = childDir.Parent;
            while (child != null)
            {
                if (child.FullName == parentDir.FullName) return true;
                child = child.Parent;
            }

            return false;
        }
    }
}
