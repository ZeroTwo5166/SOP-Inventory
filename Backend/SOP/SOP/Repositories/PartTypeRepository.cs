using Microsoft.EntityFrameworkCore;
using SOP.Database;
using SOP.Entities;

namespace SOP.Repositories
{
    public interface IPartTypeRepository
    {
        Task<PartType> CreateAsync(PartType newPartType);
        Task<PartType?> UpdateByIdAsync(int partTypeId, PartType updatePartType);
        Task<PartType?> FindByIdAsync(int partTypeId);
        Task<List<PartType>> GetAllAsync();

        // NEW: guarded delete result
        Task<DeleteResult<PartType>> DeleteByIdAsync(int partTypeId);
    }

    public class PartTypeRepository : IPartTypeRepository
    {
        private readonly DatabaseContext _context;

        public PartTypeRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<PartType> CreateAsync(PartType newPartType)
        {
            _context.PartType.Add(newPartType);
            await _context.SaveChangesAsync();
            newPartType = await FindByIdAsync(newPartType.Id);
            return newPartType;
        }

        public async Task<PartType?> FindByIdAsync(int partTypeId)
        {
            return await _context.PartType.FindAsync(partTypeId);
        }

        public async Task<List<PartType>> GetAllAsync()
        {
            return await _context.PartType.ToListAsync();
        }

        public async Task<PartType?> UpdateByIdAsync(int partTypeId, PartType updatePartType)
        {
            var partType = await FindByIdAsync(partTypeId);
            if (partType != null)
            {
                partType.PartTypeName = updatePartType.PartTypeName;

                await _context.SaveChangesAsync();

                partType = await FindByIdAsync(partTypeId);
            }
            return partType;
        }

        // NEW: Guarded delete — block if any PartGroup references this PartType
        public async Task<DeleteResult<PartType>> DeleteByIdAsync(int partTypeId)
        {
            var partType = await FindByIdAsync(partTypeId);
            if (partType == null)
            {
                return DeleteResult<PartType>.NotFound();
            }

            // "In use" check: any PartGroup that points to this PartType?
            var inUse = await _context.PartGroup.AnyAsync(pg => pg.PartTypeId == partTypeId);
            if (inUse)
            {
                // Return an InUse result so the controller can map to HTTP 409
                return DeleteResult<PartType>.InUse(null);
            }

            _context.PartType.Remove(partType);
            await _context.SaveChangesAsync();

            return DeleteResult<PartType>.Deleted(partType);
        }
    }
}
