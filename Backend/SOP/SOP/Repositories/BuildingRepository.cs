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
        Task<Building> DeleteByIdAsync(int id);
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

        public async Task<Building> DeleteByIdAsync(int buildingId)
        {
            var building = await FindByIdAsync(buildingId);
            if (building != null)
            {
                _context.Building.Remove(building);
                await _context.SaveChangesAsync();
            }
            return building;
        }
    }
}
