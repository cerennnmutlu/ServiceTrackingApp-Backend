using System.Security.Cryptography;
using System.Text;

namespace ServiceTrackingApi.Security
{
    public static class PasswordHasher
    {
        public static string Hash(string password, int iterations = 100_000, int saltSize = 16, int keySize = 32)
        {
            // Her kullanıcı/parola için güçlü rastgele salt
            byte[] salt = RandomNumberGenerator.GetBytes(saltSize);

            // PBKDF2 türevi (SHA256)
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password), //byte- array dönüşümü
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                keySize
            );
            //stringe dönüştür
            return $"PBKDF2-SHA256.{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public static bool Verify(string password, string stored)
        {
            var parts = stored.Split('.');
            if (parts.Length != 4 || parts[0] != "PBKDF2-SHA256")
                return false;

            int iterations = int.Parse(parts[1]);
            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] expected = Convert.FromBase64String(parts[3]);

            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),//kullanının girdiği parola
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expected.Length
            );

            // Timing attack'e karşı sabit süreli karşılaştırma
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
    }
}
