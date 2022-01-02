using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
/*
Modified By:    Quinn Helm
Date:           27-12-2021
*/
namespace WBSAlpha.Models
{
    public class GameOneBuild
    {
        [Key]
        [Display(Name = "Build ID")]
        public int BuildID { get; set; }

        [ForeignKey("WeaponID")]
        [Display(Name = "Weapon One")]
        public int WeaponOne { get; set; }

        [ForeignKey("WeaponID")]
        [Display(Name = "Weapon Two")]
        public int WeaponTwo { get; set; }

        [ForeignKey("RuneID")]
        [Display(Name = "Offensive Rune One")]
        public int OffensiveRuneOne { get; set; }

        [ForeignKey("RuneID")]
        [Display(Name = "Offensive Rune Two")]
        public int OffensiveRuneTwo { get; set; }

        [ForeignKey("RuneID")]
        [Display(Name = "Defensive Rune")]
        public int DefensiveRune { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [StringLength(45, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
        [Display(Name = "Name")]
        public string BuildName { get; set; }

        [DataType(DataType.Text)]
        [StringLength(180, ErrorMessage = "The {0} must be at max {1} characters long.")]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [DataType(DataType.Text)]
        [Display(Name = "Additional Notes")]
        public string Notes { get; set; }

        [Required]
        [Display(Name = "Build Rating")]
        public int Rating { get; set; }

        // I realized there's no way to calculate average if you aren't tracking number of times the build is rated
        [Required]
        [Display(Name = "Votes Count")]
        public int Votes { get; set; } 

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Created On")]
        [DisplayFormat(DataFormatString = "{0:g}", ApplyFormatInEditMode = true)]
        public DateTime CreationDate { get; set; }

        [ForeignKey("Id")]
        [Display(Name = "Created By")]
        public string UserID { get; set; }
    }
}