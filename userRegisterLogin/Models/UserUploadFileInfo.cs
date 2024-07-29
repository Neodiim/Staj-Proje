using System.ComponentModel.DataAnnotations;

namespace userRegisterLogin.Models
{
    public class UserUploadFileInfo
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public string FileName { get; set; }

        public string TableName {  get; set; }  

        public DateTime UploadDate { get; set; }





    }
}
