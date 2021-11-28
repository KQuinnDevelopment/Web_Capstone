using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
/*
Modified By:    Quinn Helm
Date:           27-11-2021
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

        public int StandingID { get; set; }
    }
}