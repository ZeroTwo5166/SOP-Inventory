using Microsoft.AspNetCore.Authorization;
using SOP.Archive.DTOs;
using SOP.Entities;

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

        //[Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                var itemGroups = await _itemGroupRepository.GetAllAsync();

                List<ItemGroupResponse> itemGroupResponses = itemGroups.Select(
                    itemGroup => MapItemGroupToItemGroupResponse(itemGroup)).ToList();

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
                ItemGroup newItemGroup = MapItemGroupRequestToItemGroup(itemGroupRequest);

                var itemGroup = await _itemGroupRepository.CreateAsync(newItemGroup);

                ItemGroupResponse itemGroupResponse = MapItemGroupToItemGroupResponse(itemGroup);

                return Ok(itemGroupResponse);
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
                var itemGroup = await _itemGroupRepository.FindByIdAsync(Id);
                if (itemGroup == null)
                {
                    return NotFound();
                }

                return Ok(MapItemGroupToItemGroupResponse(itemGroup));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPut]
        [Route("{Id}")]
        public async Task<IActionResult> UpdateByIdAsync([FromRoute] int Id, [FromBody] ItemGroupRequest itemGroupRequest)
        {
            try
            {
                var updateItemGroup = MapItemGroupRequestToItemGroup(itemGroupRequest);

                var itemGroup = await _itemGroupRepository.UpdateByIdAsync(Id, updateItemGroup);

                if (itemGroup == null)
                {
                    return NotFound();
                }

                return Ok(MapItemGroupToItemGroupResponse(itemGroup));
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
                var itemGroup = await _itemGroupRepository.ArchiveByIdAsync(Id, archiveNote);
                if (itemGroup == null)
                {
                    return NotFound();
                }

                Archive_ItemGroupResponse response = new Archive_ItemGroupResponse
                {
                    Id = itemGroup.Id,
                    DeleteTime = itemGroup.DeleteTime,
                    ItemTypeId = itemGroup.ItemTypeId,
                    ModelName = itemGroup.ModelName,
                    Price = itemGroup.Price,
                    Manufacturer = itemGroup.Manufacturer,
                    WarrantyPeriod = itemGroup.WarrantyPeriod,
                    Quantity = itemGroup.Quantity,
                    ArchiveNote = itemGroup.ArchiveNote,
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        private ItemGroupResponse MapItemGroupToItemGroupResponse(ItemGroup itemGroup)
        {
            ItemGroupResponse response = new ItemGroupResponse
            {
                Id = itemGroup.Id,
                ItemTypeId = itemGroup.ItemTypeId,
                ModelName = itemGroup.ModelName,
                Price = itemGroup.Price,
                Manufacturer = itemGroup.Manufacturer,
                WarrantyPeriod = itemGroup.WarrantyPeriod,
                Quantity = itemGroup.Quantity,
            };
            if (itemGroup.ItemType != null)
            {
                response.ItemType = new ItemGroupItemTypeResponse
                {
                    Id = itemGroup.ItemType.Id,
                    TypeName = itemGroup.ItemType.TypeName,
                    PresetId = itemGroup?.ItemType?.PresetId
                };
            }
            return response;
        }

        private ItemGroup MapItemGroupRequestToItemGroup(ItemGroupRequest itemGroupRequest)
        {
            return new ItemGroup
            {
                ItemTypeId = itemGroupRequest.ItemTypeId,
                ModelName = itemGroupRequest.ModelName,
                Price = itemGroupRequest.Price,
                Manufacturer = itemGroupRequest.Manufacturer,
                WarrantyPeriod = itemGroupRequest.WarrantyPeriod,
                Quantity = itemGroupRequest.Quantity,
            };
        }
    }
}