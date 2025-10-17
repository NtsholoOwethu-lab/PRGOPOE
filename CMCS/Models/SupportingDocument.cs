using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMCS.Models
{
    public class SupportingDocument
    {
        [Key]
        public int DocumentId { get; set; }

        [Required]
        public int ClaimId { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string FileType { get; set; } = string.Empty;

        [Required]
        public long FileSize { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("ClaimId")]
        public virtual MonthlyClaim MonthlyClaim { get; set; } = null!;
    }
}