using Azure;
using Microsoft.AspNetCore.Mvc;
using SOP.Entities;
using SOP.Repositories;

namespace SOP.Controllers
{
    //create the route for our angular to call
    [Route("api/[controller]")]
    [ApiController]
    public class PartGroupController : ControllerBase
    {
        // Injecting the IRoomRepository interface and storing it in a private readonly variable
        // This allows access to the room repository methods throughout the class
        private readonly IPartGroupRepository _partGroupRepository;

        // Initializes the controller with the address repository
        public PartGroupController(IPartGroupRepository partGroupRepository)
        {
            // Assigning the repository to the private variable
            _partGroupRepository = partGroupRepository;
        }


        private static string? SafeDecrypt(string? v)
        {
            if (string.IsNullOrWhiteSpace(v)) return v;
            try { return EncryptionHelper.Decrypt(v); }
            catch { return v; }
        }

        [Authorize("Admin", "Instruktør")]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            //We use a try methode to get an answer if anything goes wrong,
            //we can print a message and not let the user completle blind over the problem
            try
            {
                //We are using the GetAllAsync methode from the Interface and set it into a var
                var partGroups = await _partGroupRepository.GetAllAsync();

                //We are selecting and mapping the statusHistories we got from the database and making it into a list of partGroup responses
                List<PartGroupResponse> partGroupResponses = partGroups.Select(
                    partGroup => MapPartGroupToPartGroupResponse(partGroup)).ToList();

                return Ok(partGroupResponses);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør")]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] PartGroupRequest partGroupRequest)
        {
            try
            {
                PartGroup newPartGroup = MapPartGroupRequestToPartGroup(partGroupRequest);

                newPartGroup.PartName = EncryptionHelper.Encrypt(newPartGroup.PartName);
                newPartGroup.Manufacturer = EncryptionHelper.Encrypt(newPartGroup.Manufacturer);
                newPartGroup.WarrantyPeriod = EncryptionHelper.Encrypt(newPartGroup.WarrantyPeriod);

                var partGroup = await _partGroupRepository.CreateAsync(newPartGroup);

                PartGroupResponse partGroupResponse = MapPartGroupToPartGroupResponse(partGroup);

                return Ok(partGroupResponse);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør")]
        [HttpGet]
        [Route("{partGroupId}")]
        public async Task<IActionResult> FindByIdAsync([FromRoute] int partGroupId)
        {
            try
            {
                var partGroup = await _partGroupRepository.FindByIdAsync(partGroupId);
                if (partGroup == null)
                {
                    return NotFound(); //Status Code 404
                }

                return Ok(MapPartGroupToPartGroupResponse(partGroup));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør")]
        [HttpPut]
        [Route("{partGroupId}")]
        public async Task<IActionResult> UpdateByIdAsync([FromRoute] int partGroupId, [FromBody] PartGroupRequest partGroupRequest)
        {
            try
            {
                var updatePartGroup = MapPartGroupRequestToPartGroup(partGroupRequest);

                updatePartGroup.PartName = EncryptionHelper.Encrypt(updatePartGroup.PartName);
                updatePartGroup.Manufacturer = EncryptionHelper.Encrypt(updatePartGroup.Manufacturer);
                updatePartGroup.WarrantyPeriod = EncryptionHelper.Encrypt(updatePartGroup.WarrantyPeriod);


                var partGroup = await _partGroupRepository.UpdateByIdAsync(partGroupId, updatePartGroup);

                if (partGroup == null)
                {
                    return NotFound(); //Status Code 404
                }

                return Ok(MapPartGroupToPartGroupResponse(partGroup));
            }
            catch (Exception ex)
            {

                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør")]
        [HttpDelete("{partGroupId}")]
        public async Task<IActionResult> DeleteByIdAsync([FromRoute] int partGroupId)
        {
            try
            {
                var result = await _partGroupRepository.DeleteByIdAsync(partGroupId);

                return result.Status switch
                {
                    DeleteStatus.NotFound => NotFound(),

                    DeleteStatus.InUse => Conflict(new
                    {
                        code = "PARTGROUP_IN_USE",
                        message = "Part group has parts attached and cannot be deleted."
                    }),

                    DeleteStatus.Deleted => Ok(MapPartGroupToPartGroupResponse(result.Entity!)),

                    _ => Problem("Unknown delete result.")
                };
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }


        private static PartGroupResponse MapPartGroupToPartGroupResponse(PartGroup partGroup)
        {
            PartGroupResponse response = new PartGroupResponse
            {
                Id = partGroup.Id,
                PartName = SafeDecrypt(partGroup.PartName),
                Price = partGroup.Price,
                Manufacturer = SafeDecrypt(partGroup.Manufacturer),
                WarrantyPeriod = SafeDecrypt(partGroup.WarrantyPeriod),
                ReleaseDate = partGroup.ReleaseDate,
                Quantity = partGroup.Quantity,
                PartTypeId = partGroup.PartTypeId,
            };
            if (partGroup.PartType != null)
            {
                response.PartType = new PartGroupPartTypeResponse
                {
                    Id = partGroup.PartType.Id,
                    PartTypeName = partGroup.PartType.PartTypeName,
                };
            }
            return response;
        }

        private PartGroup MapPartGroupRequestToPartGroup(PartGroupRequest partGroupRequest)
        {
            return new PartGroup
            {
                PartName = partGroupRequest.PartName,
                Price = partGroupRequest.Price,
                Manufacturer = partGroupRequest.Manufacturer,
                WarrantyPeriod= partGroupRequest.WarrantyPeriod,
                ReleaseDate = partGroupRequest.ReleaseDate,
                Quantity = partGroupRequest.Quantity,
                PartTypeId = partGroupRequest.PartTypeId,
            };
        }
    }
}
