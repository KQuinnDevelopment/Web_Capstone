using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
/*
Modified By:    Quinn Helm
Date:           30-11-2021
*/
namespace WBSAlpha.Models
{
    public class Report
    {
        [Key]
        [Display(Name = "Report ID")]
        public int ReportID { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [StringLength(280, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
        [Display(Name = "Reason for Report")]
        public string Reason { get; set; }

        [Display(Name = "Responded?")]
        public bool RespondedTo { get; set; }

        [ForeignKey("MessageID")]
        [Display(Name = "Message ID")]
        public int MessageID { get; set; } 
    }
}