using Microsoft.EntityFrameworkCore;
using userRegisterLogin.Models;

namespace userRegisterLogin.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext>  options) : base(options)
        {

        }

        public DbSet<UserAccount> User { get; set; }
        public DbSet<UserFileInfo> userFileInfo { get; set; } 

        public DbSet<UserUploadFileInfo> userUploadFileInfo {  get; set; }  
         

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }


    }
}
