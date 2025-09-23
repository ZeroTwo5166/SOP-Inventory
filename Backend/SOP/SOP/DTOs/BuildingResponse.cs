using System.ComponentModel.DataAnnotations.Schema;

namespace SOP.DTOs
{
    public class BuildingResponse
    {
        public int Id { get; set; }

        public string BuildingName { get; set; } = string.Empty;

        public int ZipCode { get; set; }

        public BuildingAddressResponse BuildingAddress { get; set; }
    }

    public class BuildingAddressResponse 
    {
        public int Id { get; set; } // Added Address Id
        public int ZipCode { get; set; }

        public string Region { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string Road { get; set; } = string.Empty;
    }
}
