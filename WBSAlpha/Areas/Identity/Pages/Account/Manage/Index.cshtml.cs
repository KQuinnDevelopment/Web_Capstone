using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
Date:           27-11-2021
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
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        [DisplayFormat(DataFormatString = "{0:g}", ApplyFormatInEditMode = true)]
        public DateTime Age { get; private set; }
        [Display(Name = "Account Created")]
        [DisplayFormat(DataFormatString = "{0:g}", ApplyFormatInEditMode = true)]
        public DateTime Created { get; private set; }
        [Display(Name = "Average Build Rating")]
        public int AverageBuildRating { get; private set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Display(Name = "Public User Name")]
            [StringLength(45, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
            public string UserName { get; set; }

            [EmailAddress]
            [Display(Name = "Active Email")]
            public string Email { get; set; }
        }

        private async Task LoadAsync(CoreUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            int rating = 0;
            List<GameOneBuild> builds = _dbContext.GameOneBuilds.Where(b => b.UserID == user.Id).ToList();
            foreach (GameOneBuild build in builds)
            {
                rating += build.Rating;
            }
            // this does not have to be the most accurate
            AverageBuildRating = (builds.Count) > 0 ? (rating / builds.Count) : 0;
            // it is not a life or death application of rounding, so integer division will suffice

            UserName = userName;
            Email = user.Email;
            Age = user.Age;
            Created = user.Created;
            ValidUserName = !user.NormalizedUserName.Equals(user.NormalizedEmail);

            Input = new InputModel
            {
                UserName = userName,
                Email = user.Email
            };
        }

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
