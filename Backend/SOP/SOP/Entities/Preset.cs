namespace SOP.Entities
{
    public class Preset
    {
        [Key]
        public int Id { get; set; }

        [Column(TypeName = "nvarchar(255)")]
        public string Name { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string Data { get; set; }


        // Navigation property for reverse relation
        public ICollection<ItemType> ItemTypes { get; set; }

    }
}
