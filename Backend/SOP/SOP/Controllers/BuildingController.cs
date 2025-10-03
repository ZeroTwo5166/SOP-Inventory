using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOP.DTOs;
using SOP.Entities;
using SOP.Repositories;
using SOP.Encryption;

namespace SOP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BuildingController : ControllerBase
    {
        private readonly IBuildingRepository _buildingRepository;

        public BuildingController(IBuildingRepository buildingRepository)
        {
            _buildingRepository = buildingRepository;
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
            var buildings = await _buildingRepository.GetAllAsync();
            var dto = buildings.Select(MapBuildingToBuildingResponse).ToList();
            return Ok(dto);
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] BuildingRequest buildingRequest)
        {
            var newBuilding = MapBuildingRequestToBuilding(buildingRequest);

            // Encrypt sensitive string before save
            newBuilding.BuildingName = EncryptionHelper.Encrypt(newBuilding.BuildingName);

            var saved = await _buildingRepository.CreateAsync(newBuilding);
            return Ok(MapBuildingToBuildingResponse(saved));
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet("{Id}")]
        public async Task<IActionResult> FindByIdAsync([FromRoute] int Id)
        {
            var building = await _buildingRepository.FindByIdAsync(Id);
            if (building is null) return NotFound();
            return Ok(MapBuildingToBuildingResponse(building));
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPut("{Id}")]
        public async Task<IActionResult> UpdateByIdAsync([FromRoute] int Id, [FromBody] BuildingRequest buildingRequest)
        {
            var updateBuilding = MapBuildingRequestToBuilding(buildingRequest);

            // Encrypt before save
            updateBuilding.BuildingName = EncryptionHelper.Encrypt(updateBuilding.BuildingName);

            var updated = await _buildingRepository.UpdateByIdAsync(Id, updateBuilding);
            if (updated is null) return NotFound();

            return Ok(MapBuildingToBuildingResponse(updated));
        }

        [Authorize("Admin")]
        [HttpDelete("{Id}")]
        public async Task<IActionResult> DeleteByIdAsync([FromRoute] int Id)
        {
            var result = await _buildingRepository.DeleteByIdAsync(Id);

            return result.Status switch
            {
                DeleteStatus.NotFound => NotFound(),
                DeleteStatus.InUse => Conflict(new
                {
                    code = "BUILDING_IN_USE",
                    message = "Building has rooms and cannot be deleted."
                }),
                DeleteStatus.Deleted => Ok(MapBuildingToBuildingResponse(result.Entity!))
            };
        }

        private static BuildingResponse MapBuildingToBuildingResponse(Building building)
        {
            var response = new BuildingResponse
            {
                Id = building.Id,
                BuildingName = SafeDecrypt(building.BuildingName),
                ZipCode = building.Address?.ZipCode ?? 0,
            };

            if (building.Address != null)
            {
                response.BuildingAddress = new BuildingAddressResponse
                {
                    Id = building.Address.Id,
                    ZipCode = building.Address.ZipCode,
                    City = SafeDecrypt(building.Address.City),
                    Region = SafeDecrypt(building.Address.Region),
                    Road = SafeDecrypt(building.Address.Road),
                };
            }

            return response;
        }

        private static Building MapBuildingRequestToBuilding(BuildingRequest buildingRequest)
        {
            return new Building
            {
                BuildingName = buildingRequest.BuildingName,
                AddressId = buildingRequest.AddressId,
            };
        }
    }
}
