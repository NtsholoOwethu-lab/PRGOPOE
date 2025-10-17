using Microsoft.EntityFrameworkCore;
using CMCS.Data;
using CMCS.Models;
using CMCS.Repositories;
using Xunit;
using System;
using System.Threading.Tasks;

namespace CMCS.Tests
{
    public class ClaimRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ClaimRepository _repository;

        public ClaimRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new ClaimRepository(_context);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            var lecturer = new Lecturer
            {
                LecturerId = 1,
                FirstName = "Test",
                LastName = "Lecturer",
                Email = "test@university.com",
                HourlyRate = 250.00m,
                Department = "Test Department"
            };

            _context.Lecturers.Add(lecturer);
            _context.SaveChanges();
        }

        [Fact]
        public async Task CreateClaimAsync_ValidClaim_ReturnsCreatedClaim()
        {
            // Arrange
            var claim = new MonthlyClaim
            {
                LecturerId = 1,
                Month = 10,
                Year = 2024,
                TotalHours = 40,
                TotalAmount = 10000
            };

            // Act
            var result = await _repository.CreateClaimAsync(claim);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ClaimStatus.Submitted, result.Status);
            Assert.True(result.ClaimId > 0);
        }

        [Fact]
        public async Task GetClaimsByLecturerAsync_ValidLecturerId_ReturnsClaims()
        {
            // Arrange
            var claim = new MonthlyClaim
            {
                LecturerId = 1,
                Month = 10,
                Year = 2024,
                TotalHours = 40,
                TotalAmount = 10000,
                Status = ClaimStatus.Submitted
            };
            await _repository.CreateClaimAsync(claim);

            // Act
            var result = await _repository.GetClaimsByLecturerAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task ApproveClaimAsync_ValidClaim_UpdatesStatus()
        {
            // Arrange
            var claim = new MonthlyClaim
            {
                LecturerId = 1,
                Month = 10,
                Year = 2024,
                TotalHours = 40,
                TotalAmount = 10000,
                Status = ClaimStatus.Submitted
            };
            var createdClaim = await _repository.CreateClaimAsync(claim);

            // Act
            var result = await _repository.ApproveClaimAsync(createdClaim.ClaimId, 1, ApproverType.ProgrammeCoordinator, "Test approval");

            // Assert
            Assert.True(result);
            var updatedClaim = await _repository.GetClaimByIdAsync(createdClaim.ClaimId);
            Assert.NotNull(updatedClaim);
            Assert.Equal(ClaimStatus.UnderReview, updatedClaim.Status);
        }

        [Fact]
        public async Task RejectClaimAsync_ValidClaim_UpdatesStatusToRejected()
        {
            // Arrange
            var claim = new MonthlyClaim
            {
                LecturerId = 1,
                Month = 10,
                Year = 2024,
                TotalHours = 40,
                TotalAmount = 10000,
                Status = ClaimStatus.Submitted
            };
            var createdClaim = await _repository.CreateClaimAsync(claim);

            // Act
            var result = await _repository.RejectClaimAsync(createdClaim.ClaimId, 1, ApproverType.ProgrammeCoordinator, "Test rejection");

            // Assert
            Assert.True(result);
            var updatedClaim = await _repository.GetClaimByIdAsync(createdClaim.ClaimId);
            Assert.NotNull(updatedClaim);
            Assert.Equal(ClaimStatus.Rejected, updatedClaim.Status);
        }

        [Fact]
        public async Task GetClaimByIdAsync_InvalidId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetClaimByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteClaimAsync_ValidClaim_ReturnsTrue()
        {
            // Arrange
            var claim = new MonthlyClaim
            {
                LecturerId = 1,
                Month = 10,
                Year = 2024,
                TotalHours = 40,
                TotalAmount = 10000
            };
            var createdClaim = await _repository.CreateClaimAsync(claim);

            // Act
            var result = await _repository.DeleteClaimAsync(createdClaim.ClaimId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteClaimAsync_InvalidClaim_ReturnsFalse()
        {
            // Act
            var result = await _repository.DeleteClaimAsync(999);

            // Assert
            Assert.False(result);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}