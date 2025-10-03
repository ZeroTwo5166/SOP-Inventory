using Microsoft.EntityFrameworkCore;
using SOP.Database;
using SOP.Entities;

namespace SOP.Repositories
{
    public interface IRoomRepository
    {
        Task<List<Room>> GetAllAsync();
        Task<Room> CreateAsync(Room room);
        Task<Room> FindByIdAsync(int id);
        Task<Room> UpdateByIdAsync(int id, Room room);
        Task<DeleteResult<Room>> DeleteByIdAsync(int id);
    }
    public class RoomRepository : IRoomRepository
    {
        private readonly DatabaseContext _context;

        public RoomRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<List<Room>> GetAllAsync()
        {
            return await _context.Room
                .Include(x => x.Building)
                .ThenInclude(x => x.Address)
                .Include(x => x.Items)
                .ToListAsync();
        }

        public async Task<Room> CreateAsync(Room newRoom)
        {
            _context.Room.Add(newRoom);
            await _context.SaveChangesAsync();
            return newRoom;
        }

        public async Task<Room?> FindByIdAsync(int Id)
        {
            return await _context.Room
                .Include(x => x.Building)
                .ThenInclude(x => x.Address)
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == Id);
        }

        public async Task<Room> UpdateByIdAsync(int id, Room newRoom)
        {
            var room = await FindByIdAsync(id);

            if (room != null)
            {
                room.RoomNumber = newRoom.RoomNumber;
                room.BuildingId = newRoom.BuildingId;

                await _context.SaveChangesAsync();

                room = await FindByIdAsync(id);
            }
            return room;
        }

        //public async Task<Room> DeleteByIdAsync(int roomId)
        //{
        //    var room = await FindByIdAsync(roomId);
        //    if (room != null)
        //    {
        //        _context.Room.Remove(room);
        //        await _context.SaveChangesAsync();
        //    }
        //    return room;
        //}

        public async Task<DeleteResult<Room>> DeleteByIdAsync(int id)
        {
            // Load the entity (include bits for nicer error payloads if needed)
            var room = await _context.Room
                .Include(r => r.Building)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room is null)
            {
                return DeleteResult<Room>.NotFound();
            }

            // Guard: block delete if room still has items
            var hasItems = await _context.Item.AnyAsync(i => i.RoomId == id);
            if (hasItems)
            {
                return DeleteResult<Room>.InUse(room);
            }

            _context.Room.Remove(room);
            await _context.SaveChangesAsync();
            return DeleteResult<Room>.Deleted(room);
        }
    }
}
