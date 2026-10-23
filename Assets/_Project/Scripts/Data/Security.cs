using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FirstHand.Data
{
    public static class PinHasher
    {
        private const int Iterations = 10000;

        public static string NewSalt()
        {
            var salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            return Convert.ToBase64String(salt);
        }

        public static string Hash(string pin, string salt)
        {
            using (var kdf = new Rfc2898DeriveBytes(pin, Convert.FromBase64String(salt), Iterations))
                return Convert.ToBase64String(kdf.GetBytes(32));
        }

        public static bool Verify(string pin, string salt, string expectedHash)
        {
            if (string.IsNullOrEmpty(salt) || string.IsNullOrEmpty(expectedHash)) return false;
            var a = Convert.FromBase64String(Hash(pin, salt));
            var b = Convert.FromBase64String(expectedHash);
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }

    public class AesFileCrypto
    {
        private readonly byte[] encKey;
        private readonly byte[] macKey;

        public AesFileCrypto(byte[] masterKey)
        {
            if (masterKey == null || masterKey.Length != 32) throw new ArgumentException("Need a 32 byte key");
            using (var sha = SHA256.Create())
            {
                encKey = sha.ComputeHash(Concat(masterKey, Encoding.UTF8.GetBytes("enc")));
                macKey = sha.ComputeHash(Concat(masterKey, Encoding.UTF8.GetBytes("mac")));
            }
        }

        public byte[] Encrypt(string plainText)
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Key = encKey;
                aes.GenerateIV();
                byte[] cipher;
                using (var enc = aes.CreateEncryptor())
                {
                    var plain = Encoding.UTF8.GetBytes(plainText);
                    cipher = enc.TransformFinalBlock(plain, 0, plain.Length);
                }
                var body = Concat(aes.IV, cipher);
                using (var hmac = new HMACSHA256(macKey)) return Concat(body, hmac.ComputeHash(body));
            }
        }

        public string Decrypt(byte[] data)
        {
            if (data == null || data.Length < 16 + 32) throw new CryptographicException("File too short");
            int bodyLen = data.Length - 32;
            var body = new byte[bodyLen];
            Buffer.BlockCopy(data, 0, body, 0, bodyLen);
            using (var hmac = new HMACSHA256(macKey))
            {
                var tag = hmac.ComputeHash(body);
                int diff = 0;
                for (int i = 0; i < 32; i++) diff |= tag[i] ^ data[bodyLen + i];
                if (diff != 0) throw new CryptographicException("File was modified");
            }
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Key = encKey;
                var iv = new byte[16];
                Buffer.BlockCopy(body, 0, iv, 0, 16);
                aes.IV = iv;
                using (var dec = aes.CreateDecryptor())
                    return Encoding.UTF8.GetString(dec.TransformFinalBlock(body, 16, bodyLen - 16));
            }
        }

        private static byte[] Concat(byte[] a, byte[] b)
        {
            var r = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, r, 0, a.Length);
            Buffer.BlockCopy(b, 0, r, a.Length, b.Length);
            return r;
        }
    }

    public static class DeviceKeyProvider
    {
        public static byte[] GetOrCreate(string directory)
        {
            string path = Path.Combine(directory, ".fh_key");
            if (File.Exists(path))
            {
                var existing = File.ReadAllBytes(path);
                if (existing.Length == 32) return existing;
            }
            var key = new byte[32];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(key);
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(path, key);
            return key;
        }
    }
}
