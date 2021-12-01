using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using WBSAlpha.Data;
using WBSAlpha.Models;
/*
Modified By:    Quinn Helm
Date:           27-11-2021
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

        public Games(UserManager<CoreUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _dbContext = context;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

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

        public IActionResult CreateBuild()
        {
            return View();
        }

        public IActionResult ViewBuild(int buildID)
        {
            return View();
        }

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

        public IActionResult Chathub()
        {
            return View();
        }
    }
}