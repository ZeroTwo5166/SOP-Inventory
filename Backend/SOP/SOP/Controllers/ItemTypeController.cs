using Microsoft.AspNetCore.Authorization;
using SOP.Archive.DTOs;
using SOP.Entities;

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

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                var itemTypes = await _itemTypeRepository.GetAllAsync();

                List<ItemTypeResponse> itemTypeResponses = itemTypes.Select(
                    itemType => MapItemTypeToItemTypeResponse(itemType)).ToList();

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
                ItemType newItemType = MapItemTypeRequestToItemType(itemTypeRequest);

                var itemType = await _itemTypeRepository.CreateAsync(newItemType);

                ItemTypeResponse itemTypeResponse = MapItemTypeToItemTypeResponse(itemType);

                return Ok(itemTypeResponse);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        //[Authorize("Admin", "Instruktør", "Drift")]
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

        [Authorize("Admin", "Drift")]
        [HttpDelete]
        [Route("ArchiveById/{Id}")]
        public async Task<IActionResult> ArchiveByIdAsync([FromRoute] int Id, [FromBody] ArchiveNoteRequest archiveNoteRequest)
        {
            try
            {
                string archiveNote = archiveNoteRequest.ArchiveNote;
                var itemType = await _itemTypeRepository.ArchiveByIdAsync(Id, archiveNote);
                if (itemType == null)
                {
                    return NotFound();
                }

                Archive_ItemTypeResponse response = new Archive_ItemTypeResponse
                {
                    Id = itemType.Id,
                    DeleteTime = itemType.DeleteTime,
                    TypeName = itemType.TypeName,
                    ArchiveNote = itemType.ArchiveNote,
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }


        private ItemTypeResponse MapItemTypeToItemTypeResponse(ItemType itemType)
        {
            return new ItemTypeResponse
            {
                Id = itemType.Id,
                TypeName = itemType.TypeName,
                PresetId = itemType.PresetId
            };
        }


        private ItemType MapItemTypeRequestToItemType(ItemTypeRequest itemTypeRequest)
        {
            return new ItemType
            {
                TypeName = itemTypeRequest.TypeName,
            };
        }
    }
}