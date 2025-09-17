using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PROGCMC.Models
{
    public class SupportingDocument
    {

        public int DocumentID { get; set; }
        public int ClaimID { get; set; }
        public string FileName { get; set; }
        public DateTime UploadDate { get; set; } = DateTime.Now;
    }
}

