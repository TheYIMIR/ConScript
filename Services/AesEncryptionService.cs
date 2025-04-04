using ConScript.Core;
using ConScript.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ConScript.Services
{
    /// <summary>
    /// AES implementation of the encryption service
    /// </summary>
    public class AesEncryptionService : IEncryptionService
    {
        private const int KeySize = 256;
        private const int Iterations = 10000;

        public string Encrypt(string plainText, string password)
        {
            using var aes = Aes.Create();
            aes.KeySize = KeySize;

            // Generate random salt and IV
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] iv = RandomNumberGenerator.GetBytes(16);

            // Derive key from password using PBKDF2
            using var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            aes.Key = deriveBytes.GetBytes(aes.KeySize / 8);
            aes.IV = iv;

            using var encryptor = aes.CreateEncryptor();
            using var memoryStream = new MemoryStream();

            // Write salt and IV to the beginning of the output
            memoryStream.Write(salt, 0, salt.Length);
            memoryStream.Write(iv, 0, iv.Length);

            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            using (var streamWriter = new StreamWriter(cryptoStream))
            {
                streamWriter.Write(plainText);
            }

            return Convert.ToBase64String(memoryStream.ToArray());
        }

        public (OperationResult Result, string DecryptedText) Decrypt(string encryptedText, string password)
        {
            try
            {
                byte[] cipherTextBytes = Convert.FromBase64String(encryptedText);

                if (cipherTextBytes.Length < 32) // Salt (16) + IV (16)
                {
                    return (OperationResult.Failed("Invalid encrypted data format"), null);
                }

                using var aes = Aes.Create();
                aes.KeySize = KeySize;

                // Extract salt and IV from the cipher text
                byte[] salt = new byte[16];
                byte[] iv = new byte[16];

                Buffer.BlockCopy(cipherTextBytes, 0, salt, 0, 16);
                Buffer.BlockCopy(cipherTextBytes, 16, iv, 0, 16);

                // Derive key from password using PBKDF2
                using var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
                aes.Key = deriveBytes.GetBytes(aes.KeySize / 8);
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                using var memoryStream = new MemoryStream(cipherTextBytes, 32, cipherTextBytes.Length - 32);
                using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                using var streamReader = new StreamReader(cryptoStream);

                return (OperationResult.Succeeded(), streamReader.ReadToEnd());
            }
            catch (CryptographicException ex)
            {
                return (OperationResult.Failed($"Decryption error: {ex.Message}"), null);
            }
            catch (FormatException ex)
            {
                return (OperationResult.Failed($"Invalid format: {ex.Message}"), null);
            }
            catch (Exception ex)
            {
                return (OperationResult.Failed($"Error during decryption: {ex.Message}"), null);
            }
        }
    }
}
