using System.IO;
using System.Text;
using System.Threading.Tasks;
using CMCS.Services;
using Xunit;

namespace CMCS.Tests
{
    public class EncryptionServiceTests
    {
        [Fact]
        public async Task EncryptAndDecrypt_ReturnsOriginalContent()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "cmcs-tests");
            Directory.CreateDirectory(tempDir);

            var original = "Hello CMCS Encryption Test";
            var originalBytes = Encoding.UTF8.GetBytes(original);

            var sourceStream = new MemoryStream(originalBytes);
            var filePath = Path.Combine(tempDir, "test.enc");

            await EncryptionService.EncryptToFileAsync(sourceStream, filePath);
            var decrypted = await EncryptionService.DecryptFromFileAsync(filePath);

            var decryptedText = Encoding.UTF8.GetString(decrypted.ToArray());
            Assert.Equal(original, decryptedText);

            // cleanup
            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }
}
