using System.ComponentModel.DataAnnotations;

namespace PROGCMC.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Position { get; set; } // Programme Coordinator / Academic Manager etc.
    }
}
   
// if anything is wrong check models first.