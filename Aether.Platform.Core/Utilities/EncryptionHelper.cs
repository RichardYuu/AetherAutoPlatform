using System;
using System.Security.Cryptography;
using System.Text;

namespace Aether.Platform.Core.Utilities
{
    public static class EncryptionHelper
    {
        private static readonly byte[] Salt = new byte[] { 0x53, 0x75, 0x6E, 0x6E, 0x79, 0x4F, 0x70, 0x74 };

        public static string Encrypt(string plainText, string password)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;
            using (var aes = Aes.Create())
            {
                var key = new Rfc2898DeriveBytes(password, Salt, 10000);
                aes.Key = key.GetBytes(32);
                aes.IV = key.GetBytes(16);
                using (var encryptor = aes.CreateEncryptor())
                {
                    var plainBytes = Encoding.UTF8.GetBytes(plainText);
                    var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                    return Convert.ToBase64String(cipherBytes);
                }
            }
        }

        public static string Decrypt(string cipherText, string password)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;
            using (var aes = Aes.Create())
            {
                var key = new Rfc2898DeriveBytes(password, Salt, 10000);
                aes.Key = key.GetBytes(32);
                aes.IV = key.GetBytes(16);
                using (var decryptor = aes.CreateDecryptor())
                {
                    var cipherBytes = Convert.FromBase64String(cipherText);
                    var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                    return Encoding.UTF8.GetString(plainBytes);
                }
            }
        }
    }
}