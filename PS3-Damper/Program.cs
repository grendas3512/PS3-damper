using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

class Program
{
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
                     try { IsoMounter.Dismount(isoPath); }
                     catch (Exception ex) { Console.WriteLine("Failed to unmount: " + ex.Message); }
                     Console.WriteLine("Done.");
              }

              return 0;
       }
}

static class IsoMounter
{
       public static bool Mount(string isoPath, out string mountedRoot, int retries = 10, int delayMs = 750)
       {
              mountedRoot = string.Empty;

              // Elevation required for mount
              var mountProc = new ProcessStartInfo
              {
                     FileName = "powershell.exe",
                     Arguments = $"-NoProfile -NonInteractive -Command Mount-DiskImage -ImagePath {PSQuote(isoPath)}",
                     Verb = "runas",
                     UseShellExecute = true,
                     CreateNoWindow = true
              };

              using (var p = Process.Start(mountProc))
              {
                     p?.WaitForExit();
              }

              // Retry until drive letter appears and the root is accessible
              for (int i = 0; i < retries; i++)
              {
                     var letter = GetMountedDriveLetter(isoPath);
                     if (!string.IsNullOrEmpty(letter))
                     {
                            var root = letter.EndsWith(":", StringComparison.Ordinal) ? letter + "\\" : letter + ":\\";
                            if (Directory.Exists(root))
                            {
                                   mountedRoot = root;
                                   return true;
                            }
                     }
                     Thread.Sleep(delayMs);
              }

              return false;
       }

       public static void Dismount(string isoPath)
       {
              var dismountProc = new ProcessStartInfo
              {
                     FileName = "powershell.exe",
                     Arguments = $"-NoProfile -NonInteractive -Command Dismount-DiskImage -ImagePath {PSQuote(isoPath)}",
                     Verb = "runas",
                     UseShellExecute = true,
                     CreateNoWindow = true
              };

              using (var p = Process.Start(dismountProc))
              {
                     p?.WaitForExit();
              }
       }

       private static string? GetMountedDriveLetter(string isoPath)
       {
              var cmd = $"(Get-DiskImage -ImagePath {PSQuote(isoPath)} | Get-Volume).DriveLetter";
              var getDriveProc = new ProcessStartInfo
              {
                     FileName = "powershell.exe",
                     Arguments = $"-NoProfile -NonInteractive -Command {cmd}",
                     RedirectStandardOutput = true,
                     RedirectStandardError = true,
                     UseShellExecute = false,
                     CreateNoWindow = true
              };

              using (var result = Process.Start(getDriveProc))
              {
                     if (result == null) return null;
                     string output = result.StandardOutput.ReadToEnd().Trim();
                     result.WaitForExit();
                     if (string.IsNullOrWhiteSpace(output)) return null;
                     return output.Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
              }
       }

       private static string PSQuote(string path)
       {
              // Quote for PowerShell using single quotes; escape single quotes by doubling them
              return "'" + path.Replace("'", "''") + "'";
       }
}

static class DirectoryCopier
{
       public static void CopyTree(string sourceDir, string destinationDir)
       {
              // Normalize roots to ensure trailing separator
              if (!sourceDir.EndsWith(Path.DirectorySeparatorChar))
              {
                     sourceDir += Path.DirectorySeparatorChar;
              }

              foreach (var file in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
              {
                     string relativePath = Path.GetRelativePath(sourceDir, file);
                     string destFile = Path.Combine(destinationDir, relativePath);
                     string? destFolder = Path.GetDirectoryName(destFile);
                     if (!string.IsNullOrEmpty(destFolder))
                     {
                            Directory.CreateDirectory(destFolder);
                     }
                     File.Copy(file, destFile, overwrite: true);
              }
       }
}
