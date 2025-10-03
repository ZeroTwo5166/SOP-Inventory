namespace SOP.Encryption
{
    using System.Globalization;
    using System.Security.Cryptography;
    using System.Text;

    public static class EncryptionHelper
    {
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("1234567890123456"); // MUST be at least 16 characters
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("abcdefghijklmnop"); //  MUST be at least 16 characters

        // New: Rsa holders
        private static RSA? _rsaPub;
        private static RSA? _rsaPriv;

        // New: set RSA keys from PEM (call these in Program.cs)
        public static void SetRsaPublicKeyPem(string? publicPem)
        {
            if (string.IsNullOrWhiteSpace(publicPem))
            {
                throw new InvalidOperationException("RsaPublicPem missing");
            }
            _rsaPub = RSA.Create();
            _rsaPub.ImportFromPem(publicPem.AsSpan());
        }

        public static void SetRsaPrivateKeyPem(string? privatePem)
        {
            if (string.IsNullOrWhiteSpace(privatePem))
            {
                throw new InvalidOperationException("RsaPrivatePem missing");
            }
            _rsaPriv = RSA.Create();
            _rsaPriv.ImportFromPem(privatePem.AsSpan());
        }

        public static string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(string cipherText)
        {
            var buffer = Convert.FromBase64String(cipherText);
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(buffer);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }

        // --- NEW: Hybrid RSA (asymmetric) + AES-GCM (symmetric) ---
        // Encrypts data with a random AES key, then wraps that key with RSA-OAEP.
        // Returns:
        //   wrappedKey = base64(RSA-OAEP(contentKey))
        //   payload    = "GCM1:" + base64(nonce(12)|tag(16)|ciphertext)

        public static (string wrappedKey, string payload) HybridEncrypt(string plaintext)
        {
            if (_rsaPub is null)
            {
                throw new InvalidOperationException("RSA public key not set. Call SetRsaPublicKeyPem at startup.");
            }

            // 32-byte AES key per value
            byte[] contentKey = RandomNumberGenerator.GetBytes(32);

            // AES-GCM encrypt
            byte[] nonce = RandomNumberGenerator.GetBytes(12);
            byte[] plain = Encoding.UTF8.GetBytes(plaintext);
            byte[] cipher = new byte[plain.Length];
            byte[] tag = new byte[16];

            using (var aes = new AesGcm(contentKey))
                aes.Encrypt(nonce, plain, cipher, tag);

            // pack payload = nonce | tag | cipher
            var payloadBytes = new byte[nonce.Length + tag.Length + cipher.Length];
            Buffer.BlockCopy(nonce, 0, payloadBytes, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, payloadBytes, nonce.Length, tag.Length);
            Buffer.BlockCopy(cipher, 0, payloadBytes, nonce.Length + tag.Length, cipher.Length);

            string payload = "GCM1:" + Convert.ToBase64String(payloadBytes);
            string wrappedKey = Convert.ToBase64String(_rsaPub.Encrypt(contentKey, RSAEncryptionPadding.OaepSHA256));

            CryptographicOperations.ZeroMemory(contentKey);
            return (wrappedKey, payload);
        }

        public static string HybridDecrypt(string wrappedKey, string payload)
        {
            if (_rsaPriv is null)
                throw new InvalidOperationException("RSA private key not set. Call SetRsaPrivateKeyPem at startup.");
            if (string.IsNullOrWhiteSpace(payload) || !payload.StartsWith("GCM1:", StringComparison.Ordinal))
                throw new CryptographicException("Unsupported payload format.");

            // unwrap AES key
            byte[] contentKey = _rsaPriv.Decrypt(Convert.FromBase64String(wrappedKey), RSAEncryptionPadding.OaepSHA256);

            try
            {
                byte[] bytes = Convert.FromBase64String(payload.Substring(5)); // skip "GCM1:"
                var nonce = bytes.AsSpan(0, 12).ToArray();
                var tag = bytes.AsSpan(12, 16).ToArray();
                var cipher = bytes.AsSpan(28).ToArray();

                byte[] plain = new byte[cipher.Length];
                using (var aes = new AesGcm(contentKey))
                    aes.Decrypt(nonce, cipher, tag, plain);

                return Encoding.UTF8.GetString(plain);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(contentKey);
            }
        }

        // --- OPTIONAL: RSA-only helpers for tiny strings (not for bulk data) ---
        public static string RsaEncryptSmall(string plaintext)
        {
            if (_rsaPub is null) throw new InvalidOperationException("RSA public key not set.");
            byte[] enc = _rsaPub.Encrypt(Encoding.UTF8.GetBytes(plaintext), RSAEncryptionPadding.OaepSHA256);
            return Convert.ToBase64String(enc);
        }
        public static string RsaDecryptSmall(string base64Cipher)
        {
            if (_rsaPriv is null) throw new InvalidOperationException("RSA private key not set.");
            byte[] dec = _rsaPriv.Decrypt(Convert.FromBase64String(base64Cipher), RSAEncryptionPadding.OaepSHA256);
            return Encoding.UTF8.GetString(dec);
        }
    }

}
