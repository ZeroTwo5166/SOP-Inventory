using Microsoft.AspNetCore.Authorization;
using SOP.Archive.DTOs;
using SOP.Entities;

namespace SOP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemController : ControllerBase
    {
        private readonly IItemRepository _itemRepository;

        public ItemController(IItemRepository itemRepository)
        {
            _itemRepository = itemRepository;
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                var items = await _itemRepository.GetAllAsync();

                List<ItemResponse> itemResponses = items.Select(
                    item => MapItemToItemResponse(item)).ToList();

                return Ok(itemResponses);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] ItemRequest itemRequest)
        {
            try
            {
                Item newItem = MapItemRequestToItem(itemRequest);

                var item = await _itemRepository.CreateAsync(newItem);

                ItemResponse itemResponse = MapItemToItemResponse(item);

                return Ok(itemResponse);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet]
        [Route("{Id}")]
        public async Task<IActionResult> FindByIdAsync([FromRoute] int Id)
        {
            try
            {
                var item = await _itemRepository.FindByIdAsync(Id);
                if (item == null)
                {
                    return NotFound();
                }

                return Ok(MapItemToItemResponse(item));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPut]
        [Route("{Id}")]
        public async Task<IActionResult> UpdateByIdAsync([FromRoute] int Id, [FromBody] ItemRequest itemRequest)
        {
            try
            {
                var updateItem = MapItemRequestToItem(itemRequest);

                var item = await _itemRepository.UpdateByIdAsync(Id, updateItem);

                if (item == null)
                {
                    return NotFound();
                }

                return Ok(MapItemToItemResponse(item));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Drift")]
        [HttpDelete]
        [Route("ArchiveById/{Id}")]
        public async Task<IActionResult> ArchiveByIdAsync([FromRoute] int Id, [FromBody] ArchiveNoteRequest archiveNoteRequest)
        {
            try
            {
                string archiveNote = archiveNoteRequest.ArchiveNote;
                var item = await _itemRepository.ArchiveByIdAsync(Id, archiveNote);
                if (item == null)
                {
                    return NotFound();
                }

                Archive_ItemResponse response = new Archive_ItemResponse
                {
                    Id = item.Id,
                    DeleteTime = item.DeleteTime,
                    ItemGroupId = item.ItemGroupId,
                    RoomId = item.RoomId,
                    SerialNumber = item.SerialNumber,
                    ArchiveNote = item.ArchiveNote,
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        private ItemResponse MapItemToItemResponse(Item item)
        {
            ItemResponse response = new ItemResponse
            {
                Id = item.Id,
                RoomId = item.RoomId,
                ItemGroupId = item.ItemGroupId,
                SerialNumber = item.SerialNumber,
            };
            if (item.ItemGroup != null)
            {
                response.ItemGroup = new ItemItemGroupResponse
                {
                    Id = item.ItemGroup.Id,
                    ModelName = item.ItemGroup.ModelName,
                    ItemTypeId = item.ItemGroup.ItemTypeId,
                    Price = item.ItemGroup.Price,
                    Quantity = item.ItemGroup.Quantity,
                    Manufacturer = item.ItemGroup.Manufacturer,
                    WarrantyPeriod = item.ItemGroup.WarrantyPeriod,
                };
                if (item.ItemGroup.ItemType != null)
                {
                    response.ItemGroup.ItemType = new ItemItemTypeResponse
                    {
                        Id = item.ItemGroup.ItemType.Id,
                        TypeName = item.ItemGroup.ItemType.TypeName,
                    };
                }
            }
            if (item.Room != null)
            {
                response.Room = new ItemRoomResponse
                {
                    Id = item.Room.Id,
                    BuildingId = item.Room.BuildingId,
                    RoomNumber = item.Room.RoomNumber,
                };

                if (item.Room.Building != null)
                {
                    response.Room.Building = new ItemBuildingResponse
                    {
                        Id = item.Room.Building.Id,
                        AddressId = item.Room.Building.AddressId,
                        BuildingName = item.Room.Building.BuildingName,
                    };

                    if (item.Room.Building.Address != null)
                    {
                        response.Room.Building.buildingAddress = new ItemAddressResponse
                        {
                            Id = item.Room.Building.Address.Id, // Added Address Id
                            ZipCode = item.Room.Building.Address.ZipCode,
                            Road = item.Room.Building.Address.Road,
                            Region = item.Room.Building.Address.Region,
                            City = item.Room.Building.Address.City,
                        };
                    }
                }
            }
            if (item.StatusHistories != null)
            {
                response.StatusHistories = item.StatusHistories.Select(statusHistory =>
                {
                    var statusHistoryResponse = new ItemStatusHistoryResponse
                    {
                        Id = statusHistory.Id,
                        ItemId = statusHistory.ItemId,
                        StatusId = statusHistory.StatusId,
                        StatusUpdateDate = statusHistory.StatusUpdateDate,
                        Note = statusHistory.Note,
                    };
                    if (statusHistory.Status != null)
                    {
                        statusHistoryResponse.Status = new ItemStatusResponse
                        {
                            Id = statusHistory.Status.Id,
                            TypeName = statusHistory.Status.Name,
                        };
                    }
                    return statusHistoryResponse;
                }).ToList();
            }
            if (item.Loan != null)
            {
                response.Loan = new ItemLoanResponse
                {
                    Id = item.Loan.Id,
                    LoanDate = item.Loan.LoanDate,
                    ReturnDate = item.Loan.ReturnDate,
                    ItemId = item.Loan.ItemId,
                    UserId = item.Loan.UserId,
                };
            }

            return response;
        }

        private Item MapItemRequestToItem(ItemRequest itemRequest)
        {
            return new Item
            {
                RoomId = itemRequest.RoomId,
                ItemGroupId = itemRequest.ItemGroupId,
                SerialNumber = itemRequest.SerialNumber,
            };
        }
    }
}