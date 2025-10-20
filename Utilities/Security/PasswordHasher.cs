using System;
using System.Security.Cryptography;

namespace CarRentalManagment.Utilities.Security
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100_000;

        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password cannot be null or empty", nameof(password));
            }

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var key = DeriveKey(password, salt);
            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(key);
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            {
                return false;
            }

            var parts = storedHash.Split(':');
            if (parts.Length != 2)
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[0]);
            var expectedKey = Convert.FromBase64String(parts[1]);
            var actualKey = DeriveKey(password, salt);
            return CryptographicOperations.FixedTimeEquals(expectedKey, actualKey);
        }

        private static byte[] DeriveKey(string password, byte[] salt)
        {
            using var algorithm = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            return algorithm.GetBytes(KeySize);
        }
    }
}
