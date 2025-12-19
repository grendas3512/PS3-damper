using System.Diagnostics;

namespace PS3Damper
{
    /// <summary>
    /// Provides functionality to mount and dismount ISO disk images using PowerShell.
    /// </summary>
    public static class IsoMounter
    {
        /// <summary>
        /// Mounts an ISO file and returns the mounted drive root path.
        /// </summary>
        /// <param name="isoPath">Full path to the ISO file to mount.</param>
        /// <param name="mountedRoot">Output parameter containing the mounted drive root (e.g., "D:\").</param>
        /// <param name="retries">Number of times to retry checking for the mounted drive. Default is 10.</param>
        /// <param name="delayMs">Delay in milliseconds between retry attempts. Default is 750ms.</param>
        /// <returns>True if the ISO was successfully mounted and the drive is accessible; otherwise, false.</returns>
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

        /// <summary>
        /// Dismounts the specified ISO file.
        /// </summary>
        /// <param name="isoPath">Full path to the ISO file to dismount.</param>
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

        /// <summary>
        /// Gets the drive letter of a mounted ISO image.
        /// </summary>
        /// <param name="isoPath">Full path to the ISO file.</param>
        /// <returns>The drive letter if found; otherwise, null.</returns>
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

        /// <summary>
        /// Escapes a path for use in PowerShell commands by wrapping it in single quotes
        /// and escaping any single quotes within the path.
        /// </summary>
        /// <param name="path">The path to escape.</param>
        /// <returns>The escaped path suitable for PowerShell.</returns>
        private static string PSQuote(string path)
        {
            // Quote for PowerShell using single quotes; escape single quotes by doubling them
            return "'" + path.Replace("'", "''") + "'";
        }
    }
}
