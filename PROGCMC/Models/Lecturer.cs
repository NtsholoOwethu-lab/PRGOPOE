using System.ComponentModel.DataAnnotations;

namespace PROGCMC.Models
{
    public class Lecturer
    {

        public int LecturerID { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string EmailAddress { get; set; }
    }
}
