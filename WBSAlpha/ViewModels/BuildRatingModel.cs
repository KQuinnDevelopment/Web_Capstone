using System;
using System.ComponentModel.DataAnnotations;
/*
Modified By:    Quinn Helm
Date:           28-12-2021
*/
namespace WBSAlpha.Models
{
    /// <summary>
    /// A simple build (view) model that allows for the rating of a given item build.
    /// </summary>
    public class BuildRatingModel
    {
        public int BuildID { get; set; }

        [Display(Name = "Created By")]
        public string CreatorName { get; set; }

        [DataType(DataType.Text)]
        [StringLength(45, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
        [Display(Name = "Build Name")]
        public string BuildName { get; set; }

        [DataType(DataType.Text)]
        [StringLength(180, ErrorMessage = "The {0} must be at max {1} characters long.")]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [DataType(DataType.Text)]
        [Display(Name = "Additional Notes")]
        public string Notes { get; set; }

        [Display(Name = "Weapon One")]
        public Weapon WeaponOne { get; set; }

        [Display(Name = "Weapon Two")]
        public Weapon WeaponTwo { get; set; }

        [Display(Name = "Offensive Rune One")]
        public Rune OffensiveRuneOne { get; set; }

        [Display(Name = "Offensive Rune Two")]
        public Rune OffensiveRuneTwo { get; set; }

        [Display(Name = "Defensive Rune")]
        public Rune DefensiveRune { get; set; }

        [Required]
        [Display(Name = "Build Rating")]
        public int Rating { get; set; }

        [Display(Name = "Votes Count")]
        public int Votes { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Created On")]
        [DisplayFormat(DataFormatString = "{0:g}", ApplyFormatInEditMode = true)]
        public DateTime CreationDate { get; set; }

        [Required]
        [Range(1,5)]
        [Display(Name = "Build Rating")]
        public int BuildRating { get; set; }
    }
}