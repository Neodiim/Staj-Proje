using System.ComponentModel.DataAnnotations;

namespace userRegisterLogin.ViewModels
{
    public class UserRegistration
    {
        [Required(ErrorMessage ="xxx")]
        public string userPassword { get; set; }
       
        
        [Required(ErrorMessage ="yyy")]
        [EmailAddress(ErrorMessage ="please enter valid email")]
        public string userMail { get; set; }


        [Required(ErrorMessage = "user name is required")]
        public string userName { get; set; }
    }
}
