using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SOP.Entities
{
    public class Item
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Room.Id")]
        public int RoomId { get; set; }

        [ForeignKey("ItemGroup.Id")]
        public int ItemGroupId { get; set; }

        [Column(TypeName = "nvarchar(255)")]
        public string SerialNumber { get; set; }

        [Column(TypeName = "nvarchar(512)")]
        public string? ItemImageUrl { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? ItemInfo { get; set; }

        public List<StatusHistory> StatusHistories { get; set; }

        public ItemGroup ItemGroup { get; set; }

        public Room Room { get; set; }

        public Loan Loan { get; set; }
    }
}
