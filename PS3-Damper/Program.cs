using System;
using System.IO;

namespace PS3Damper
{
    /// <summary>
    /// Main program entry point for PS3 Damper - extracts ISO files by mounting and copying contents.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Main entry point of the application.
        /// </summary>
        /// <param name="args">Command line arguments. First argument should be the ISO file path.</param>
        /// <returns>Exit code: 0 for success, 1 for invalid input, 2 for mount failure.</returns>
        static int Main(string[]? args)
        {
            string? isoPath = null;

            if (args != null && args.Length > 0)
            {
                isoPath = args[0];
            }
            else
            {
                Console.Write("Enter full path to ISO file (e.g., E:\\MyDisk.iso): ");
                isoPath = Console.ReadLine();
            }

            if (string.IsNullOrWhiteSpace(isoPath))
            {
                Console.WriteLine("No input provided.");
                return 1;
            }

            if (!File.Exists(isoPath) || !string.Equals(Path.GetExtension(isoPath), ".iso", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Invalid ISO file.");
                return 1;
            }

            string isoDirectory = Path.GetDirectoryName(isoPath) ?? string.Empty;
            string isoName = Path.GetFileNameWithoutExtension(isoPath);
            string outputDir = Path.Combine(isoDirectory, isoName);

            Console.WriteLine("Mounting ISO...");
            if (!IsoMounter.Mount(isoPath, out var mountedRoot))
            {
                Console.WriteLine("Failed to mount ISO.");
                return 2;
            }

            Console.WriteLine($"Mounted at: {mountedRoot}");

            try
            {
                Console.WriteLine("Copying files to: " + outputDir);
                Directory.CreateDirectory(outputDir);
                DirectoryCopier.CopyTree(mountedRoot, outputDir);
                Console.WriteLine("Copy complete.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error copying files: " + ex.Message);
            }
            finally
            {
                Console.WriteLine("Unmounting ISO...");
                try
                {
                    IsoMounter.Dismount(isoPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to unmount: " + ex.Message);
                }
                Console.WriteLine("Done.");
            }

            return 0;
        }
    }
}
