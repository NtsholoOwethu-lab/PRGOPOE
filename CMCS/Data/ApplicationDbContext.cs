using Microsoft.EntityFrameworkCore;
using CMCS.Models;

namespace CMCS.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Lecturer> Lecturers { get; set; }
        public DbSet<MonthlyClaim> MonthlyClaims { get; set; }
        public DbSet<SupportingDocument> SupportingDocuments { get; set; }
        public DbSet<ClaimApproval> ClaimApprovals { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<MonthlyClaim>()
                .HasOne(m => m.Lecturer)
                .WithMany(l => l.MonthlyClaims)
                .HasForeignKey(m => m.LecturerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SupportingDocument>()
                .HasOne(s => s.MonthlyClaim)
                .WithMany(m => m.SupportingDocuments)
                .HasForeignKey(s => s.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClaimApproval>()
                .HasOne(c => c.MonthlyClaim)
                .WithMany(m => m.ClaimApprovals)
                .HasForeignKey(c => c.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);

            // Remove the HasData seeding for in-memory database
        }
    }
}