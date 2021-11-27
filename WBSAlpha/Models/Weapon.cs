using System.ComponentModel.DataAnnotations;
/*
Modified By:    Quinn Helm
Date:           18-09-2021
*/
namespace WBSAlpha.Models
{
    public class Weapon
    {
        [Key]
        [Display(Name = "Weapon ID")]
        public int WeaponID { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [StringLength(45, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
        [Display(Name = "Weapon Name")]
        public string WeaponName { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [StringLength(180, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 4)]
        [Display(Name = "Description")]
        public string WeaponDescription { get; set; }

        [Required]
        [Display(Name = "Damage")]
        public int WeaponDamage { get; set; }

        [Required]
        [Display(Name = "Chance to Hit")]
        public int WeaponHit { get; set; }

        [Required]
        [Display(Name = "Critical Strike Chance")]
        public int WeaponCrit { get; set; }

        // eventually need to save image ._.
    }
}