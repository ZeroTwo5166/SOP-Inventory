using Microsoft.EntityFrameworkCore;
using SOP.Database;
using SOP.Entities;

namespace SOP.Repositories
{
    public interface IComputerPartRepository
    {
        Task<ComputerPart> CreateAsync(ComputerPart newComputerPart);
        Task<ComputerPart?> UpdateByIdAsync(int computerPartId, ComputerPart updateComputerPart);
        Task<DeleteResult<ComputerPart>> DeleteByIdAsync(int computerPartId);
        Task<ComputerPart?> FindByIdAsync(int computerPartId);
        Task<List<ComputerPart>> GetAllAsync();
    }

    public class ComputerPartRepository : IComputerPartRepository
    {
        private readonly DatabaseContext _context;

        public ComputerPartRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<ComputerPart> CreateAsync(ComputerPart newComputerPart)
        {
            _context.ComputerPart.Add(newComputerPart);
            await _context.SaveChangesAsync();
            newComputerPart = await FindByIdAsync(newComputerPart.Id);
            return newComputerPart;
        }

        public async Task<DeleteResult<ComputerPart>> DeleteByIdAsync(int computerPartId)
        {
            var part = await FindByIdAsync(computerPartId);
            if (part == null)
            {
                return DeleteResult<ComputerPart>.NotFound();
            }

            // In-use guard: block if this part is linked to a computer via the join row
            var inUse = await _context.Computer_ComputerPart
                .AnyAsync(j => j.ComputerPartId == computerPartId);

            if (inUse)
            {
                return DeleteResult<ComputerPart>.InUse(part);
            }

            _context.ComputerPart.Remove(part);
            await _context.SaveChangesAsync();
            return DeleteResult<ComputerPart>.Deleted(part);
        }

        public async Task<ComputerPart?> FindByIdAsync(int computerPartId)
        {
            return await _context.ComputerPart
                .Include(cp => cp.PartGroup)
                .ThenInclude(pg => pg.PartType)
                .Include(cp => cp.Computer_ComputerPart)
                .FirstOrDefaultAsync(cp => cp.Id == computerPartId);
        }

        public async Task<List<ComputerPart>> GetAllAsync()
        {
            return await _context.ComputerPart
                .Include(cp => cp.PartGroup)
                .ThenInclude(pg => pg.PartType)
                .Include(cp => cp.Computer_ComputerPart)
                .ToListAsync();
        }

        public async Task<ComputerPart?> UpdateByIdAsync(int computerPartId, ComputerPart updateComputerPart)
        {
            var computerPart = await FindByIdAsync(computerPartId);
            if (computerPart != null)
            {
                computerPart.PartGroupId = updateComputerPart.PartGroupId;
                computerPart.SerialNumber = updateComputerPart.SerialNumber;
                computerPart.ModelNumber = updateComputerPart.ModelNumber;

                await _context.SaveChangesAsync();

                computerPart = await FindByIdAsync(computerPartId);
            }
            return computerPart;
        }
    }
}
