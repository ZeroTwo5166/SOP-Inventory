using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOP.DTOs;
using SOP.Entities;
using SOP.Repositories;
using System.Net;

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

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                List<Building> building = await _buildingRepository.GetAllAsync();

                List<BuildingResponse> buildingResponse = building.Select(
                    building => MapBuildingToBuildingResponse(building)).ToList();
                return Ok(buildingResponse);
            }
            catch (Exception ex)
            {

                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] BuildingRequest buildingRequest)
        {
            try
            {
                Building newBuilding = MapBuildingRequestToBuilding(buildingRequest);

                var building = await _buildingRepository.CreateAsync(newBuilding);

                BuildingResponse buildingResponse = MapBuildingToBuildingResponse(building);

                return Ok(buildingResponse);
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
                var building = await _buildingRepository.FindByIdAsync(Id);
                if (building == null)
                {
                    return NotFound(); 
                }

                return Ok(MapBuildingToBuildingResponse(building));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPut]
        [Route("{Id}")]
        public async Task<IActionResult> UpdateByIdAsync([FromRoute] int Id, [FromBody] BuildingRequest buildingRequest)
        {
            try
            {
                var updateBuilding = MapBuildingRequestToBuilding(buildingRequest);

                var building = await _buildingRepository.UpdateByIdAsync(Id, updateBuilding);

                if (building == null)
                {
                    return NotFound(); 
                }

                return Ok(MapBuildingToBuildingResponse(building));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin")]
        [HttpDelete]
        [Route("{Id}")]
        public async Task<IActionResult> DeleteByIdAsync([FromRoute] int Id)
        {
            try
            {
                var building = await _buildingRepository.DeleteByIdAsync(Id);
                if (building == null)
                {
                    return NotFound(); 
                }

                return Ok(MapBuildingToBuildingResponse(building));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        private static BuildingResponse MapBuildingToBuildingResponse(Building building)
        {
            BuildingResponse response = new BuildingResponse
            {
                Id = building.Id,
                BuildingName = building.BuildingName,
                ZipCode = building.Address.ZipCode,
            };

            if (building.Address != null)
            {
                response.BuildingAddress = new BuildingAddressResponse
                {
                    Id = building.Address.Id,
                    ZipCode = building.Address.ZipCode,
                    City = building.Address.City,
                    Region = building.Address.Region,
                    Road = building.Address.Road,
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
