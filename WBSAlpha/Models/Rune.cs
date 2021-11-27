using System.ComponentModel.DataAnnotations;
/*
Modified By:    Quinn Helm
Date:           18-09-2021
*/
namespace WBSAlpha.Models
{
    public class Rune
    {
        [Key]
        [Display(Name = "Rune ID")]
        public int RuneID { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [StringLength(45, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 4)]
        [Display(Name = "Rune Name")]
        public string RuneName { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [StringLength(180, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 4)]
        [Display(Name = "Description")]
        public string RuneDescription { get; set; }

        // eventually need to save image ._.
    }
}