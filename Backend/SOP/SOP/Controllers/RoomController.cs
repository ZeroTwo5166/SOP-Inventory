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
    public class RoomController : ControllerBase
    {
        private readonly IRoomRepository _roomRepository;

        public RoomController(IRoomRepository roomRepository)
        {
            _roomRepository = roomRepository;
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
            var rooms = await _roomRepository.GetAllAsync();
            var dto = rooms.Select(MapRoomToRoomResponse).ToList();
            return Ok(dto);
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] RoomRequest roomRequest)
        {
            var newRoom = MapRoomRequestToRoom(roomRequest);
            var saved = await _roomRepository.CreateAsync(newRoom);
            return Ok(MapRoomToRoomResponse(saved));
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet("{Id}")]
        public async Task<IActionResult> FindByIdAsync([FromRoute] int Id)
        {
            var room = await _roomRepository.FindByIdAsync(Id);
            if (room is null) return NotFound();
            return Ok(MapRoomToRoomResponse(room));
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPut("{Id}")]
        public async Task<IActionResult> UpdateByIdAsync([FromRoute] int Id, [FromBody] RoomRequest roomRequest)
        {
            var updateRoom = MapRoomRequestToRoom(roomRequest);
            var updated = await _roomRepository.UpdateByIdAsync(Id, updateRoom);
            if (updated is null) return NotFound();
            return Ok(MapRoomToRoomResponse(updated));
        }

        [Authorize("Admin")]
        [HttpDelete("{Id}")]
        public async Task<IActionResult> DeleteByIdAsync([FromRoute] int Id)
        {
            var result = await _roomRepository.DeleteByIdAsync(Id);

            return result.Status switch
            {
                DeleteStatus.NotFound => NotFound(),
                DeleteStatus.InUse => Conflict(new
                {
                    code = "ROOM_IN_USE",
                    message = "Room contains items and cannot be deleted."
                }),
                DeleteStatus.Deleted => Ok(MapRoomToRoomResponse(result.Entity!))
            };
        }

        private static RoomResponse MapRoomToRoomResponse(Room room)
        {
            var response = new RoomResponse
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
                    BuildingName = SafeDecrypt(room.Building.BuildingName),
                    AddressId = room.Building.AddressId,
                };

                if (room.Building.Address != null)
                {
                    response.Building.buildingAddress = new RoomAddressResponse
                    {
                        ZipCode = room.Building.Address.ZipCode,
                        Region = SafeDecrypt(room.Building.Address.Region),
                        City = SafeDecrypt(room.Building.Address.City),
                        Road = SafeDecrypt(room.Building.Address.Road),
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
