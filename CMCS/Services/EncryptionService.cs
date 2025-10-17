using System.Security.Cryptography;
using System.Text;

namespace CMCS.Services
{
    public static class EncryptionService
    {
        // NOTE: For demo purposes the key is derived from a passphrase.
        // In production, store keys securely (Azure Key Vault / environment variables) and rotate.
        private static readonly byte[] _key = CreateKeyFromPassphrase("CMCS-Demo-Passphrase-ChangeMe!");

        private static byte[] CreateKeyFromPassphrase(string passphrase)
        {
            // Derive a 32-byte key using PBKDF2 with a constant salt (demo). Use per-app salt or secure key store in prod.
            var salt = Encoding.UTF8.GetBytes("CMCS-Demo-Salt-ChangeMe");
            using var kdf = new Rfc2898DeriveBytes(passphrase, salt, 100_000, HashAlgorithmName.SHA256);
            return kdf.GetBytes(32);
        }

        // Encrypt the stream and write to disk (filePath). Returns the filePath written.
        public static async Task<string> EncryptToFileAsync(Stream sourceStream, string filePath, CancellationToken cancellationToken = default)
        {
            // Produce a random IV for AES
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Key = _key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();
            var iv = aes.IV;

            // We'll compute HMAC-SHA256 over (iv + ciphertext) to protect integrity
            using var hmac = new HMACSHA256(_key);

            // Create output file and write: [IV length (1)][IV][ciphertext][HMAC(32)]
            using var outFs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);

            // Write IV length (byte) and IV bytes
            outFs.WriteByte((byte)iv.Length);
            await outFs.WriteAsync(iv, 0, iv.Length, cancellationToken);

            // Create crypto transform and write ciphertext
            using var crypto = aes.CreateEncryptor(aes.Key, iv);
            using var cryptoStream = new CryptoStream(outFs, crypto, CryptoStreamMode.Write);

            // Copy source -> cryptoStream
            await sourceStream.CopyToAsync(cryptoStream, 81920, cancellationToken);
            await cryptoStream.FlushAsync(cancellationToken);
            cryptoStream.FlushFinalBlock();

            // compute HMAC by re-reading file (excluding the trailing HMAC itself). Simpler: compute HMAC as we wrote.
            // For simplicity, compute HMAC over (IV length + IV + ciphertext) by reading file content.
            outFs.Flush();
            outFs.Position = 0;
            using var ms = new MemoryStream();
            await outFs.CopyToAsync(ms, cancellationToken);
            var contentBytes = ms.ToArray();

            var hmacBytes = hmac.ComputeHash(contentBytes);

            // Append HMAC
            await outFs.WriteAsync(hmacBytes, 0, hmacBytes.Length, cancellationToken);
            await outFs.FlushAsync(cancellationToken);

            return filePath;
        }

        // Decrypt file from disk and return a MemoryStream with plaintext
        public static async Task<MemoryStream> DecryptFromFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("Encrypted file not found.", filePath);

            var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);

            if (fileBytes.Length < 1 + 16 + 32)
                throw new InvalidOperationException("Encrypted file is too short or corrupted.");

            // Read IV length and IV
            int ivLen = fileBytes[0];
            if (ivLen <= 0 || ivLen > 32) throw new InvalidOperationException("Invalid IV length.");

            var iv = new byte[ivLen];
            Buffer.BlockCopy(fileBytes, 1, iv, 0, ivLen);

            // HMAC is the last 32 bytes
            var hmacTag = new byte[32];
            Buffer.BlockCopy(fileBytes, fileBytes.Length - 32, hmacTag, 0, 32);

            // Ciphertext is the bytes between (1 + ivLen) and (length - 32)
            var cipherStart = 1 + ivLen;
            var cipherLength = fileBytes.Length - 32 - cipherStart;
            if (cipherLength <= 0) throw new InvalidOperationException("No ciphertext found.");

            var cipherBytes = new byte[cipherLength];
            Buffer.BlockCopy(fileBytes, cipherStart, cipherBytes, 0, cipherLength);

            // Verify HMAC
            using var hmac = new HMACSHA256(_key);
            // compute expected HMAC over entire content before the tag (i.e., everything except the last 32 bytes)
            var dataForHmac = new byte[fileBytes.Length - 32];
            Buffer.BlockCopy(fileBytes, 0, dataForHmac, 0, dataForHmac.Length);
            var expectedHmac = hmac.ComputeHash(dataForHmac);

            if (!CryptographicOperations.FixedTimeEquals(expectedHmac, hmacTag))
                throw new CryptographicException("HMAC validation failed. File may be corrupted or tampered with.");

            // Decrypt ciphertext
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Key = _key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var plainMs = new MemoryStream();
            using (var ms = new MemoryStream(cipherBytes))
            using (var decryptor = aes.CreateDecryptor(aes.Key, iv))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            {
                await cs.CopyToAsync(plainMs, cancellationToken);
            }

            plainMs.Position = 0;
            return plainMs;
        }
    }
}
