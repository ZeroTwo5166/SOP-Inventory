using Microsoft.EntityFrameworkCore;
using SOP.Database;
using SOP.Entities;

namespace SOP.Repositories
{

    public interface IBuildingRepository
    {
        Task<List<Building>> GetAllAsync();
        Task<Building> CreateAsync(Building building);
        Task<Building> FindByIdAsync(int id);
        Task<Building> UpdateByIdAsync(int id, Building building);
        Task<DeleteResult<Building>> DeleteByIdAsync(int id);

    }
    public class BuildingRepository : IBuildingRepository
    {
        private readonly DatabaseContext _context;
        public BuildingRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<List<Building>> GetAllAsync()
        {
            return await _context.Building
                .Include(x => x.Address)
                .ToListAsync();
        }

        public async Task<Building> CreateAsync(Building newBuilding)
        {
            _context.Building.Add(newBuilding);
            await _context.SaveChangesAsync();

            var buildingWithAddress = await _context.Building
                .Include(b => b.Address)
                .FirstOrDefaultAsync(b => b.Id == newBuilding.Id);

            return buildingWithAddress;
        }

        public async Task<Building?> FindByIdAsync(int Id)
        {
            return await _context.Building
                .Include(b => b.Address)
                .FirstOrDefaultAsync(b => b.Id == Id);
        }

        public async Task<Building> UpdateByIdAsync(int id, Building newBuilding)
        {
            var building = await FindByIdAsync(id);

            if (building != null)
            {
                building.BuildingName = newBuilding.BuildingName;
                building.AddressId = newBuilding.AddressId;

                await _context.SaveChangesAsync();

                building = await FindByIdAsync(id);
            }
            return building;
        }

        //public async Task<Building> DeleteByIdAsync(int buildingId)
        //{
        //    var building = await FindByIdAsync(buildingId);
        //    if (building != null)
        //    {
        //        _context.Building.Remove(building);
        //        await _context.SaveChangesAsync();
        //    }
        //    return building;
        //}

        public async Task<DeleteResult<Building>> DeleteByIdAsync(int id)
        {
            // Load (with Address for nicer response)
            var building = await _context.Building
                .Include(b => b.Address)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (building is null)
            {
                return DeleteResult<Building>.NotFound();
            }

            // Guard: block delete if the building still has rooms
            var hasRooms = await _context.Room.AnyAsync(r => r.BuildingId == id);
            if (hasRooms)
            {
                return DeleteResult<Building>.InUse(building);

            }

            _context.Building.Remove(building);
            await _context.SaveChangesAsync();
            return DeleteResult<Building>.Deleted(building);

        }
    }
}
