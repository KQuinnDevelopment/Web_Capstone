using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using WBSAlpha.Data;
using WBSAlpha.Models;
/*
Modified By:    Quinn Helm
Date:           31-12-2021
*/
namespace WBSAlpha.ViewModels
{
    /// <summary>
    /// This build (view) model is intended to capture the important user-input elements of an item build for a game.
    /// </summary>
    public class BuildMaker
    {
        [Required]
        [StringLength(45, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
        [Display(Name = "Build Name")]
        public string BuildName { get; set; }

        [StringLength(100, ErrorMessage = "The {0} must be at max {1} characters long.")]
        [Display(Name = "Brief Description")]
        public string Description { get; set; }

        [Display(Name = "Additional Notes")]
        public string Notes { get; set; }

        [Display(Name = "Weapon One")]
        public int WeaponOne { get; set; }

        [Display(Name = "Weapon Two")]
        public int WeaponTwo { get; set; }

        [Display(Name = "Offensive Rune One")]
        public int OffensiveRuneOne { get; set; }

        [Display(Name = "Offensive Rune Two")]
        public int OffensiveRuneTwo { get; set; }

        [Display(Name = "Defensive Rune")]
        public int DefensiveRune { get; set; }

        [BindNever]
        public List<SelectListItem> AvailableWeapons { get; set; }
        [BindNever]
        public List<SelectListItem> AvailableRunes { get; set; }

        /// <summary>
        /// Empty Constructor.
        /// </summary>
        public BuildMaker() { }

        public BuildMaker(ApplicationDbContext _context)
        {
            List<Weapon> weapons = _context.Weapons.ToList();
            List<Rune> runes = _context.Runes.ToList();
            AvailableWeapons = new List<SelectListItem>(weapons.Count);
            AvailableRunes = new List<SelectListItem>(runes.Count);
            foreach (Weapon weapon in weapons)
            {
                AvailableWeapons.Add(new SelectListItem { Value = weapon.WeaponID.ToString(), Text = weapon.WeaponName });
            }
            foreach (Rune rune in runes)
            {
                AvailableRunes.Add(new SelectListItem { Value = rune.RuneID.ToString(), Text = rune.RuneName });
            }
        }
    }
}