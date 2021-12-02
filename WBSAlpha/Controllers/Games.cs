using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using WBSAlpha.Data;
using WBSAlpha.Models;
/*
Modified By:    Quinn Helm
Date:           01-12-2021
*/
namespace WBSAlpha.Controllers
{
    [Authorize]
    public class Games : Controller
    {
        private readonly UserManager<CoreUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        public bool validUserName { get; private set; }
        // whether the user is banned or kicked, as either makes the bool true
        public bool userBanned { get; private set; }
        public bool userKicked { get; private set; }
        public DateTime jailEnds { get; private set; }

        [BindProperty]
        public BuildInput Builder { get; set; }

        public Games(UserManager<CoreUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _dbContext = context;
        }

        /// <summary>
        /// The Games landing page.
        /// </summary>
        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        // handle builds section
        /// <summary>
        /// This presents the user with only their builds that are available in the database.
        /// </summary>
        public async Task<IActionResult> FindMyBuilds()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                ViewBag.UserID = user.Id;
            }
            ViewBag.UserBuilds = true;
            return View("Builds");
        }
        /// <summary>
        /// This finds all available builds in the database and allows for
        /// the user to be able to delete their own builds in the event they find theirs.
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> FindAllBuilds()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                ViewBag.UserID = user.Id;
            }
            else
            {
                ViewBag.UserId = "Anonymous";
            }
            ViewBag.UserBuilds = false;
            return View("Builds");
        }
        /// <summary>
        /// Present the user with a form to create a build.
        /// </summary>
        public IActionResult BuildCreator()
        {
            Builder = new BuildInput(_dbContext);
            return View("CreateBuild");
        }
        /// <summary>
        /// Attempt to create a build with the provided options.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBuild()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    // this should always result in ints but I'm using tryparse just in case...
                    _ = int.TryParse(Builder.WeaponOne, out int one);
                    _ = int.TryParse(Builder.WeaponTwo, out int two);
                    _ = int.TryParse(Builder.OffensiveRuneOne, out int oOne);
                    _ = int.TryParse(Builder.OffensiveRuneTwo, out int oTwo);
                    _ = int.TryParse(Builder.DefensiveRune, out int dRune);
                    DateTime current = DateTime.Now;
                    GameOneBuild build = new()
                    {
                        BuildName = Builder.BuildName,
                        Description = (Builder.Description != null) ? Builder.Description : "",
                        Notes = (Builder.Notes != null) ? Builder.Notes : "",
                        WeaponOne = one,
                        WeaponTwo = two,
                        OffensiveRuneOne = oOne,
                        OffensiveRuneTwo = oTwo,
                        DefensiveRune = dRune,
                        CreationDate = current,
                        Rating = 0,
                        UserID = user.Id
                    };
                    return RedirectToAction("Builds");
                }
            }
            return View("Builds");
        }
        /// <summary>
        /// Fills view data with information related to the given build, present that view to the user.
        /// </summary>
        /// <param name="buildID">ID of build to show.</param>
        public async Task<IActionResult> BuildDetails(int buildID)
        {
            GameOneBuild build = await _dbContext.GameOneBuilds.FindAsync(buildID);
            ViewData["WeaponOne"] = await _dbContext.Weapons.FindAsync(build.WeaponOne);
            ViewData["WeaponTwo"] = await _dbContext.Weapons.FindAsync(build.WeaponTwo);
            ViewData["ORuneOne"] = await _dbContext.Runes.FindAsync(build.OffensiveRuneOne);
            ViewData["ORuneTwo"] = await _dbContext.Runes.FindAsync(build.OffensiveRuneTwo);
            ViewData["DRune"] = await _dbContext.Runes.FindAsync(build.DefensiveRune);
            var user = await _userManager.GetUserAsync(User);
            bool owner = false;
            if (user != null)
            {
                owner = build.UserID == user.Id;
            }
            ViewData["UserOwnsBuild"] = owner;
            return View(build);
        }
        /// <summary>
        /// Attempts to delete the given build from the database.
        /// </summary>
        /// <param name="buildID">ID of build to delete.</param>
        public async Task<IActionResult> DeleteBuild(int buildID)
        {
            GameOneBuild build = await _dbContext.GameOneBuilds.FindAsync(buildID);
            if (build != null)
            {
                _dbContext.GameOneBuilds.Remove(build);
                await _dbContext.SaveChangesAsync();
            }
            return View("Builds");
        }
        public class BuildInput
        {
            private readonly ApplicationDbContext _dbContext;
            public List<SelectListItem> AvailableWeapons { get; private set; }
            public List<SelectListItem> AvailableRunes { get; private set; }

            public BuildInput(ApplicationDbContext context)
            {
                // no idea if this will work, here's hoping...
                _dbContext = context;
                List<Weapon> weapons = new List<Weapon>(6);
                List<Rune> runes = new List<Rune>(6);
                _dbContext.Weapons.ForEachAsync(w => { weapons.Add(w); }).Wait();
                _dbContext.Runes.ForEachAsync(r => { runes.Add(r); }).Wait();
                // just in case
                weapons.TrimExcess();
                runes.TrimExcess();
                AvailableWeapons = new List<SelectListItem>(weapons.Count);
                AvailableRunes = new List<SelectListItem>(runes.Count);
                foreach (Weapon weapon in weapons)
                {
                    AvailableWeapons.Add(new SelectListItem { Value = weapon.WeaponID.ToString(), Text = weapon.WeaponName });
                }
                foreach (Rune rune in runes)
                {
                    AvailableRunes.Add(new SelectListItem { Value = rune.RuneID.ToString(), Text = rune.RuneName });
                }
            }

            [Required]
            [DataType(DataType.Text)]
            [StringLength(45, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
            [Display(Name = "Build Name")]
            public string BuildName { get; set; }

            [DataType(DataType.Text)]
            [StringLength(180, ErrorMessage = "The {0} must be at max {1} characters long.")]
            [Display(Name = "Brief Description")]
            public string Description { get; set; }

            [DataType(DataType.Text)]
            [Display(Name = "Additional Notes")]
            public string Notes { get; set; }

            [Required]
            [Display(Name = "Weapon One")]
            public string WeaponOne { get; set; }

            [Required]
            [Display(Name = "Weapon Two")]
            public string WeaponTwo { get; set; }
            
            [Required]
            [Display(Name = "Offensive Rune One")]
            public string OffensiveRuneOne { get; set; }

            [Required]
            [Display(Name = "Offensive Rune Two")]
            public string OffensiveRuneTwo { get; set; }

            [Required]
            [Display(Name = "Defensive Rune")]
            public string DefensiveRune { get; set; }
        }

        // handle chat section
        /// <summary>
        /// Test the user's account and privileges to see if they are allowed in chat.
        /// If the user is not allowed, present them with a justification barring them from chat.
        /// </summary>
        public async Task<IActionResult> Chat()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                validUserName = false;
            }
            else
            {
                validUserName = !user.NormalizedUserName.Equals(user.NormalizedEmail);
                DateTime time = DateTime.Now;
                Standing uStanding = await _dbContext.Standings.FirstOrDefaultAsync(u => u.StandingID == user.StandingID);

                if (uStanding != null) {
                    if (uStanding.BanEnds != null)
                    {
                        if (uStanding.BanEnds > time)
                        {
                            userBanned = true;
                            jailEnds = (DateTime)uStanding.BanEnds;
                        } else
                        {
                            uStanding.BanEnds = null;
                            await _dbContext.SaveChangesAsync();
                            userBanned = false;
                        }
                    }
                    if (!userBanned && uStanding.KickEnds != null)
                    {
                        if (uStanding.KickEnds > time)
                        {
                            userKicked = true;
                            jailEnds = (DateTime) uStanding.KickEnds;
                        }
                        else
                        {
                            uStanding.KickEnds = null;
                            await _dbContext.SaveChangesAsync();
                            userKicked = false;
                        }
                    }
                }
            }
            ViewBag.ValidUser = validUserName;
            ViewBag.Banned = userBanned;
            ViewBag.Kicked = userKicked;
            if (userBanned || userKicked)
            {
                ViewBag.JailEnds = jailEnds;
            }
            return View();
        }

        /// <summary>
        /// If the user has met the prerequisit conditions in the Chat() method,
        /// this allows them to enter the chatroom.
        /// </summary>
        public IActionResult Chathub()
        {
            return View();
        }
    }
}