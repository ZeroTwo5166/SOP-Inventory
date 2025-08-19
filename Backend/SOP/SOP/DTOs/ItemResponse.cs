using SOP.Entities;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SOP.DTOs
{
    public class ItemResponse
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public int ItemGroupId { get; set; }
        public string SerialNumber { get; set; } = string.Empty;

        public ItemItemGroupResponse ItemGroup { get; set; }
        public ItemRoomResponse Room { get; set; }
        public List<ItemStatusHistoryResponse> StatusHistories { get; set; }
        public ItemLoanResponse Loan { get; set; }

    }

    public class ItemItemGroupResponse
    {
        public int Id { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public int ItemTypeId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string Manufacturer { get; set; } = string.Empty;
        public string WarrantyPeriod { get; set; } = string.Empty;
        public ItemItemTypeResponse ItemType { get; set; }
    }

    public class ItemItemTypeResponse
    {
        public int Id { get; set; }
        public string TypeName { get; set; } = string.Empty;
    }

    public class ItemRoomResponse
    {
        public int Id { get; set; }
        public int BuildingId { get; set; }
        public int RoomNumber { get; set; }
        public ItemBuildingResponse Building { get; set; }
}

    public class ItemStatusHistoryResponse
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public int StatusId { get; set; }
        public DateTime StatusUpdateDate { get; set; }
        public string Note { get; set; } = string.Empty;
        public ItemStatusResponse Status { get; set; }

    }

    public class ItemStatusResponse
    {
        public int Id { get; set; }
        public string TypeName { get; set; }
    }

    public class ItemLoanResponse
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int ItemId { get; set; }

        public DateTime LoanDate { get; set; }

        public DateTime ReturnDate { get; set; }
    }

    public class ItemBuildingResponse
{
        public int Id { get; set; }

        public string BuildingName { get; set; } = string.Empty;

        public int AddressId { get; set; }

        public ItemAddressResponse buildingAddress { get; set; }
    }

    public class ItemAddressResponse
    {
        public int Id { get; set; } // Added Address Id

        public int ZipCode { get; set; }

        public string Region { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string Road { get; set; } = string.Empty;
    }
}
