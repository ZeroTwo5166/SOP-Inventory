using System.ComponentModel.DataAnnotations.Schema;

namespace SOP.DTOs
{
    public class RoomResponse
    {
        public int Id { get; set; }
        public int BuildingId { get; set; }
        public int RoomNumber { get; set; }

        public BuildingRoomResponse Building { get; set; }
    }

    public class BuildingRoomResponse
    {
        public int Id { get; set; }

        public string BuildingName { get; set; } = string.Empty;

        // FK to Address
        public int AddressId { get; set; }

        // Optional: still expose ZipCode from Address
        public int ZipCode { get; set; }

        public RoomAddressResponse buildingAddress { get; set; }
    }

    public class RoomAddressResponse
    {
        public int Id { get; set; }  // Address PK

        public int ZipCode { get; set; }

        public string Region { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string Road { get; set; } = string.Empty;
    }
}
