using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CMCS.Controllers;
using CMCS.Data;
using CMCS.Models;
using CMCS.Repositories;
using CMCS.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CMCS.Tests
{
    public class LecturerControllerTests
    {
        [Fact]
        public async Task DeleteDocument_ReturnsJsonSuccess()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("LecturerControllerTestsDB_" + System.Guid.NewGuid())
                .Options;

            using var ctx = new ApplicationDbContext(options);

            // seed lecturer and claim
            var lecturer = new Lecturer { LecturerId = 1, FirstName = "T", LastName = "L", Email = "t@l.com", HourlyRate = 100 };
            ctx.Lecturers.Add(lecturer);
            ctx.SaveChanges();

            var claim = new MonthlyClaim { ClaimId = 1, LecturerId = 1, Month = 1, Year = 2024, TotalHours = 10, TotalAmount = 1000, Status = ClaimStatus.Draft };
            ctx.MonthlyClaims.Add(claim);
            ctx.SaveChanges();

            // create temp uploads and encrypted file
            var tempUploads = Path.Combine(Path.GetTempPath(), "cmcs-tests-upload");
            Directory.CreateDirectory(tempUploads);
            var content = "Sample PDF content";
            var contentBytes = Encoding.UTF8.GetBytes(content);
            using var ms = new MemoryStream(contentBytes);
            var encPath = Path.Combine(tempUploads, System.Guid.NewGuid().ToString() + ".enc");
            await EncryptionService.EncryptToFileAsync(ms, encPath);

            var doc = new SupportingDocument
            {
                DocumentId = 1,
                ClaimId = claim.ClaimId,
                FileName = "sample.pdf",
                FileType = "application/pdf",
                FileSize = contentBytes.Length,
                FilePath = Path.GetFileName(encPath),
                UploadDate = System.DateTime.Now
            };
            ctx.SupportingDocuments.Add(doc);
            ctx.SaveChanges();

            // set up repository and environment
            var repo = new ClaimRepository(ctx);
            var env = new TestWebHostEnvironment { WebRootPath = Path.GetTempPath() };

            // copy encrypted file into webroot/uploads
            var uploadsDir = Path.Combine(env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);
            var destPath = Path.Combine(uploadsDir, doc.FilePath);
            File.Copy(encPath, destPath, overwrite: true);

            var logger = new NullLogger<LecturerController>();
            var controller = new LecturerController(repo, ctx, env, logger);

            // Act
            var result = await controller.DeleteDocument(doc.DocumentId) as JsonResult;

            // Assert
            Assert.NotNull(result);
            dynamic data = result.Value;
            Assert.True((bool)data.success);

            // cleanup
            if (File.Exists(encPath)) File.Delete(encPath);
            if (File.Exists(destPath)) File.Delete(destPath);
        }

        // Fixed IWebHostEnvironment stub
        private class TestWebHostEnvironment : IWebHostEnvironment
        {
            public string WebRootPath { get; set; } = Path.GetTempPath();
            public string EnvironmentName { get; set; }
            public string ApplicationName { get; set; }
            public IFileProvider WebRootFileProvider { get; set; }
            public string ContentRootPath { get; set; }
            public IFileProvider ContentRootFileProvider { get; set; }
        }
    }
}
