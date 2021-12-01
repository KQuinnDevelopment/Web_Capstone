using System.ComponentModel.DataAnnotations;
/*
Modified By:    Quinn Helm
Date:           18-09-2021
*/
namespace WBSAlpha.Models
{
    public class Chatroom
    {
        [Key]
        [Display(Name = "Chat ID")]
        public int ChatID { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [StringLength(45, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 4)]
        [Display(Name = "Chat Name")]
        public string ChatName { get; set; }

        [DataType(DataType.Text)]
        [StringLength(90, ErrorMessage = "The {0} must be at max {1} characters long.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }
    }
}