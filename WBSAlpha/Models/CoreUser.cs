using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
/*
Modified By:    Quinn Helm
Date:           17-10-2021
*/
namespace WBSAlpha.Models
{
    public class CoreUser : IdentityUser 
    {
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        [DisplayFormat(DataFormatString = "{0:g}", ApplyFormatInEditMode = true)]
        public DateTime Age { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Account Created")]
        [DisplayFormat(DataFormatString = "{0:g}", ApplyFormatInEditMode = true)]
        public DateTime Created { get; set; }

        [Required]
        [Display(Name = "User Standing ID")]
        public Standing Standing { get; set; }
    }
}