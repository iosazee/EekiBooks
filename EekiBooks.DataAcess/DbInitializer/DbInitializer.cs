using EekiBooks.DataAccess;
using EekiBooks.DataAcess.Repository.IRepository;
using EekiBooks.Models;
using EekiBooks.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EekiBooks.DataAcess.DbInitializer
{
    public class DbInitializer : IDbInitializer
    {

       
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public DbInitializer(UserManager<IdentityUser> userManager,
                RoleManager<IdentityRole> roleManager,
                ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
           
        }

        public void Initialize()
        {
            //Apply migrations if they are not applied
            try
            {
                if (_db.Database.GetPendingMigrations().Count() > 0 )
                {
                    _db.Database.Migrate();
                }
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"Error applying migrations: {ex.Message}");
            }

            //Create roles, if not created
            if (!_roleManager.RoleExistsAsync(SD.Role_Admin).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_User_Comp)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_User_Indi)).GetAwaiter().GetResult();


                //Create admin user, if not created
                _userManager.CreateAsync(new ApplicationUser
                {
                    UserName = "test@eekibooks.com",
                    Email = "test@eekibooks.com",
                    Name = "Don Draper ",
                    PhoneNumber = "1234567890",
                    StreetAddress = "test 123 Drive",
                    City = "Lincoln",
                    PostalCode = "DF291BN"
                }, "Benin*10").GetAwaiter().GetResult();
               

                ApplicationUser user = _db.ApplicationUsers.FirstOrDefault(u => u.Email == "saze@myyahoo.com");

                _userManager.AddToRoleAsync(user, SD.Role_Admin).GetAwaiter().GetResult();
            }
            return;
        }
    }
}
