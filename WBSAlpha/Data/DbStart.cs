using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using WBSAlpha.Models;
/*
Modified By:    Quinn Helm
Date:           20-12-2021
*/
namespace WBSAlpha.Data
{
    public static class DbStart
    {
        public static AppSecrets PW { get; set; }
        public static async Task<int> SeedRolesAndUsers(IServiceProvider serviceProvider)
        {
            // if db doesn't exist, create it
            var _dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
            _dbContext.Database.Migrate();
            var _roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            if (_roleManager.Roles.Any())
            {
                return 1; // for logging
            }
            int result = await ImportantRoles(_roleManager);
            if (result != 0)
            {
                return 2; // for logging
            }
            result = await ImportantUsers(serviceProvider, PW);
            if (result != 0)
            {
                return 3; // for logging
            }
            
            return 0;
        }

        public static async Task<int> SeedWeaponsAndRunes(IServiceProvider serviceProvider)
        {
            // if db doesn't exist, create it
            var _dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
            _dbContext.Database.Migrate();

            if (!_dbContext.Runes.Any())
            {
                Rune newRune = new()
                {
                    RuneName = "Empty",
                    RuneDescription = "Essence of Nothing."
                };
                await _dbContext.Runes.AddAsync(newRune);
                newRune = new()
                {
                    RuneName = "Poison Rune",
                    RuneDescription = "Essence of Death condensed into a potent catalyst."
                };
                await _dbContext.Runes.AddAsync(newRune);
                newRune = new()
                {
                    RuneName = "Chaos Rune",
                    RuneDescription = "Essence of Nature condensed into a potent catalyst."
                };
                await _dbContext.Runes.AddAsync(newRune);
                newRune = new()
                {
                    RuneName = "Lightning Rune",
                    RuneDescription = "Essence of Impact condensed into a potent catalyst."
                };
                await _dbContext.Runes.AddAsync(newRune);
                newRune = new()
                {
                    RuneName = "Fire Rune",
                    RuneDescription = "Essence of Warmth condensed into a potent catalyst."
                };
                await _dbContext.Runes.AddAsync(newRune);
                newRune = new()
                {
                    RuneName = "Frost Rune",
                    RuneDescription = "Essence of Avalanche condensed into a potent catalyst."
                };
                await _dbContext.Runes.AddAsync(newRune);
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                return 1; // for logging
            }

            if (!_dbContext.Weapons.Any())
            {
                Weapon newWeapon = new()
                {
                    WeaponName = "Empty",
                    WeaponDescription = "Quick! Check your bags for anything you can throw at them!",
                    WeaponHit = 0,
                    WeaponCrit = 0,
                    WeaponDamage = 0
                };
                await _dbContext.Weapons.AddAsync(newWeapon);
                newWeapon = new()
                {
                    WeaponName = "Axe",
                    WeaponDescription = "Not for throwing. Good for cleaving.",
                    WeaponHit = 85,
                    WeaponCrit = 10,
                    WeaponDamage = 30
                };
                await _dbContext.Weapons.AddAsync(newWeapon);
                newWeapon = new()
                {
                    WeaponName = "Bow",
                    WeaponDescription = "Good enough for the hunter-gatherers, good enough for you.",
                    WeaponHit = 85,
                    WeaponCrit = 15,
                    WeaponDamage = 20
                };
                await _dbContext.Weapons.AddAsync(newWeapon);
                newWeapon = new()
                {
                    WeaponName = "Cannon",
                    WeaponDescription = "'Why outthink your opponent when you can blow them up?' - Modern War Arts Philosophy",
                    WeaponHit = 70,
                    WeaponCrit = 10,
                    WeaponDamage = 45
                };
                await _dbContext.Weapons.AddAsync(newWeapon);
                newWeapon = new()
                {
                    WeaponName = "Hammer",
                    WeaponDescription = "'Think of your enemies as if they were nails.' - Modern War Arts Philosophy",
                    WeaponHit = 80,
                    WeaponCrit = 5,
                    WeaponDamage = 40
                };
                await _dbContext.Weapons.AddAsync(newWeapon);
                newWeapon = new()
                {
                    WeaponName = "Spear",
                    WeaponDescription = "'Stick them with the pointy end.' - Modern War Arts Philosophy",
                    WeaponHit = 80,
                    WeaponCrit = 20,
                    WeaponDamage = 25
                };
                await _dbContext.Weapons.AddAsync(newWeapon);
                newWeapon = new()
                {
                    WeaponName = "Sword",
                    WeaponDescription = "Hack and Slash.",
                    WeaponHit = 85,
                    WeaponCrit = 20,
                    WeaponDamage = 15
                };
                await _dbContext.Weapons.AddAsync(newWeapon);
                await _dbContext.SaveChangesAsync();
            } 
            else
            {
                return 2; // for logging
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

        private static async Task<int> ImportantUsers(IServiceProvider provider, AppSecrets pw)
        {
            ApplicationDbContext _context = provider.GetRequiredService<ApplicationDbContext>();
            UserManager<CoreUser> userManager = provider.GetRequiredService<UserManager<CoreUser>>();
            CoreUser hasAdmin = await userManager.FindByNameAsync("ADMINISTRATOR");
            if (hasAdmin == null)
            {
                Standing uStanding = new();
                await _context.Standings.AddAsync(uStanding);
                await _context.SaveChangesAsync();
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
                var result = await userManager.CreateAsync(admin, pw.AdminPassword);
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
                Standing uStanding = new();
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
                await _context.Standings.AddAsync(uStanding);
                await _context.SaveChangesAsync();
                var result = await userManager.CreateAsync(mod, pw.ModPassword);
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