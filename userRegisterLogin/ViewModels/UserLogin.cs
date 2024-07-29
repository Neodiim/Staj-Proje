using System.ComponentModel.DataAnnotations;

namespace userRegisterLogin.ViewModels
{
    public class UserLogin
    {
        
        [Required(ErrorMessage ="required")]
        public string userPassword { get; set; }


        [Required(ErrorMessage ="Required")]
        [EmailAddress(ErrorMessage ="dogru mail adresi gir")]
        public string userMail { get; set; }
    }
}
