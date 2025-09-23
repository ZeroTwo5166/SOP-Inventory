using Microsoft.AspNetCore.Mvc;
using SOP.DTOs;
using SOP.Entities;
using SOP.Repositories;

namespace SOP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomController : ControllerBase
    {
        private readonly IRoomRepository _roomRepository;

        public RoomController(IRoomRepository roomRepository)
        {
            _roomRepository = roomRepository;
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                List<Room> room = await _roomRepository.GetAllAsync();

                List<RoomResponse> roomResponses = room.Select(
                    room => MapRoomToRoomResponse(room)).ToList();
                return Ok(roomResponses);
            }
            catch (Exception ex)
            {

                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] RoomRequest roomRequest)
        {
            try
            {
                Room newRoom = MapRoomRequestToRoom(roomRequest);

                var room = await _roomRepository.CreateAsync(newRoom);

                RoomResponse roomResponse = MapRoomToRoomResponse(room);

                return Ok(roomResponse);
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
                var room = await _roomRepository.FindByIdAsync(Id);
                if (room == null)
                {
                    return NotFound(); 
                }

                return Ok(MapRoomToRoomResponse(room));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPut]
        [Route("{Id}")]
        public async Task<IActionResult> UpdateByIdAsync([FromRoute] int Id, [FromBody] RoomRequest roomRequest)
        {
            try
            {
                var updateRoom = MapRoomRequestToRoom(roomRequest);

                var room = await _roomRepository.UpdateByIdAsync(Id, updateRoom);

                if (room == null)
                {
                    return NotFound();
                }

                return Ok(MapRoomToRoomResponse(room));
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
                var room = await _roomRepository.DeleteByIdAsync(Id);
                if (room == null)
                {
                    return NotFound();
                }

                return Ok(MapRoomToRoomResponse(room));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        private static RoomResponse MapRoomToRoomResponse(Room room)
        {
            RoomResponse response = new RoomResponse
            {
                Id = room.Id,
                BuildingId = room.BuildingId,
                RoomNumber = room.RoomNumber,
            };

            if (room.Building != null)
            {
                response.Building = new BuildingRoomResponse
                {
                    Id = room.Building.Id,
                    BuildingName = room.Building.BuildingName,
                    AddressId = room.Building.AddressId,
                };

                if (room.Building.Address != null)
                {
                    response.Building.buildingAddress = new RoomAddressResponse
                    {
                        ZipCode = room.Building.Address.ZipCode,
                        Region = room.Building.Address.Region,
                        City = room.Building.Address.City,
                        Road = room.Building.Address.Road,
                    };
                }
            }

            return response;
        }

        private static Room MapRoomRequestToRoom(RoomRequest roomRequest)
        {
            return new Room
            {
                BuildingId = roomRequest.BuildingId,
                RoomNumber = roomRequest.RoomNumber,
            };
        }
    }
}
