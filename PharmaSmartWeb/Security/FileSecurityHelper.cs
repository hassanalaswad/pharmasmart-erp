using System;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace PharmaSmartWeb.Security
{
    public static class FileSecurityHelper
    {
        public static bool IsValidImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            // 1. Check Extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
                return false;

            // 2. Check Magic Numbers (Signatures) to prevent malicious files disguised as images
            using (var reader = new BinaryReader(file.OpenReadStream()))
            {
                var signatures = new byte[][]
                {
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, // JPEG
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 }, // JPEG
                    new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 }, // JPEG
                    new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } // PNG
                };

                var headerBytes = reader.ReadBytes(8);
                bool isValid = false;

                foreach (var signature in signatures)
                {
                    if (headerBytes.Length >= signature.Length)
                    {
                        bool match = true;
                        for (int i = 0; i < signature.Length; i++)
                        {
                            if (headerBytes[i] != signature[i])
                            {
                                match = false;
                                break;
                            }
                        }
                        if (match)
                        {
                            isValid = true;
                            break;
                        }
                    }
                }

                // Reset stream position for later copying
                file.OpenReadStream().Position = 0;

                return isValid;
            }
        }

        public static string SanitizeFileName(string originalFileName)
        {
            if (string.IsNullOrWhiteSpace(originalFileName))
                return Guid.NewGuid().ToString();

            // Prevent path traversal attacks like ../../../windows/system32/cmd.exe
            var pureFileName = Path.GetFileName(originalFileName);
            
            // Remove any potential dangerous characters or script extensions
            var extension = Path.GetExtension(pureFileName).ToLowerInvariant();
            
            return Guid.NewGuid().ToString("N") + extension;
        }
    }
}
