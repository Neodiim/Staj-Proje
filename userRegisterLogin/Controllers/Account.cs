using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Collections.Immutable;
using System.Security.Claims;
using userRegisterLogin.Data;
using userRegisterLogin.Models;
using userRegisterLogin.ViewModels;


namespace userRegisterLogin.Controllers
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using System.Data;
    public class Account : Controller
    {

        private readonly AppDbContext _context;

        private readonly IConfiguration _configuration;


        //
        private readonly IPasswordHasher<UserAccount> _passwordHasher;
        //

        public Account(AppDbContext appDbContext, IPasswordHasher<UserAccount> passwordHasher, IConfiguration configuration)
        {
            _context = appDbContext;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }


        public IActionResult Index()
        {

            return View(_context.User.ToList());
        }

        public IActionResult Registration()
        {

            return View();
        }

        [HttpPost]
        public IActionResult Registration(UserRegistration model)
        {
            if(ModelState.IsValid)
            {
                UserAccount account = new UserAccount
                {
                    userMail = model.userMail,
                    userName = model.userName
                };

                // Şifreyi hash'le
                account.userPassword = _passwordHasher.HashPassword(account, model.userPassword);


                try
                {
                    _context.User.Add(account);
                    _context.SaveChanges();

                    ModelState.Clear();
                    ViewBag.Message = $"{account.userName} registered successfully.";
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", "Please enter unique email or password");
                    return View(model);

                    throw;
                }
                return View();

            }
            return View(model);
        }


        public IActionResult Login()
        {

            return View();  
        }

        [HttpPost]
        public IActionResult Login(UserLogin model)
        {

            if(ModelState.IsValid)
            {

                var user = _context.User.Where(x => x.userMail == model.userMail).FirstOrDefault();

                if (user != null)
                {
                    var result = _passwordHasher.VerifyHashedPassword(user, user.userPassword, model.userPassword);

                    if (result == PasswordVerificationResult.Success)
                    { 

                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, user.userMail),
                            new Claim("Name", user.userName),
                            new Claim(ClaimTypes.Role, "User"),
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                        return RedirectToAction("SecurePage");

                    }

                    
                }
                else
                {
                    ModelState.AddModelError("", "User mail or password is not correct");
                }

            }
            return View(model);
        }


        public IActionResult LogOut() 
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index"); 
        }



        [Authorize]
        public async Task<IActionResult> SecurePage()
        {
            ViewBag.Name = User.Identity.Name;
            var userId = User.FindFirstValue(ClaimTypes.Name);

            var userUploadFileInfo = await _context.userUploadFileInfo
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.UploadDate)
                .ToListAsync();

            return View(userUploadFileInfo);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult>SecurePage(IFormFile file)
        {
            ExcelPackage.LicenseContext = LicenseContext.Commercial;
            if (file == null || file.Length == 0)
            {
                return Content("Dosya seçilmedi.");
            }


            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (fileExtension != ".xlsx")
            {
               
                return Content("Lütfen sadece .xlsx uzantılı dosya yükleyin.");

            }


            var userId = User.FindFirstValue(ClaimTypes.Name);
            var fileName = Path.GetFileNameWithoutExtension(file.FileName);
            var tableName = $"Excel_{fileName}_{DateTime.Now:yyyyMMddHHmmss}";
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension.Rows;
                    var colCount = worksheet.Dimension.Columns;

                    // Sütun isimlerini oluştur
                    var columnNames = new List<string> { "Id", "UserId", "Cihaz" }; 
                    var currentTime = new DateTime(2023, 1, 1, 0, 0, 0); 
                    var dayCounter = 0;
                    for (int col = 1; col <= colCount - 1; col++) 
                    {
                        var columnName = currentTime.ToString("HH:mm");
                        if (columnNames.Contains(columnName))
                        {
                            dayCounter++;
                            columnName = $"{columnName}_Day{dayCounter}";
                        }
                        columnNames.Add(columnName);
                        currentTime = currentTime.AddMinutes(15);
                    }

                    var createTableSql = $"CREATE TABLE {tableName} (Id INT IDENTITY(1,1) PRIMARY KEY, UserId NVARCHAR(450), Cihaz NVARCHAR(MAX), ";
                    for (int col = 3; col < columnNames.Count; col++) 
                    {
                        createTableSql += $"[{columnNames[col]}] NVARCHAR(MAX), ";
                    }
                    createTableSql = createTableSql.TrimEnd(',', ' ') + ")";
                    await _context.Database.ExecuteSqlRawAsync(createTableSql);

                    
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var insertSql = $"INSERT INTO {tableName} (UserId, Cihaz, ";
                        for (int col = 3; col < columnNames.Count; col++)
                        {
                            insertSql += $"[{columnNames[col]}], ";
                        }
                        insertSql = insertSql.TrimEnd(',', ' ') + ") VALUES (@UserId, @Cihaz, ";
                        for (int col = 2; col <= colCount; col++) 
                        {
                            insertSql += $"@Column{col}, ";
                        }
                        insertSql = insertSql.TrimEnd(',', ' ') + ")";

                        var parameters = new List<Microsoft.Data.SqlClient.SqlParameter>
                {
                    new Microsoft.Data.SqlClient.SqlParameter("@UserId", userId),
                        new Microsoft.Data.SqlClient.SqlParameter("@Cihaz", (object)worksheet.Cells[row, 1].Value ?? DBNull.Value)
                };
                        for (int col = 2; col <= colCount; col++)
                        {
                            var cellValue = worksheet.Cells[row, col].Value?.ToString();
                            parameters.Add(new Microsoft.Data.SqlClient.SqlParameter($"@Column{col}", (object)cellValue ?? DBNull.Value));
                        }
                        await _context.Database.ExecuteSqlRawAsync(insertSql, parameters);
                    }

                    var userUploadFileInfo = new UserUploadFileInfo
                    {
                        UserId = userId,
                        FileName = fileName,
                        TableName = tableName,
                        UploadDate = DateTime.Now
                    };
                    _context.userUploadFileInfo.Add(userUploadFileInfo);
                    await _context.SaveChangesAsync();
                }
            }
            //return Content("Dosya başarıyla yüklendi ve veriler kaydedildi.");
            return RedirectToAction("SecurePage");

        }


        //[Authorize]
        public async Task<IActionResult> GetUserExcelData(string tableName)
        {
           string _connectionString;
            _connectionString = _configuration.GetConnectionString("AppDbContext");



            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Console.WriteLine($"UserId: {userId}");
                Console.WriteLine($"TableName: {tableName}");

                var dataQuery = $"SELECT * FROM [UserLogin].[dbo].[{tableName}]";
                Console.WriteLine($"SQL Query: {dataQuery}");

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(dataQuery, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (!reader.HasRows)
                        {
                            return Ok(new { message = $"'{tableName}' tablosu boş." });
                        }

                        var dataTable = new DataTable();
                        dataTable.Load(reader);

                        Console.WriteLine($"Data count: {dataTable.Rows.Count}");

                        // DataTable'ı JSON'a çevir
                        var jsonResult = new List<Dictionary<string, object>>();
                        foreach (DataRow row in dataTable.Rows)
                        {
                            var dict = new Dictionary<string, object>();
                            foreach (DataColumn col in dataTable.Columns)
                            {
                                dict[col.ColumnName] = row[col];
                            }
                            jsonResult.Add(dict);
                        }

                        return Json(jsonResult);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserExcelData: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return StatusCode(500, "Sunucu tarafında bir hata oluştu.");
            }

        }



        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteUserExcelData(string tableName)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.Name);

                // Kullanıcının bu tabloyu silme yetkisi var mı kontrol et
                var fileInfo = await _context.userUploadFileInfo.FirstOrDefaultAsync(f => f.TableName == tableName && f.UserId == userId);
                if (fileInfo == null)
                {
                    return NotFound("Tablo bulunamadı veya silme yetkiniz yok.");
                }

                // Tabloyu sil
                await _context.Database.ExecuteSqlRawAsync($"DROP TABLE IF EXISTS [{tableName}]");

                // UserUploadFileInfo tablosundan kaydı sil
                _context.userUploadFileInfo.Remove(fileInfo);
                //_context.UserUploadFileInfo.Remove(fileInfo);
                await _context.SaveChangesAsync();

                return Ok("Tablo başarıyla silindi.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteUserExcelData: {ex.Message}");
                return StatusCode(500, "Tablo silinirken bir hata oluştu.");
            }
        }
















    }
}
