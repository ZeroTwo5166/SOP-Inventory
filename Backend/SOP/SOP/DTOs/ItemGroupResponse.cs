using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SOP.DTOs
{
    public class ItemGroupResponse
    {
        public int Id { get; set; }
        public int ItemTypeId { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string Manufacturer { get; set; } = string.Empty;
        public string WarrantyPeriod { get; set; } = string.Empty;

        public ItemGroupItemTypeResponse ItemType { get; set; }
    }

    public class ItemGroupItemTypeResponse
    {
        public int Id { get; set; }
        public string TypeName { get; set; } = string.Empty;

        public int? PresetId { get; set; }
    }
}
