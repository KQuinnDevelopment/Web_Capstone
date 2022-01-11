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
Date:           07-01-2022
*/
namespace WBSAlpha.Controllers
{
    [Authorize]
    public class GamesController : Controller
    {
        private readonly UserManager<CoreUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        public bool ValidUserName { get; private set; }
        // whether the user is banned or kicked, as either makes the bool true
        public bool UserBanned { get; private set; }
        public bool UserKicked { get; private set; }
        public DateTime JailEnds { get; private set; }

        public GamesController(UserManager<CoreUser> userManager, ApplicationDbContext context)
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

        /// <summary>
        /// Test the user's account and privileges to see if they are allowed in chat.
        /// If the user is not allowed, present them with a justification barring them from chat.
        /// </summary>
        public async Task<IActionResult> Chat()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ValidUserName = false;
            }
            else
            {
                if (await _userManager.IsInRoleAsync(user, "Administrator"))
                {
                    return Forbid(); // administrators not allowed in chat
                }
                ValidUserName = !user.NormalizedUserName.Equals(user.NormalizedEmail);
                DateTime time = DateTime.Now;
                Standing uStanding = await _dbContext.Standings.FirstOrDefaultAsync(u => u.StandingID == user.StandingID);

                if (uStanding != null) {
                    if (uStanding.BanEnds != null)
                    {
                        if (uStanding.BanEnds > time)
                        {
                            UserBanned = true;
                            JailEnds = (DateTime)uStanding.BanEnds;
                        } else
                        {
                            uStanding.BanEnds = null;
                            await _dbContext.SaveChangesAsync();
                            UserBanned = false;
                        }
                    }
                    if (!UserBanned && uStanding.KickEnds != null)
                    {
                        if (uStanding.KickEnds > time)
                        {
                            UserKicked = true;
                            JailEnds = (DateTime) uStanding.KickEnds;
                        }
                        else
                        {
                            uStanding.KickEnds = null;
                            await _dbContext.SaveChangesAsync();
                            UserKicked = false;
                        }
                    }
                }
            }
            ViewBag.ValidUser = ValidUserName;
            ViewBag.Banned = UserBanned;
            ViewBag.Kicked = UserKicked;
            if (UserBanned || UserKicked)
            {
                ViewBag.JailEnds = JailEnds;
            }
            return View();
        }

        /// <summary>
        /// This is used by the chats view to enter the actual chat client.
        /// </summary>
        public ActionResult Chats()
        {
            return View("ChatClient");
        }
    }
}