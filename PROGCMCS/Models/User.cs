using System.ComponentModel.DataAnnotations;

namespace PROGCMCS.Models
{
    public class User
    {
        public int Id { get; set; }


        [Required]
        public string UserId { get; set; }


        [Required]
        [StringLength(50)]
        public string Name { get; set; }


        [Required]
        [StringLength(50)]
        public string Surname { get; set; }


        [Required]
        [StringLength(50)]
        public string Department { get; set; }


        [Required]
        [Range(0, 999999)]
        public decimal DefaultRatePerJob { get; set; }


        [Required]
        [StringLength(50)]
        public string RoleName { get; set; }

    }
}
