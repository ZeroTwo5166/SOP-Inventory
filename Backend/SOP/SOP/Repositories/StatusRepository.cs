using Microsoft.EntityFrameworkCore;
using SOP.Database;
using SOP.Entities;

namespace SOP.Repositories
{
    public interface IStatusRepository
    {
        Task<Status> CreateAsync(Status newStatus);
        Task<Status?> FindByIdAsync(int statusId);
        Task<List<Status>> GetAllAsync();

        // Guarded delete: returns NotFound / InUse / Deleted
        Task<DeleteResult<Status>> DeleteByIdAsync(int statusId);

        // Optional helper (kept for reuse)
        Task<bool> HasStatusHistoryAsync(int statusId);
    }

    public class StatusRepository : IStatusRepository
    {
        private readonly DatabaseContext _context;

        public StatusRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Status> CreateAsync(Status newStatus)
        {
            _context.Status.Add(newStatus);
            await _context.SaveChangesAsync();
            newStatus = await FindByIdAsync(newStatus.Id);
            return newStatus;
        }

        public async Task<Status?> FindByIdAsync(int statusId)
        {
            return await _context.Status.FindAsync(statusId);
        }

        public async Task<List<Status>> GetAllAsync()
        {
            return await _context.Status.ToListAsync();
        }

        public async Task<DeleteResult<Status>> DeleteByIdAsync(int statusId)
        {
            var status = await FindByIdAsync(statusId);
            if (status == null)
            {
                return DeleteResult<Status>.NotFound();
            }

            // "In use" guard: any StatusHistory rows referencing this status?
            var inUse = await _context.StatusHistory.AnyAsync(sh => sh.StatusId == statusId);
            if (inUse)
            {
                // Don’t delete; let controller map to HTTP 409
                return DeleteResult<Status>.InUse(null);
            }

            _context.Status.Remove(status);
            await _context.SaveChangesAsync();
            return DeleteResult<Status>.Deleted(status);
        }

        // Helper retained if you want to check elsewhere
        public async Task<bool> HasStatusHistoryAsync(int statusId)
        {
            return await _context.StatusHistory.AnyAsync(sh => sh.StatusId == statusId);
        }
    }
}
