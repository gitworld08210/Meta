using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace MetaCricket.Backend
{
    /// <summary>
    /// Provides AES-encrypted storage for sensitive data in PlayerPrefs.
    /// Tokens and secrets are encrypted before being written to PlayerPrefs
    /// and decrypted when read back, preventing plaintext exposure on disk.
    /// </summary>
    public static class SecureStorage
    {
        // Key derivation uses a device-unique identifier combined with a salt.
        // This is not hardware-security-module level protection, but it prevents
        // trivial reading of tokens from SharedPreferences XML on Android.
        private static readonly byte[] Salt = Encoding.UTF8.GetBytes("MetaCricket_SecureStorage_Salt_v1");

        private static byte[] _derivedKey;
        private static byte[] _derivedIV;

        /// <summary>
        /// Store an encrypted string value in PlayerPrefs.
        /// </summary>
        /// <param name="key">The PlayerPrefs key to store under.</param>
        /// <param name="value">The plaintext value to encrypt and store.</param>
        public static void SetString(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                PlayerPrefs.SetString(key, "");
                return;
            }

            try
            {
                string encrypted = Encrypt(value);
                PlayerPrefs.SetString(key, encrypted);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SecureStorage] Failed to encrypt value for key '{key}': {ex.Message}");
                // Fallback: do not store if encryption fails
            }
        }

        /// <summary>
        /// Retrieve and decrypt a string value from PlayerPrefs.
        /// </summary>
        /// <param name="key">The PlayerPrefs key to read from.</param>
        /// <param name="defaultValue">Default value if key does not exist or decryption fails.</param>
        /// <returns>The decrypted plaintext value, or defaultValue on failure.</returns>
        public static string GetString(string key, string defaultValue = "")
        {
            string stored = PlayerPrefs.GetString(key, "");

            if (string.IsNullOrEmpty(stored))
            {
                return defaultValue;
            }

            try
            {
                return Decrypt(stored);
            }
            catch (Exception)
            {
                // If decryption fails (e.g., key changed or data corrupted),
                // try reading as plaintext for migration from unencrypted storage
                Debug.LogWarning($"[SecureStorage] Decryption failed for key '{key}'. Data may be from a previous unencrypted session.");
                return stored;
            }
        }

        /// <summary>
        /// Delete a key from PlayerPrefs.
        /// </summary>
        /// <param name="key">The key to delete.</param>
        public static void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }

        /// <summary>
        /// Check if a key exists in PlayerPrefs.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists.</returns>
        public static bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        /// <summary>
        /// Save PlayerPrefs to disk.
        /// </summary>
        public static void Save()
        {
            PlayerPrefs.Save();
        }

        private static string Encrypt(string plaintext)
        {
            EnsureKeyDerived();

            using (Aes aes = Aes.Create())
            {
                aes.Key = _derivedKey;
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                using (MemoryStream ms = new MemoryStream())
                {
                    // Prepend IV to the ciphertext so we can retrieve it during decryption
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
                        cs.Write(plaintextBytes, 0, plaintextBytes.Length);
                        cs.FlushFinalBlock();
                    }

                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        private static string Decrypt(string cipherBase64)
        {
            EnsureKeyDerived();

            byte[] cipherBytes = Convert.FromBase64String(cipherBase64);

            using (Aes aes = Aes.Create())
            {
                aes.Key = _derivedKey;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Extract IV from the beginning of the ciphertext
                byte[] iv = new byte[aes.BlockSize / 8];
                Array.Copy(cipherBytes, 0, iv, 0, iv.Length);
                aes.IV = iv;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                using (MemoryStream ms = new MemoryStream(cipherBytes, iv.Length, cipherBytes.Length - iv.Length))
                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (StreamReader reader = new StreamReader(cs, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private static void EnsureKeyDerived()
        {
            if (_derivedKey != null) return;

            // Use device unique identifier as the passphrase for key derivation.
            // This ties the encryption to the specific device.
            string passphrase = SystemInfo.deviceUniqueIdentifier;

            using (Rfc2898DeriveBytes keyDerivation = new Rfc2898DeriveBytes(
                passphrase, Salt, 10000, HashAlgorithmName.SHA256))
            {
                _derivedKey = keyDerivation.GetBytes(32); // AES-256
                _derivedIV = keyDerivation.GetBytes(16);  // 128-bit IV (used as fallback only)
            }
        }
    }
}
