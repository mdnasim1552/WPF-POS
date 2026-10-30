using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace PosManagement.Services
{
    public class PasswordService
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 210_000;

        public string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize);

            return $"PBKDF2-SHA256${Iterations}$" +
                   $"{Convert.ToBase64String(salt)}$" +
                   $"{Convert.ToBase64String(hash)}";
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                var parts = storedHash.Split('$');

                if (parts.Length != 4 ||
                    parts[0] != "PBKDF2-SHA256" ||
                    !int.TryParse(parts[1], out int iterations) ||
                    iterations < 100_000 ||
                    iterations > 1_000_000)
                {
                    return false;
                }

                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expectedHash = Convert.FromBase64String(parts[3]);

                if (salt.Length != SaltSize ||
                    expectedHash.Length != KeySize)
                {
                    return false;
                }

                byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    KeySize);

                return CryptographicOperations.FixedTimeEquals(
                    actualHash,
                    expectedHash);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
