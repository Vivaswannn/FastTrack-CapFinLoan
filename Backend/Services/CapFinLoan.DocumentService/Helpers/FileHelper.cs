using Microsoft.AspNetCore.Hosting;

namespace CapFinLoan.DocumentService.Helpers
{
    /// <summary>
    /// Static helper class for file upload operations.
    /// Handles validation, path generation, and size formatting.
    /// </summary>
    public static class FileHelper
    {
        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".pdf", ".jpg", ".jpeg", ".png"
            };

        private const long MaxFileSizeBytes = 5_242_880; // 5 MB

        /// <summary>
        /// Checks whether the file extension is in the allowed list.
        /// Allowed: .pdf .jpg .jpeg .png (case insensitive).
        /// </summary>
        public static bool IsValidExtension(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            return !string.IsNullOrWhiteSpace(extension)
                   && AllowedExtensions.Contains(extension);
        }

        /// <summary>
        /// Checks whether the file size is within the 5 MB limit.
        /// </summary>
        public static bool IsValidFileSize(long sizeBytes)
        {
            return sizeBytes <= MaxFileSizeBytes;
        }

        /// <summary>
        /// Generates a unique storage filename based on a new GUID plus the original extension.
        /// Example: "3f2504e0-4f89-11d3-pdf"
        /// </summary>
        public static string GenerateStoredFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName).ToLower();
            return Guid.NewGuid().ToString() + extension;
        }

        /// <summary>
        /// Returns the full absolute path for saving an uploaded file.
        /// Path pattern: {WebRootPath}/uploads/{year}/{month:D2}/{storedFileName}
        /// </summary>
        public static string GetUploadPath(IWebHostEnvironment env, string storedFileName)
        {
            var now = DateTime.UtcNow;
            return Path.Combine(
                env.WebRootPath,
                "uploads",
                now.Year.ToString(),
                now.Month.ToString("D2"),
                storedFileName);
        }

        /// <summary>
        /// Returns the relative path used for database storage.
        /// Pattern: uploads/{year}/{month:D2}/{storedFileName}
        /// </summary>
        public static string GetRelativePath(string storedFileName)
        {
            var now = DateTime.UtcNow;
            return $"uploads/{now.Year}/{now.Month:D2}/{storedFileName}";
        }

        /// <summary>
        /// Formats a byte count into a human-readable string.
        /// Examples: "2.4 MB", "512 KB", "256 B"
        /// </summary>
        public static string FormatFileSize(long bytes)
        {
            if (bytes >= 1_048_576)
                return $"{bytes / 1_048_576.0:F1} MB";

            if (bytes >= 1_024)
                return $"{bytes / 1_024.0:F0} KB";

            return $"{bytes} B";
        }

        /// <summary>
        /// Ensures that the directory containing <paramref name="filePath"/> exists.
        /// Creates all intermediate directories if necessary.
        /// </summary>
        public static void EnsureDirectoryExists(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
        }
    }
}
