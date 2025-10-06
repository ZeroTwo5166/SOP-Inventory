namespace SOP.Entities
{
    public class ItemType
    {
        [Key]
        public int Id { get; set; }

        [Column(TypeName = "nvarchar(255)")]
        public string TypeName { get; set; }

        // Foreign key to Preset
        public int? PresetId { get; set; }


        // Navigation property
        [ForeignKey("PresetId")]
        public Preset? Preset { get; set; }

    }
}
