using System.ComponentModel.DataAnnotations;
using WBSAlpha.Models;
/*
Modified By:    Quinn Helm
Date:           02-01-2022
*/
namespace WBSAlpha.ViewModels
{
    /// <summary>
    /// Used by moderators and administrators to view the given user's profile.
    /// </summary>
    public class ProfileViewModel
    {
        public CoreUser TargetUser { get; set; }
        public Standing UserStanding { get; set; }
        [Display(Name = "Date of Birth")]
        public string UserAge { get; set; } // specified separately to fix formatting
        public bool IsAModerator { get; set; }
    }
}