namespace SOP.DTOs
{
    public class ItemRequest
    {
        [Required]
        public int RoomId { get; set; }

        [Required]
        public int ItemGroupId { get; set; }

        [Required]
        [StringLength(255, ErrorMessage = "The SerialNumber cannot be longer than 255 chars")]
        public string SerialNumber { get; set; }

        public string? ItemImageUrl { get; set; }

        public string? ItemInfo { get; set; }

    }
}
