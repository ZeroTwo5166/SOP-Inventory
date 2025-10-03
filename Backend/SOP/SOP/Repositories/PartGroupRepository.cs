using Microsoft.EntityFrameworkCore;
using SOP.Database;
using SOP.Entities;

namespace SOP.Repositories
{
    public interface IPartGroupRepository
    {
        Task<PartGroup> CreateAsync(PartGroup newPartGroup);
        Task<PartGroup?> UpdateByIdAsync(int partGroupId, PartGroup updatePartGroup);
        Task<PartGroup?> FindByIdAsync(int partGroupId);
        Task<List<PartGroup>> GetAllAsync();

        // new guarded delete
        Task<DeleteResult<PartGroup>> DeleteByIdAsync(int partGroupId);
    }
    public class PartGroupRepository : IPartGroupRepository
    {
        private readonly DatabaseContext _context;

        // Initializes the repository with the database context for accessing data
        public PartGroupRepository(DatabaseContext context)
        {
            _context = context;
        }

        // Adds a new PartGroup, saves changes, retrieves, and returns it
        public async Task<PartGroup> CreateAsync(PartGroup newPartGroup)
        {
            _context.PartGroup.Add(newPartGroup);
            await _context.SaveChangesAsync();
            newPartGroup = await FindByIdAsync(newPartGroup.Id);
            return newPartGroup;
        }

        // Please refer to the class diagram or ER diagram for entity relationships
        // Finds a PartGroup by ID, including related entities and returns it
        public async Task<PartGroup?> FindByIdAsync(int partGroupId)
        {
            return await _context.PartGroup
                .Include(pg => pg.PartType)
                .FirstOrDefaultAsync(pg => pg.Id == partGroupId);
        }

        // Please refer to the class diagram or ER diagram for entity relationships
        // Retrieves all PartGroups, including related entities and returns them
        public async Task<List<PartGroup>> GetAllAsync()
        {
            return await _context.PartGroup
                .Include(pg => pg.PartType)
                .ToListAsync();
        }

        // Updates a PartGroup by ID and returns the updated entity. 
        public async Task<PartGroup?> UpdateByIdAsync(int partGroupId, PartGroup updatePartGroup)
        {
            var partGroup = await FindByIdAsync(partGroupId);
            if (partGroup != null)
            {
                partGroup.PartName = updatePartGroup.PartName;
                partGroup.Quantity = updatePartGroup.Quantity;
                partGroup.Price = updatePartGroup.Price;
                partGroup.Manufacturer = updatePartGroup.Manufacturer;
                partGroup.WarrantyPeriod = updatePartGroup.WarrantyPeriod;
                partGroup.ReleaseDate = updatePartGroup.ReleaseDate;
                partGroup.PartTypeId = updatePartGroup.PartTypeId;

                await _context.SaveChangesAsync();

                partGroup = await FindByIdAsync(partGroupId);
            }
            return partGroup;
        }

        public async Task<DeleteResult<PartGroup>> DeleteByIdAsync(int partGroupId)
        {
            var partGroup = await _context.PartGroup
                .Include(pg => pg.PartType)
                .FirstOrDefaultAsync(pg => pg.Id == partGroupId);

            if (partGroup is null)
            {
                return DeleteResult<PartGroup>.NotFound();
            }

            // In-use check
            var inUse = await _context.ComputerPart.AnyAsync(cp => cp.PartGroupId == partGroupId);
            if (inUse)
            {
                return DeleteResult<PartGroup>.InUse(partGroup);
            }

            _context.PartGroup.Remove(partGroup);
            await _context.SaveChangesAsync();
            return DeleteResult<PartGroup>.Deleted(partGroup);
        }
    }
}
