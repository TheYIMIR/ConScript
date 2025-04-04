using ConScript.Core;

namespace ConScript.Interfaces
{
    /// <summary>
    /// Interface for encryption services
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>
        /// Encrypts the given plain text
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <param name="password">Password to use for encryption</param>
        /// <returns>Encrypted text</returns>
        string Encrypt(string plainText, string password);

        /// <summary>
        /// Decrypts the given encrypted text
        /// </summary>
        /// <param name="encryptedText">Text to decrypt</param>
        /// <param name="password">Password to use for decryption</param>
        /// <returns>Result containing the decrypted text or error message</returns>
        (OperationResult Result, string DecryptedText) Decrypt(string encryptedText, string password);
    }
}
