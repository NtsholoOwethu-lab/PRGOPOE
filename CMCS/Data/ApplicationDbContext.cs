using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CMCS.Models;

namespace CMCS.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Lecturer> Lecturers { get; set; }
        public DbSet<MonthlyClaim> MonthlyClaims { get; set; }
        public DbSet<SupportingDocument> SupportingDocuments { get; set; }
        public DbSet<ClaimApproval> ClaimApprovals { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // REQUIRED for Identity tables to be created properly
            base.OnModelCreating(modelBuilder);

            // Lecturer → MonthlyClaims
            modelBuilder.Entity<MonthlyClaim>()
                .HasOne(m => m.Lecturer)
                .WithMany(l => l.MonthlyClaims)
                .HasForeignKey(m => m.LecturerId)
                .OnDelete(DeleteBehavior.Restrict);

            // MonthlyClaim → SupportingDocuments
            modelBuilder.Entity<SupportingDocument>()
                .HasOne(s => s.MonthlyClaim)
                .WithMany(m => m.SupportingDocuments)
                .HasForeignKey(s => s.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);

            // MonthlyClaim → ClaimApprovals
            modelBuilder.Entity<ClaimApproval>()
                .HasOne(c => c.MonthlyClaim)
                .WithMany(m => m.ClaimApprovals)
                .HasForeignKey(c => c.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
