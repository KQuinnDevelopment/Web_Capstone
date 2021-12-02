using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using WBSAlpha.Models;

namespace WBSAlpha.Data
{
    public static class DbStart
    {
        public static string PW { get; set; }
        public static async Task<int> SeedRolesAndUsers(IServiceProvider serviceProvider)
        {
            // if db doesn't exist, create it
            var _dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
            _dbContext.Database.Migrate();
            var _roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var _userManager = serviceProvider.GetRequiredService<UserManager<CoreUser>>();

            if (_roleManager.Roles.Count() > 0)
            {
                return 1; // for logging
            }
            int result = await ImportantRoles(_roleManager);
            if (result != 0)
            {
                return 2; // for logging
            }
            result = await ImportantUsers(_userManager, _dbContext, PW);
            if (result != 0)
            {
                return 3; // for logging
            }
            return 0;
        }

        private static async Task<int> ImportantRoles(RoleManager<IdentityRole> roleManager)
        {
            bool exists = await roleManager.RoleExistsAsync("Administrator");
            if (!exists)
            {
                var result = await roleManager.CreateAsync(new IdentityRole("Administrator"));
                if (!result.Succeeded)
                {
                    return 1;
                }
            }
            exists = await roleManager.RoleExistsAsync("Moderator");
            if (!exists)
            {
                var result = await roleManager.CreateAsync(new IdentityRole("Moderator"));
                if (!result.Succeeded)
                {
                    return 2;
                }
            }
            return 0;
        }

        private static async Task<int> ImportantUsers(UserManager<CoreUser> userManager, ApplicationDbContext context, string pw)
        {
            CoreUser hasAdmin = await userManager.FindByNameAsync("ADMINISTRATOR");
            if (hasAdmin == null)
            {
                Standing uStanding = new Standing();
                DateTime atm = DateTime.Now;
                CoreUser admin = new()
                {
                    Email = "testadmin@wbsgames.ca",
                    UserName = "ADMINISTRATOR",
                    Age = atm,
                    EmailConfirmed = true,
                    StandingID = uStanding.StandingID,
                    Created = atm
                };
                await context.Standings.AddAsync(uStanding);
                await context.SaveChangesAsync();
                var result = await userManager.CreateAsync(admin, pw);
                if (!result.Succeeded)
                {
                    return 1;
                }
                {
                    result = await userManager.AddToRoleAsync(admin, "Administrator");
                    if (!result.Succeeded)
                    {
                        return 2;
                    }
                }
            }
            CoreUser hasMod = await userManager.FindByNameAsync("THEWOLF");
            if (hasMod == null)
            {
                Standing uStanding = new Standing();
                DateTime atm = DateTime.Now;
                CoreUser mod = new()
                {
                    Email = "testmod@wbsgames.ca",
                    UserName = "THEWOLF",
                    Age = atm,
                    EmailConfirmed = true,
                    StandingID = uStanding.StandingID,
                    Created = atm
                };
                await context.Standings.AddAsync(uStanding);
                await context.SaveChangesAsync();
                var result = await userManager.CreateAsync(mod, pw);
                if (!result.Succeeded)
                {
                    return 3;
                } else
                {
                    result = await userManager.AddToRoleAsync(mod, "Moderator");
                    if (!result.Succeeded)
                    {
                        return 4;
                    }
                }
            }
            return 0;
        }
    }
}