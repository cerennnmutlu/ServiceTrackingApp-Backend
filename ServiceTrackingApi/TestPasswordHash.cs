using System.Security.Cryptography;
using System.Text;

namespace ServiceTrackingApi
{
    public class TestPasswordHash
    {
        public static void Main(string[] args)
        {
            // Test password hashing
            string password = "admin123";
            string hash = HashPassword(password);
            
            Console.WriteLine($"Password: {password}");
            Console.WriteLine($"Hash: {hash}");
            
            // Test verification
            bool isValid = VerifyPassword(password, hash);
            Console.WriteLine($"Verification: {isValid}");
            
            // Test with seed data hashes
            string seedHash1 = "jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=";
            string seedHash2 = "VIiMbxxhHm4xwKllklxUHUOhHy4CS9am8UBHEuU2HCk=";
            string seedHash3 = "8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92";
            
            Console.WriteLine($"\nTesting seed hashes:");
            Console.WriteLine($"admin123 vs seedHash1: {VerifyPassword("admin123", seedHash1)}");
            Console.WriteLine($"shift123 vs seedHash2: {VerifyPassword("shift123", seedHash2)}");
            Console.WriteLine($"security123 vs seedHash3: {VerifyPassword("security123", seedHash3)}");
        }
        
        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "salt"));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private static bool VerifyPassword(string password, string hash)
        {
            var passwordHash = HashPassword(password);
            return passwordHash == hash;
        }
    }
}