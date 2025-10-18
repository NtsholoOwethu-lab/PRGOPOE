using System.Security.Cryptography;
using System.Text;

namespace CMCS.Services
{
    public static class EncryptionService
    {
        // Demo passphrase - replace with secure env var in production
        private static readonly byte[] _key = CreateKeyFromPassphrase(Environment.GetEnvironmentVariable("CMCS_KEY") ?? "CMCS-Demo-Passphrase-ChangeMe!");

        private static byte[] CreateKeyFromPassphrase(string passphrase)
        {
            var salt = Encoding.UTF8.GetBytes("CMCS-Demo-Salt-ChangeMe");
            using var kdf = new Rfc2898DeriveBytes(passphrase, salt, 100_000, HashAlgorithmName.SHA256);
            return kdf.GetBytes(32);
        }

        public static async Task<string> EncryptToFileAsync(Stream sourceStream, string filePath, CancellationToken cancellationToken = default)
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Key = _key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();
            var iv = aes.IV;

            using var hmac = new HMACSHA256(_key);

            using var outFs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            outFs.WriteByte((byte)iv.Length);
            await outFs.WriteAsync(iv, 0, iv.Length, cancellationToken);

            using var crypto = aes.CreateEncryptor(aes.Key, iv);
            using var cryptoStream = new CryptoStream(outFs, crypto, CryptoStreamMode.Write);

            await sourceStream.CopyToAsync(cryptoStream, 81920, cancellationToken);
            await cryptoStream.FlushAsync(cancellationToken);
            cryptoStream.FlushFinalBlock();

            outFs.Flush();
            outFs.Position = 0;

            using var ms = new MemoryStream();
            await outFs.CopyToAsync(ms, cancellationToken);
            var contentBytes = ms.ToArray();

            var hmacBytes = hmac.ComputeHash(contentBytes);

            // ✅ FIXED: no await here
            outFs.Seek(0, SeekOrigin.End);
            await outFs.WriteAsync(hmacBytes, 0, hmacBytes.Length, cancellationToken);
            await outFs.FlushAsync(cancellationToken);

            return filePath;
        }

        public static async Task<MemoryStream> DecryptFromFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("Encrypted file not found.", filePath);

            var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);

            if (fileBytes.Length < 1 + 16 + 32)
                throw new InvalidOperationException("Encrypted file is too short or corrupted.");

            int ivLen = fileBytes[0];
            var iv = new byte[ivLen];
            Buffer.BlockCopy(fileBytes, 1, iv, 0, ivLen);

            var hmacTag = new byte[32];
            Buffer.BlockCopy(fileBytes, fileBytes.Length - 32, hmacTag, 0, 32);

            var cipherStart = 1 + ivLen;
            var cipherLength = fileBytes.Length - 32 - cipherStart;
            var cipherBytes = new byte[cipherLength];
            Buffer.BlockCopy(fileBytes, cipherStart, cipherBytes, 0, cipherLength);

            using var hmac = new HMACSHA256(_key);
            var dataForHmac = new byte[fileBytes.Length - 32];
            Buffer.BlockCopy(fileBytes, 0, dataForHmac, 0, dataForHmac.Length);
            var expectedHmac = hmac.ComputeHash(dataForHmac);

            if (!CryptographicOperations.FixedTimeEquals(expectedHmac, hmacTag))
                throw new CryptographicException("HMAC validation failed. File may be corrupted or tampered with.");

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
