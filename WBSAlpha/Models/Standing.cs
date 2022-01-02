using System;
using System.ComponentModel.DataAnnotations;
/*
Modified By:    Quinn Helm
Date:           03-12-2021
*/
namespace WBSAlpha.Models
{
    public class Standing
    {
        [Key]
        [Display(Name = "Standing ID")]
        public int StandingID { get; set; }

        [Display(Name = "Recent Kicks")]
        public int KickCount { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Kick Ends On")]
        [DisplayFormat(DataFormatString = "{0:g}")]
        public DateTime? KickEnds { get; set; }

        [Display(Name = "Total Times Kicked")]
        public int KickTotal { get; set; }

        [Display(Name = "Report ID")]
        public int Justification { get; set; }

        [Display(Name = "Recent Bans")]
        public int BanCount { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Ban Ends On")]
        [DisplayFormat(DataFormatString = "{0:g}")]
        public DateTime? BanEnds { get; set; }

        [Display(Name = "Total Times Banned")]
        public int BanTotal { get; set; }
    }
}