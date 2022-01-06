using System.Collections.Generic;
using WBSAlpha.Models;
/*
Modified By:    Quinn Helm
Date:           02-01-2022
*/
namespace WBSAlpha.ViewModels
{
    /// <summary>
    /// Used by administrators to ban a given user.
    /// </summary>
    public class UserBanViewModel
    {
        public List<CoreUser> RudeUsers { get; set; }
        public List<string> RudeMessages { get; set; }
        public List<string> Reasons { get; set; }
    }
}