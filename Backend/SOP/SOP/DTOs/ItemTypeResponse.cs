namespace SOP.DTOs
{
    public class ItemTypeResponse
    {
        public int Id { get; set; }
        public string TypeName { get; set; } = string.Empty;

        public int? PresetId { get; set; }
    }
}
