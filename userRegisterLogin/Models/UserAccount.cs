using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace userRegisterLogin.Models
{
    [Index(nameof(userMail), IsUnique=true)]
    public class UserAccount
    {
        [Key]
        public int userID { get; set; } 
        [Required(ErrorMessage = "user name is required")]
        public string userName { get; set; }

        [Required(ErrorMessage =" password is required")]
        public string userPassword { get; set; }


        [Required(ErrorMessage = "mail is required")]
        public string userMail { get; set; }

    }
}
