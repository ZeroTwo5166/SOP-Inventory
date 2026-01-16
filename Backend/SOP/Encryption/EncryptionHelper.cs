namespace SOP.Encryption
{
    using System.Security.Cryptography;
    using System.Text;

    public static class EncryptionHelper
    {
        private static readonly byte[] Key = Encoding.UTF8.GetBytes(
            Environment.GetEnvironmentVariable("ENCRYPTION_KEY")
            ?? "12345678901234567890123456789012" // 32 bytes
        );

        private static readonly byte[] LegacyIV = Encoding.UTF8.GetBytes(
            Environment.GetEnvironmentVariable("ENCRYPTION_IV")
            ?? "abcdefghijklmnop" // legacy static IV (16 bytes)
        );

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            using var aes = Aes.Create();
            aes.Key = Key;
            aes.GenerateIV(); // new secure IV

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            // Encode new format: IV:cipher
            return $"{Convert.ToBase64String(aes.IV)}:{Convert.ToBase64String(ms.ToArray())}";
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            // Detect new format by presence of colon
            if (cipherText.Contains(':'))
                return DecryptNewFormat(cipherText);
            else
                return DecryptLegacyFormat(cipherText);
        }

        // New: IV:CIPHER format
        private static string DecryptNewFormat(string cipherText)
        {
            var parts = cipherText.Split(':');
            if (parts.Length != 2)
                throw new Exception("Invalid encrypted format");

            var iv = Convert.FromBase64String(parts[0]);
            var buffer = Convert.FromBase64String(parts[1]);

            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = iv;

            using var ms = new MemoryStream(buffer);
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }

        // Old: Base64( AES-CBC with static IV )
        private static string DecryptLegacyFormat(string cipherText)
        {
            var buffer = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = LegacyIV; // old static IV

            using var ms = new MemoryStream(buffer);
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
    }
}
