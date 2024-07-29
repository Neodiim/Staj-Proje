using System.ComponentModel.DataAnnotations;

namespace userRegisterLogin.Models
{
    public class UserFileInfo
    {
        [Key]
        public string tableID { get; set; }
        public string userData { get; set; }
        public string numberData { get; set; }
    }
}
