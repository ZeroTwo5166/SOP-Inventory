using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Emit;

namespace SOP.Entities
{
    public class Address
    {
        [Key]
        public int Id { get; set; }  // New primary key

        public int ZipCode { get; set; }  // No longer the primary key

        [Column(TypeName = "nvarchar(255)")]
        public string Region { get; set; }

        [Column(TypeName = "nvarchar(255)")]
        public string City { get; set; }

        [Column(TypeName = "nvarchar(255)")]
        public string Road { get; set; }
    }
}
