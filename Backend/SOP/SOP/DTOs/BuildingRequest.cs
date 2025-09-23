namespace SOP.DTOs
{
    public class BuildingRequest
    {
        [Required]
        [StringLength(255, ErrorMessage = "BuildingName must not be longer than 255 letters")]
        public string BuildingName { get; set; } = string.Empty;
        [Required]
        public int AddressId { get; set; }
    }
}
