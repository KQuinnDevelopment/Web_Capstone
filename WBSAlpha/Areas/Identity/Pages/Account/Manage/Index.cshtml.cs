using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WBSAlpha.Authorization;
using WBSAlpha.Data;
using WBSAlpha.Models;
/*
Modified By:    Quinn Helm
Date:           07-01-2022
*/
namespace WBSAlpha.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public partial class IndexModel : PageModel
    {
        private readonly UserManager<CoreUser> _userManager;
        private readonly SignInManager<CoreUser> _signInManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly IAuthorizationService _authorization;

        public IndexModel(
            UserManager<CoreUser> userManager,
            SignInManager<CoreUser> signInManager,
            ApplicationDbContext context,
            IAuthorizationService authorizationService) : base()
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _dbContext = context;
            _authorization = authorizationService;
        }

        public string UserName { get; set; }
        public bool ValidUserName { get; private set; }
        [EmailAddress]
        [Display(Name = "Active Email")]
        public string Email { get; set; }
        [Display(Name = "Date of Birth")]
        public string Age { get; private set; } // string to fix formatting 
        [Display(Name = "Account Created")]
        [DisplayFormat(DataFormatString = "{0:g}", ApplyFormatInEditMode = true)]
        public DateTime Created { get; private set; }
        [Display(Name = "Average Build Rating")]
        public int AverageBuildRating { get; private set; }
        [DataType(DataType.DateTime)]
        [Display(Name = "Last Login")]
        [DisplayFormat(DataFormatString = "{0:g}", ApplyFormatInEditMode = true)]
        public DateTime LastLogin { get; private set; }
        [DataType(DataType.DateTime)]
        [Display(Name = "Last Time Kicked")]
        [DisplayFormat(DataFormatString = "{0:g}", ApplyFormatInEditMode = true)]
        public DateTime LastKicked { get; private set; }
        public int KickTotal { get; private set; }
        [DataType(DataType.DateTime)]
        [Display(Name = "Last Time Banned")]
        [DisplayFormat(DataFormatString = "{0:g}", ApplyFormatInEditMode = true)]
        public DateTime LastBanned { get; private set; }
        public int BanTotal { get; private set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// The input model to bind changable parameters to, from the Identity index form.
        /// </summary>
        public class InputModel
        {
            [Display(Name = "Public User Name")]
            [StringLength(45, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
            public string UserName { get; set; }

            [StringLength(45, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [EmailAddress]
            [Display(Name = "Active Email")]
            public string Email { get; set; }
        }

        /// <summary>
        /// Loads the given user profile, populating important and end-user relevant data.
        /// </summary>
        private async Task LoadAsync(CoreUser user)
        {
            string userName = user.UserName;
            Standing uStanding = await _dbContext.Standings.FirstOrDefaultAsync(s => s.StandingID == user.StandingID);
            int rating = 0;
            int votes = 0;
            List<GameOneBuild> builds = _dbContext.GameOneBuilds.Where(b => b.UserID == user.Id).ToList();
            foreach (GameOneBuild build in builds)
            {
                rating += build.Rating;
                votes += build.Votes;
            }
            // this does not have to be the most accurate
            if (votes == 0)
            {
                AverageBuildRating = 0;
            }
            else
            {
                AverageBuildRating = (builds.Count) > 0 ? ((rating / votes) / builds.Count) : 0;
            }
            // it is not a life or death application of rounding, so integer division will suffice
            UserName = userName;
            Email = user.Email;
            Age = user.Age.ToShortDateString(); // better formatting
            Created = user.Created;
            ValidUserName = !user.NormalizedUserName.Equals(user.NormalizedEmail);
            LastKicked = (uStanding.KickEnds != null) ? ((DateTime) uStanding.KickEnds) : DateTime.MinValue;
            LastBanned = (uStanding.BanEnds != null) ? ((DateTime)uStanding.BanEnds) : DateTime.MinValue;
            KickTotal = uStanding.KickTotal;
            BanTotal = uStanding.BanTotal;

            Input = new InputModel
            {
                UserName = userName,
                Email = user.Email
            };
        }

        /// <summary>
        /// If the user is authorized to view this profile, load their information and return the profile view.
        /// </summary>
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }
            // ensure that the user is allowed to view the profile before loading it
            var authorized = await _authorization.AuthorizeAsync(User, user, AuthorizationOperations.Read);
            if (!authorized.Succeeded)
            {
                return Forbid();
            }

            await LoadAsync(user);
            return Page();
        }

        /// <summary>
        /// When the user updates their information on the Identity index form, this is responsible for 
        /// updating it within the database.
        /// </summary>
        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }
            // ensure that the user is allowed to perform the action before even considering their input
            var authorized = await _authorization.AuthorizeAsync(User, user, AuthorizationOperations.Update);
            if (!authorized.Succeeded)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            await _userManager.UpdateAsync(user);

            ValidUserName = !user.NormalizedUserName.Equals(user.NormalizedEmail);

            await _signInManager.RefreshSignInAsync(user);
            if (!ValidUserName)
            {
                StatusMessage = "Your username should not be the same as your email";
            } 
            else
            {
                StatusMessage = "Your profile has been updated";
            }
            return RedirectToPage();
        }
    }
}
