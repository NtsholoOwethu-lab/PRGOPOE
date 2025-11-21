using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PROGCMCS.Models;

namespace PROGCMCS.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Custom DbSets
        public DbSet<Lecturer> Lecturers { get; set; }
        public DbSet<MonthlyClaim> MonthlyClaims { get; set; }
        public DbSet<SupportingDocument> SupportingDocuments { get; set; }
        public DbSet<ClaimApproval> ClaimApprovals { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Important for Identity

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
        }
    }
}
