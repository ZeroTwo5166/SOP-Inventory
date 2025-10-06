namespace SOP.DTOs
{
    public class ItemTypeRequest
    {
        [Required]
        [StringLength(255, ErrorMessage = "The TypeName cannot be longer than 255 chars")]
        public string TypeName { get; set; }

        public int? PresetId { get; set; }

    }
}
