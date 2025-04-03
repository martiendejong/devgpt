using System.Security.Cryptography;

namespace DevGPT.NewAPI
{
    public class Checksum
    {
        public string CalculateChecksum(string filePath)
        {
            if (!File.Exists(filePath))
                return "";
            using (var sha256 = SHA256.Create())
            using (var fileStream = File.OpenRead(filePath))
            {
                var hashBytes = sha256.ComputeHash(fileStream);
                return Convert.ToHexString(hashBytes); // Converts to a readable hex string
            }
        }
    }
}