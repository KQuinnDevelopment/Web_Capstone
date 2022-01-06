using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
/*
Modified By:    Quinn Helm
Date:           05-01-2022
*/
namespace WBSAlpha.Models
{
    public class CoreUser : IdentityUser 
    {
        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Date of Birth")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime Age { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Account Created")]
        [DisplayFormat(DataFormatString = "{0:g}", ApplyFormatInEditMode = true)]
        public DateTime Created { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Last Logged In")]
        [DisplayFormat(DataFormatString = "{0:g}", ApplyFormatInEditMode = true)]
        public DateTime LastLogin { get; set; }

        public int StandingID { get; set; }
    }
}