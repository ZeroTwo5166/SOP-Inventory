using Microsoft.EntityFrameworkCore;
using SOP.Database;
using SOP.Entities;

namespace SOP.Repositories
{
    public interface IComputerRepository
    {
        Task<Computer> CreateAsync(Computer newComputer);
        Task<DeleteResult<Computer>> DeleteByIdAsync(int computerId);
        Task<DeleteResult<Computer>> DeleteComputerAndItemByIdAsync(int computerId);
        Task<Computer?> FindByIdAsync(int computerId);
        Task<List<Computer>> GetAllAsync();
    }
    public class ComputerRepository : IComputerRepository
    {
        private readonly DatabaseContext _context;

        // Initializes the repository with the database context for accessing data
        public ComputerRepository(DatabaseContext context)
        {
            _context = context;
        }

        // Adds a new Computer, saves changes, retrieves, and returns it
        public async Task<Computer> CreateAsync(Computer newComputer)
        {
            _context.Computer.Add(newComputer);
            await _context.SaveChangesAsync();
            newComputer = await FindByIdAsync(newComputer.Id);
            return newComputer;
        }

        // Finds and deletes a Computer by ID, then saves changes and returns it
        public async Task<DeleteResult<Computer?>> DeleteByIdAsync(int computerId)
        {
            var computer = await FindByIdAsync(computerId);
            if (computer is null)
                return DeleteResult<Computer>.NotFound();

            // Guard: refuse delete if this computer still has parts
            var hasParts = await _context.Computer_ComputerPart.AnyAsync(j => j.ComputerId == computerId);
            if (hasParts)
                return DeleteResult<Computer>.InUse(computer);

            _context.Computer.Remove(computer);
            await _context.SaveChangesAsync();
            return DeleteResult<Computer>.Deleted(computer);
        }

        public async Task<DeleteResult<Computer>> DeleteComputerAndItemByIdAsync(int computerId)
        {
            // Load with Item for the combined delete
            var computer = await _context.Computer
                .Include(c => c.Item)
                .FirstOrDefaultAsync(c => c.Id == computerId);

            if (computer is null)
                return DeleteResult<Computer>.NotFound();

            // 1) Block if still has parts
            var hasParts = await _context.Computer_ComputerPart.AnyAsync(j => j.ComputerId == computerId);
            if (hasParts)
                return DeleteResult<Computer>.InUse(computer);

            // 2) Block if the item is currently loaned out
            // (Computer.Id == Item.Id by model, so use computerId)
            var hasActiveLoan = await _context.Loan.AnyAsync(l => l.ItemId == computerId && l.ReturnDate == null);
            if (hasActiveLoan)
                return DeleteResult<Computer>.InUse(computer);

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                if (computer.Item is not null)
                    _context.Item.Remove(computer.Item);

                _context.Computer.Remove(computer);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            return DeleteResult<Computer>.Deleted(computer);
        }

        // Please refer to the class diagram or ER diagram for entity relationships
        // Finds a Computer by ID, including related entities and returns it
        public async Task<Computer?> FindByIdAsync(int computerId)
        {
            return await _context.Computer
                .Include(c => c.Item)
                .Include(c => c.Computer_ComputerParts)
                .ThenInclude(ccp => ccp.ComputerPart)
                .ThenInclude(cp => cp.PartGroup)
                .ThenInclude(pg => pg.PartType)
                .FirstOrDefaultAsync(c => c.Id == computerId);
        }

        // Please refer to the class diagram or ER diagram for entity relationships
        // Retrieves all Computers, including related entities and returns them
        public async Task<List<Computer>> GetAllAsync()
        {
            return await _context.Computer
                .Include(c => c.Item)
                .Include(c => c.Computer_ComputerParts)
                .ThenInclude(ccp => ccp.ComputerPart)
                .ThenInclude(cp => cp.PartGroup)
                .ThenInclude(pg => pg.PartType)
                .ToListAsync();
        }
    }
}
