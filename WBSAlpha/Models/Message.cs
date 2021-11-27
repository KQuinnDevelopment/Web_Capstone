using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
/*
Modified By:    Quinn Helm
Date:           21-11-2021
*/
namespace WBSAlpha.Models
{
    public class Message
    {
        [Key]
        [Display(Name = "Message ID")]
        public int MessageID { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [StringLength(280, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
        [Display(Name = "Contents")]
        public string Content { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Sent At")]
        [DisplayFormat(DataFormatString = "{0:g}")]
        public DateTime Timestamp { get; set; }

        [ForeignKey("Id")]
        [Display(Name = "Sent By User")]
        public string SentFromUser { get; set; }

        [ForeignKey("Id")]
        [Display(Name = "Sent To User")]
        public string SentToUser { get; set; }

        [Display(Name = "Chatroom ID")]
        public int ChatID { get; set; }
    }
}