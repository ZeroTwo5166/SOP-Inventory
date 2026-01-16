using System.ComponentModel.DataAnnotations;

namespace SOP.DTOs
{
    public class RequestRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public string RecipientEmail { get; set; }

        [Required]
        public string Item { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public string Status { get; set; }
    }
}

