namespace SOP.DTOs
{
    public class AddressRequest
    {

        public int? Id { get; set; }   // Nullable → only needed for updates

        [Required]
        public int ZipCode { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Region must not be longer then a hundred letters")]
        public string Region { get; set; } = string.Empty;

        [Required]
        [StringLength(500, ErrorMessage = "City must not be longer then five hundred letters")]
        public string City { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500, ErrorMessage = "Road must not be longer then five hundred letters")]
        public string Road { get; set; } = string.Empty;
    }
}
