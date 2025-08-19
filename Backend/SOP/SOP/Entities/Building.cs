using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Emit;

namespace SOP.Entities
{
    public class Building
    {
        [Key]
        public int Id { get; set; }

        [Column(TypeName = "nvarchar(255)")]
        public string BuildingName { get; set; }

        
        public int AddressId { get; set; }

        [ForeignKey("AddressId")]
        public Address Address { get; set; }
    }
}
