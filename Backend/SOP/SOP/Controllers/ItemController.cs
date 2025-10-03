using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOP.Archive.DTOs;
using SOP.DTOs;
using SOP.Encryption;
using SOP.Entities;
using SOP.Repositories;

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

        // Safe decrypt: if value isn't ciphertext yet, just return it as-is
        private static string? SafeDecrypt(string? v)
        {
            if (string.IsNullOrWhiteSpace(v)) return v;
            try { return EncryptionHelper.Decrypt(v); }
            catch { return v; }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            var items = await _itemRepository.GetAllAsync();
            var itemResponses = items.Select(MapItemToItemResponse).ToList();
            return Ok(itemResponses);
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] ItemRequest itemRequest)
        {
            var newItem = MapItemRequestToItem(itemRequest);

            // Encrypt sensitive fields BEFORE save
            newItem.SerialNumber = EncryptionHelper.Encrypt(newItem.SerialNumber);

            var item = await _itemRepository.CreateAsync(newItem);
            var itemResponse = MapItemToItemResponse(item);

            return Ok(itemResponse);
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet("{Id}")]
        public async Task<IActionResult> FindByIdAsync([FromRoute] int Id)
        {
            var item = await _itemRepository.FindByIdAsync(Id);
            if (item == null) return NotFound();

            return Ok(MapItemToItemResponse(item));
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPut("{Id}")]
        public async Task<IActionResult> UpdateByIdAsync([FromRoute] int Id, [FromBody] ItemRequest itemRequest)
        {
            var updateItem = MapItemRequestToItem(itemRequest);

            // Encrypt before saving
            updateItem.SerialNumber = EncryptionHelper.Encrypt(updateItem.SerialNumber);

            var item = await _itemRepository.UpdateByIdAsync(Id, updateItem);
            if (item == null) return NotFound();

            return Ok(MapItemToItemResponse(item));
        }

        // Hard delete with guard from repository (409 if in use)
        [Authorize("Admin")]
        [HttpDelete("{Id}")]
        public async Task<IActionResult> DeleteByIdAsync([FromRoute] int Id)
        {
            var result = await _itemRepository.DeleteByIdAsync(Id);

            return result.Status switch
            {
                DeleteStatus.NotFound => NotFound(),
                DeleteStatus.InUse => Conflict(new
                {
                    code = "ITEM_IN_USE",
                    message = "Item has an active loan and cannot be deleted."
                }),
                DeleteStatus.Deleted => Ok(MapItemToItemResponse(result.Entity!))
            };
        }

        // Archive with guard from repository (409 if in use)
        [Authorize("Admin", "Drift")]
        [HttpDelete("ArchiveById/{Id}")]
        public async Task<IActionResult> ArchiveByIdAsync([FromRoute] int Id, [FromBody] ArchiveNoteRequest archiveNoteRequest)
        {
            // Encrypt archive note before persisting
            var encryptedNote = EncryptionHelper.Encrypt(archiveNoteRequest.ArchiveNote);

            var result = await _itemRepository.ArchiveByIdAsync(Id, encryptedNote);

            return result.Status switch
            {
                ArchiveStatus.NotFound => NotFound(),
                ArchiveStatus.InUse => Conflict(new
                {
                    code = "ITEM_IN_USE",
                    message = "Item has an active loan and cannot be archived."
                }),
                ArchiveStatus.Archived => Ok(new Archive_ItemResponse
                {
                    Id = result.Entity!.Id,
                    DeleteTime = result.Entity.DeleteTime,
                    ItemGroupId = result.Entity.ItemGroupId,
                    RoomId = result.Entity.RoomId,
                    SerialNumber = SafeDecrypt(result.Entity.SerialNumber),
                    ArchiveNote = SafeDecrypt(result.Entity.ArchiveNote),
                })
            };
        }

        private ItemResponse MapItemToItemResponse(Item item)
        {
            var response = new ItemResponse
            {
                Id = item.Id,
                RoomId = item.RoomId,
                ItemGroupId = item.ItemGroupId,
                SerialNumber = SafeDecrypt(item.SerialNumber),
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
                        BuildingName = SafeDecrypt(item.Room.Building.BuildingName),
                    };

                    if (item.Room.Building.Address != null)
                    {
                        response.Room.Building.buildingAddress = new ItemAddressResponse
                        {
                            Id = item.Room.Building.Address.Id,
                            ZipCode = item.Room.Building.Address.ZipCode,
                            Road = SafeDecrypt(item.Room.Building.Address.Road),
                            Region = SafeDecrypt(item.Room.Building.Address.Region),
                            City = SafeDecrypt(item.Room.Building.Address.City),
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
