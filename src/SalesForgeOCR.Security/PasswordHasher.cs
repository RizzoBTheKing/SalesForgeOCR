using System;
using System.Security.Cryptography;

namespace SalesForgeOCR.Security
{
    public class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 200_000;

        public static string Hash(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);
            using var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var key = derive.GetBytes(KeySize);
            var parts = new byte[1 + SaltSize + KeySize];
            parts[0] = 1; // version
            Buffer.BlockCopy(salt, 0, parts, 1, SaltSize);
            Buffer.BlockCopy(key, 0, parts, 1 + SaltSize, KeySize);
            return Convert.ToBase64String(parts);
        }

        public static bool Verify(string password, string hashed)
        {
            try
            {
                var parts = Convert.FromBase64String(hashed);
                if (parts[0] != 1) return false;
                var salt = new byte[SaltSize];
                Buffer.BlockCopy(parts, 1, salt, 0, SaltSize);
                var key = new byte[KeySize];
                Buffer.BlockCopy(parts, 1 + SaltSize, key, 0, KeySize);
                using var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
                var attempt = derive.GetBytes(KeySize);
                return CryptographicOperations.FixedTimeEquals(attempt, key);
            }
            catch
            {
                return false;
            }
        }
    }
}
