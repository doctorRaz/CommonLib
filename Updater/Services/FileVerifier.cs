using System;
using System.IO;
using System.Security.Cryptography;

namespace dRz.Updater.Services
{
    /// <summary>
    /// Проверяет размер и SHA-256 загруженного файла.
    /// </summary>
    public static class FileVerifier
    {
        /// <summary>
        /// Проверяет файл по ожидаемому размеру и SHA-256.
        /// </summary>
        public static bool Verify(
            string filePath,
            long expectedSize,
            string expectedSha256)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            if (new FileInfo(filePath).Length != expectedSize)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(expectedSha256))
            {
                return false;
            }

            using FileStream stream = File.OpenRead(filePath);
            using SHA256 sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(stream);
            string actualHash = Convert.ToHexString(hash);

            return string.Equals(
                actualHash,
                expectedSha256,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}