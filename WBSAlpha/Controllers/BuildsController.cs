using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WBSAlpha.Authorization;
using WBSAlpha.Data;
using WBSAlpha.Models;
using WBSAlpha.ViewModels;
/*
Modified By:    Quinn Helm
Date:           02-01-2021
*/
namespace WBSAlpha.Controllers
{
    [Authorize]
    public class BuildsController : Controller
    {
        private readonly UserManager<CoreUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuthorizationService _authorization;

        public BuildsController(UserManager<CoreUser> userManager,
            ApplicationDbContext context,
            IAuthorizationService authorizationService)
        {
            _userManager = userManager;
            _context = context;
            _authorization = authorizationService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string sorting, string searchText)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                ViewData["ActiveUser"] = user.Id;
            }

            ViewBag.NameSort = String.IsNullOrEmpty(sorting) ? "build_name_desc" : "";
            ViewBag.RatingSort = sorting == "rating_asc" ? "rating_asc" : "rating_desc";
            List<GameOneBuild> builds = await _context.GameOneBuilds.ToListAsync();
            List<GameOneBuild> outBuilds = new(0);
            GameOneBuild build;
            if (!String.IsNullOrEmpty(searchText))
            {
                string converted = searchText.Trim().ToLower();
                List<GameOneBuild> temp;
                List<Weapon> weapons = await _context.Weapons.Where(w => (w.WeaponName.Contains(converted)) || (w.WeaponDescription.Contains(converted))).ToListAsync();
                List<Rune> runes = await _context.Runes.Where(r => (r.RuneName.Contains(converted)) || (r.RuneDescription.Contains(converted))).ToListAsync();
                if (weapons.Count > 0)
                {
                    foreach (Weapon w in weapons)
                    {
                        temp = builds.Where(b => (w.WeaponID == b.WeaponOne) || (w.WeaponID == b.WeaponTwo)).ToList();
                        outBuilds.Concat(temp);
                    }
                }
                if (runes.Count > 0)
                {
                    foreach (Rune r in runes)
                    {
                        temp = builds.Where(b => (r.RuneID == b.OffensiveRuneOne) || (r.RuneID == b.OffensiveRuneTwo) || (r.RuneID == b.DefensiveRune)).ToList();
                        outBuilds.Concat(temp);
                    }
                }
                temp = builds.Where(b => b.BuildName.Contains(converted)).ToList();
                outBuilds.Concat(temp);
                builds = outBuilds.Distinct().ToList();
            }
            switch (sorting) 
            {
                case "build_name_desc":
                    outBuilds = builds.OrderByDescending(b => b.BuildName).ToList();
                    break;
                case "rating_asc":
                    Dictionary<int, int> unorder = builds.ToDictionary(b => b.BuildID, b => (b.Rating / b.Votes));
                    var order = unorder.OrderBy(a => a.Value);
                    foreach (KeyValuePair<int, int> avg in order)
                    {
                        build = builds.Find(b => b.BuildID == avg.Key);
                        outBuilds.Add(build);
                    }
                    // builds = builds.OrderBy(b => (b.Rating / b.Votes)).ToList();
                    break;
                case "rating_desc":
                    Dictionary<int, int> unordered = builds.ToDictionary(b => b.BuildID, b => (b.Rating / b.Votes));
                    var ordered = unordered.OrderByDescending(a => a.Value);
                    foreach (KeyValuePair<int, int> avg in ordered)
                    {
                        build = builds.Find(b => b.BuildID == avg.Key);
                        outBuilds.Add(build);
                    }
                    break;
                default:
                    outBuilds = builds.OrderBy(b => b.BuildName).ToList();
                    break;
            }
            return View(outBuilds);
        }

        // GET: Builds/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gameOneBuild = await _context.GameOneBuilds.FirstOrDefaultAsync(m => m.BuildID == id);
            if (gameOneBuild == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(gameOneBuild.UserID);
            if (user != null)
            {
                BuildRatingModel Rater = new(); // create a new instance of the viewmodel BuildRatingModel
                Rater.BuildID = gameOneBuild.BuildID;
                Rater.BuildName = gameOneBuild.BuildName;
                Rater.Description = gameOneBuild.Description;
                Rater.Notes = gameOneBuild.Notes;
                Rater.CreationDate = gameOneBuild.CreationDate;
                Rater.CreatorName = user.UserName;
                Rater.Votes = gameOneBuild.Votes;
                Rater.Rating = (gameOneBuild.Votes > 0) ? (gameOneBuild.Rating / gameOneBuild.Votes) : 0; // integer division because precision isn't life threatening

                Rater.WeaponOne = await _context.Weapons.FindAsync(gameOneBuild.WeaponOne);
                Rater.WeaponTwo = await _context.Weapons.FindAsync(gameOneBuild.WeaponTwo);
                Rater.OffensiveRuneOne = await _context.Runes.FindAsync(gameOneBuild.OffensiveRuneOne);
                Rater.OffensiveRuneTwo = await _context.Runes.FindAsync(gameOneBuild.OffensiveRuneTwo);
                Rater.DefensiveRune = await _context.Runes.FindAsync(gameOneBuild.DefensiveRune);

                var thisUser = await _userManager.GetUserAsync(User);
                bool roleValid = await _userManager.IsInRoleAsync(thisUser, "Administrator");
                // to control whether user can rate the build or not, they must not be the build's creator
                bool isOwner = (thisUser != null) && (thisUser.Id.Equals(gameOneBuild.UserID));
                bool canDelete = isOwner || roleValid;
                ViewData["IsOwner"] = isOwner;
                ViewData["CanDelete"] = canDelete;

                return View(Rater);
            }
            return RedirectToAction(nameof(Index)); // if all else fails, return to index
        }

        // GET: Builds/Create
        public IActionResult Create()
        {
            return View(new BuildMaker(_context));
        }

        // POST: Builds/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BuildMaker build)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                string uID = user.Id;
                DateTime created = DateTime.Now;
                // I am not using the constructor with parameters in {} because I want to explicitly lay things out
                GameOneBuild gameOneBuild = new();
                gameOneBuild.BuildName = build.BuildName;
                gameOneBuild.Description = build.Description;
                gameOneBuild.Notes = build.Notes;
                gameOneBuild.WeaponOne = build.WeaponOne;
                gameOneBuild.WeaponTwo = build.WeaponTwo;
                gameOneBuild.DefensiveRune = build.DefensiveRune;
                gameOneBuild.OffensiveRuneOne = build.OffensiveRuneOne;
                gameOneBuild.OffensiveRuneTwo = build.OffensiveRuneTwo;
                gameOneBuild.UserID = uID;
                gameOneBuild.Rating = 0;
                gameOneBuild.CreationDate = created;

                _context.Add(gameOneBuild);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(new BuildMaker(_context));
        }

        // POST: rating a build
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rate(BuildRatingModel rater)
        {
            var build = await _context.GameOneBuilds.FirstOrDefaultAsync(m => m.BuildID == rater.BuildID);

            if (build != null)
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        int totalRating = build.Rating + rater.BuildRating;
                        int votes = build.Votes;
                        if (totalRating > build.Rating)
                        {
                            // this should always be true but just in case...
                            build.Rating = totalRating;
                            build.Votes = (votes + 1);
                            _context.Update(build);
                            await _context.SaveChangesAsync(); // only update the build if rating has changed, save db r/w
                        }
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!GameOneBuildExists(build.BuildID))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
            } else
            {
                return NotFound();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Builds/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gameOneBuild = await _context.GameOneBuilds.FindAsync(id);
            if (gameOneBuild == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(gameOneBuild.UserID);
            if (user != null)
            {
                var authorized = await _authorization.AuthorizeAsync(User, user, AuthorizationOperations.Delete);
                if (!authorized.Succeeded)
                {
                    return Forbid();
                }
                BuildRatingModel Rater = new(); // create a new instance of the viewmodel BuildRatingModel
                Rater.BuildID = gameOneBuild.BuildID;
                Rater.BuildName = gameOneBuild.BuildName;
                Rater.Description = gameOneBuild.Description;
                Rater.Notes = gameOneBuild.Notes;
                Rater.CreationDate = gameOneBuild.CreationDate;
                Rater.CreatorName = user.UserName;
                Rater.Votes = gameOneBuild.Votes;
                Rater.Rating = Rater.Rating = (gameOneBuild.Votes > 0) ? (gameOneBuild.Rating / gameOneBuild.Votes) : 0; // integer division because precision isn't life threatening

                Rater.WeaponOne = await _context.Weapons.FindAsync(gameOneBuild.WeaponOne);
                Rater.WeaponTwo = await _context.Weapons.FindAsync(gameOneBuild.WeaponTwo);
                Rater.OffensiveRuneOne = await _context.Runes.FindAsync(gameOneBuild.OffensiveRuneOne);
                Rater.OffensiveRuneTwo = await _context.Runes.FindAsync(gameOneBuild.OffensiveRuneTwo);
                Rater.DefensiveRune = await _context.Runes.FindAsync(gameOneBuild.DefensiveRune);

                var thisUser = await _userManager.GetUserAsync(User);
                bool roleValid = await _userManager.IsInRoleAsync(thisUser, "Administrator");
                // to control whether user can rate the build or not, they must not be the build's creator
                bool isOwner = (thisUser != null) && (thisUser.Id == gameOneBuild.UserID);
                bool canDelete = isOwner || roleValid;
                ViewData["IsOwner"] = isOwner;
                ViewData["CanDelete"] = canDelete;

                return View(Rater);
            }
            return RedirectToAction(nameof(Index)); // if all else fails, return to index
        }

        // POST: Builds/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Failed to delete the build with ID {id}.");
            }
            var authorized = await _authorization.AuthorizeAsync(User, user, AuthorizationOperations.Delete);
            if (!authorized.Succeeded)
            {
                return Forbid();
            }
            var gameOneBuild = await _context.GameOneBuilds.FindAsync(id);
            _context.GameOneBuilds.Remove(gameOneBuild);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GameOneBuildExists(int id)
        {
            return _context.GameOneBuilds.Any(e => e.BuildID == id);
        }
    }
}