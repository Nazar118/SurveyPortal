using Microsoft.AspNetCore.Identity;
using SurveyPortal.API.Models;

namespace SurveyPortal.API.Helpers
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

            // 1. Rolleri oluştur (Admin ve User)
            string[] roleNames = { "Admin", "User" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    // Rol yoksa yeni oluştur
                    await roleManager.CreateAsync(new AppRole { Name = roleName });
                }
            }

            // 2. Varsayılan Admin Kullanıcısını oluştur
            var adminUser = await userManager.FindByEmailAsync("admin@surveyportal.com");
            if (adminUser == null)
            {
                var newAdmin = new AppUser
                {
                    UserName = "admin",
                    Email = "admin@surveyportal.com",
                    FirstName = "Sistem",
                    LastName = "Yöneticisi"
                };

                var result = await userManager.CreateAsync(newAdmin, "Admin123*");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }
        }
    }
}