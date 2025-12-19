namespace PS3Damper
{
    /// <summary>
    /// Provides functionality to recursively copy directory trees.
    /// </summary>
    public static class DirectoryCopier
    {
        /// <summary>
        /// Recursively copies all files from the source directory to the destination directory,
        /// preserving the directory structure.
        /// </summary>
        /// <param name="sourceDir">The source directory to copy from.</param>
        /// <param name="destinationDir">The destination directory to copy to.</param>
        public static void CopyTree(string sourceDir, string destinationDir)
        {
            // Normalize roots to ensure trailing separator
            if (!sourceDir.EndsWith(System.IO.Path.DirectorySeparatorChar))
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
}
