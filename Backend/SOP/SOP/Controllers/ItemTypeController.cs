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
    public class ItemTypeController : ControllerBase
    {
        private readonly IItemTypeRepository _itemTypeRepository;

        public ItemTypeController(IItemTypeRepository itemTypeRepository)
        {
            _itemTypeRepository = itemTypeRepository;
        }

        private static string? SafeDecrypt(string? v)
        {
            if (string.IsNullOrWhiteSpace(v)) return v;
            try { return EncryptionHelper.Decrypt(v); } catch { return v; }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                var itemTypes = await _itemTypeRepository.GetAllAsync();

                var itemTypeResponses = itemTypes
                    .Select(itemType => MapItemTypeToItemTypeResponse(itemType))
                    .ToList();

                return Ok(itemTypeResponses);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] ItemTypeRequest itemTypeRequest)
        {
            try
            {
                var newItemType = MapItemTypeRequestToItemType(itemTypeRequest);

                var itemType = await _itemTypeRepository.CreateAsync(newItemType);

                var itemTypeResponse = MapItemTypeToItemTypeResponse(itemType);

                return Ok(itemTypeResponse);
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
                var itemType = await _itemTypeRepository.FindByIdAsync(Id);
                if (itemType == null)
                {
                    return NotFound();
                }

                return Ok(MapItemTypeToItemTypeResponse(itemType));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        // Archive is guarded in the repository: returns NotFound / InUse / Archived
        [Authorize("Admin", "Drift")]
        [HttpDelete]
        [Route("ArchiveById/{Id}")]
        public async Task<IActionResult> ArchiveByIdAsync([FromRoute] int Id, [FromBody] ArchiveNoteRequest archiveNoteRequest)
        {
            try
            {
                var encryptedNote = EncryptionHelper.Encrypt(archiveNoteRequest.ArchiveNote);

                var result = await _itemTypeRepository.ArchiveByIdAsync(Id, encryptedNote);

                switch (result.Status)
                {
                    case ArchiveStatus.NotFound:
                        return NotFound();

                    case ArchiveStatus.InUse:
                        // Block because there are ItemGroups/Items still referencing this ItemType
                        return Conflict(new
                        {
                            message = "Cannot archive ItemType because it is in use by one or more ItemGroups/Items.",
                            code = "IN_USE",
                            blockedBy = new[] { "ItemGroup", "Item" }
                        });

                    case ArchiveStatus.Archived:
                        var entity = result.Entity!;
                        var response = new Archive_ItemTypeResponse
                        {
                            Id = entity.Id,
                            DeleteTime = entity.DeleteTime,
                            TypeName = entity.TypeName,
                            ArchiveNote = SafeDecrypt(entity.ArchiveNote),
                        };
                        return Ok(response);

                    default:
                        return Problem("Unknown archive status.");
                }
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        private static ItemTypeResponse MapItemTypeToItemTypeResponse(ItemType itemType)
        {
            return new ItemTypeResponse
            {
                Id = itemType.Id,
                TypeName = itemType.TypeName
            };
        }

        private static ItemType MapItemTypeRequestToItemType(ItemTypeRequest itemTypeRequest)
        {
            return new ItemType
            {
                TypeName = itemTypeRequest.TypeName,
            };
        }
    }
}
