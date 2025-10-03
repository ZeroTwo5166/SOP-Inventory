using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOP.Archive.DTOs;
using SOP.Entities;
using SOP.Repositories;     
using SOP.Encryption;    

namespace SOP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemGroupController : ControllerBase
    {
        private readonly IItemGroupRepository _itemGroupRepository;

        public ItemGroupController(IItemGroupRepository itemGroupRepository)
        {
            _itemGroupRepository = itemGroupRepository;
        }

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
            try
            {
                var itemGroups = await _itemGroupRepository.GetAllAsync();

                var itemGroupResponses = itemGroups
                    .Select(MapItemGroupToItemGroupResponse)
                    .ToList();

                return Ok(itemGroupResponses);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] ItemGroupRequest itemGroupRequest)
        {
            try
            {
                var newItemGroup = MapItemGroupRequestToItemGroup(itemGroupRequest);

                // Encrypt string fields before save
                newItemGroup.ModelName = EncryptionHelper.Encrypt(newItemGroup.ModelName);
                newItemGroup.Manufacturer = EncryptionHelper.Encrypt(newItemGroup.Manufacturer);
                newItemGroup.WarrantyPeriod = EncryptionHelper.Encrypt(newItemGroup.WarrantyPeriod);

                var itemGroup = await _itemGroupRepository.CreateAsync(newItemGroup);
                return Ok(MapItemGroupToItemGroupResponse(itemGroup));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet("{Id}")]
        public async Task<IActionResult> FindByIdAsync([FromRoute] int Id)
        {
            try
            {
                var itemGroup = await _itemGroupRepository.FindByIdAsync(Id);
                if (itemGroup == null) return NotFound();

                return Ok(MapItemGroupToItemGroupResponse(itemGroup));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPut("{Id}")]
        public async Task<IActionResult> UpdateByIdAsync([FromRoute] int Id, [FromBody] ItemGroupRequest itemGroupRequest)
        {
            try
            {
                var updateItemGroup = MapItemGroupRequestToItemGroup(itemGroupRequest);

                // Encrypt string fields BEFORE save
                updateItemGroup.ModelName = EncryptionHelper.Encrypt(updateItemGroup.ModelName);
                updateItemGroup.Manufacturer = EncryptionHelper.Encrypt(updateItemGroup.Manufacturer);
                updateItemGroup.WarrantyPeriod = EncryptionHelper.Encrypt(updateItemGroup.WarrantyPeriod);

                var itemGroup = await _itemGroupRepository.UpdateByIdAsync(Id, updateItemGroup);
                if (itemGroup == null) return NotFound();

                return Ok(MapItemGroupToItemGroupResponse(itemGroup));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        // Guarded archive: 200 on success, 404 if not found, 409 if "in use"
        [Authorize("Admin", "Drift")]
        [HttpDelete("ArchiveById/{Id}")]
        public async Task<IActionResult> ArchiveByIdAsync([FromRoute] int Id, [FromBody] ArchiveNoteRequest archiveNoteRequest)
        {
            try
            {
                var encryptedNote = EncryptionHelper.Encrypt(archiveNoteRequest.ArchiveNote);

                var result = await _itemGroupRepository.ArchiveByIdAsync(Id, encryptedNote);

                switch (result.Status)
                {
                    case ArchiveStatus.NotFound:
                        return NotFound();

                    case ArchiveStatus.InUse:
                        return Conflict(new
                        {
                            code = "ITEMGROUP_IN_USE",
                            message = "Item group cannot be archived because it still has dependent items (and/or items that are in use)."
                        });

                    case ArchiveStatus.Archived:
                        var ig = result.Entity!;
                        var response = new Archive_ItemGroupResponse
                        {
                            Id = ig.Id,
                            DeleteTime = ig.DeleteTime,
                            ItemTypeId = ig.ItemTypeId,
                            ModelName = SafeDecrypt(ig.ModelName),
                            Price = ig.Price,
                            Manufacturer = SafeDecrypt(ig.Manufacturer),
                            WarrantyPeriod = SafeDecrypt(ig.WarrantyPeriod),
                            Quantity = ig.Quantity,
                            ArchiveNote = SafeDecrypt(ig.ArchiveNote),
                        };
                        return Ok(response);

                    default:
                        // Should never happen, but don't 500 the user.
                        return Problem("Unknown archive result.");
                }
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        private static ItemGroupResponse MapItemGroupToItemGroupResponse(ItemGroup itemGroup)
        {
            var response = new ItemGroupResponse
            {
                Id = itemGroup.Id,
                ItemTypeId = itemGroup.ItemTypeId,
                ModelName = SafeDecrypt(itemGroup.ModelName),
                Price = itemGroup.Price,
                Manufacturer = SafeDecrypt(itemGroup.Manufacturer),
                WarrantyPeriod = SafeDecrypt(itemGroup.WarrantyPeriod),
                Quantity = itemGroup.Quantity,
            };

            if (itemGroup.ItemType != null)
            {
                response.ItemType = new ItemGroupItemTypeResponse
                {
                    Id = itemGroup.ItemType.Id,
                    TypeName = itemGroup.ItemType.TypeName
                };
            }

            return response;
        }

        private static ItemGroup MapItemGroupRequestToItemGroup(ItemGroupRequest req)
        {
            return new ItemGroup
            {
                ItemTypeId = req.ItemTypeId,
                ModelName = req.ModelName,
                Price = req.Price,
                Manufacturer = req.Manufacturer,
                WarrantyPeriod = req.WarrantyPeriod,
                Quantity = req.Quantity,
            };
        }
    }
}
