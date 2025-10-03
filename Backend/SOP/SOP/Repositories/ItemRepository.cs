using Microsoft.EntityFrameworkCore;
using SOP.Database;
using SOP.Entities;
using SOP.Archive.Entities;

namespace SOP.Repositories
{
    public interface IItemRepository
    {
        Task<Item> CreateAsync(Item newItem);
        Task<Item?> FindByIdAsync(int itemId);
        Task<Item?> UpdateByIdAsync(int itemId, Item updateItem);
        Task<List<Item>> GetAllAsync();

        // Guarded operations
        Task<DeleteResult<Item>> DeleteByIdAsync(int itemId);
        Task<ArchiveResult<Archive_Item>> ArchiveByIdAsync(int itemId, string archiveNote);
    }

    public class ItemRepository : IItemRepository
    {
        private readonly DatabaseContext _context;

        public ItemRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Item> CreateAsync(Item newItem)
        {
            _context.Item.Add(newItem);
            await _context.SaveChangesAsync();
            return await FindByIdAsync(newItem.Id);
        }

        public async Task<Item?> FindByIdAsync(int itemId)
        {
            return await _context.Item
                .Include(i => i.ItemGroup)
                    .ThenInclude(ig => ig.ItemType)
                .Include(i => i.StatusHistories)
                    .ThenInclude(sh => sh.Status)
                .Include(i => i.Room)
                    .ThenInclude(r => r.Building)
                        .ThenInclude(b => b.Address)
                .Include(i => i.Loan)
                .FirstOrDefaultAsync(i => i.Id == itemId);
        }

        public async Task<List<Item>> GetAllAsync()
        {
            return await _context.Item
                .Include(i => i.ItemGroup)
                    .ThenInclude(ig => ig.ItemType)
                .Include(i => i.StatusHistories)
                    .ThenInclude(sh => sh.Status)
                .Include(i => i.Room)
                    .ThenInclude(r => r.Building)
                        .ThenInclude(b => b.Address)
                .Include(i => i.Loan)
                .ToListAsync();
        }

        public async Task<Item?> UpdateByIdAsync(int itemId, Item updateItem)
        {
            var item = await FindByIdAsync(itemId);
            if (item == null) return null;

            item.ItemGroupId = updateItem.ItemGroupId;
            item.RoomId = updateItem.RoomId;
            item.SerialNumber = updateItem.SerialNumber;

            await _context.SaveChangesAsync();
            return await FindByIdAsync(itemId);
        }

        // ========= Guarded hard delete =========
        public async Task<DeleteResult<Item>> DeleteByIdAsync(int itemId)
        {
            var item = await _context.Item
                .Include(i => i.Room)
                .Include(i => i.ItemGroup)
                .FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null)
                return DeleteResult<Item>.NotFound();

            var inUse = await _context.Loan.AnyAsync(l => l.ItemId == itemId && l.ReturnDate == null);
            if (inUse)
                return DeleteResult<Item>.InUse(null);

            // If cascade isn't configured for StatusHistory, remove them manually
            var histories = _context.StatusHistory.Where(sh => sh.ItemId == itemId);
            _context.StatusHistory.RemoveRange(histories);

            _context.Item.Remove(item);
            await _context.SaveChangesAsync();

            return DeleteResult<Item>.Deleted(item);
        }

        // ========= Guarded archive =========
        public async Task<ArchiveResult<Archive_Item>> ArchiveByIdAsync(int itemId, string archiveNote)
        {
            var item = await _context.Item
                .Include(i => i.StatusHistories)
                .FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null)
                return ArchiveResult<Archive_Item>.NotFound();

            var inUse = await _context.Loan.AnyAsync(l => l.ItemId == itemId && l.ReturnDate == null);
            if (inUse)
                return ArchiveResult<Archive_Item>.InUse(null);

            var archive = new Archive_Item
            {
                Id = item.Id,
                DeleteTime = DateTime.Now,
                ItemGroupId = item.ItemGroupId,
                RoomId = item.RoomId,
                SerialNumber = item.SerialNumber,
                ArchiveNote = archiveNote,
                StatusHistories = item.StatusHistories?.Select(sh => new Archive_StatusHistory
                {
                    Id = sh.Id,
                    ItemId = item.Id,
                    StatusId = sh.StatusId,
                    StatusUpdateDate = sh.StatusUpdateDate,
                    Note = sh.Note,
                    ArchiveNote = archiveNote,
                    DeleteTime = DateTime.Now
                }).ToList()
            };

            _context.Archive_Item.Add(archive);

            // If cascade isn't configured for StatusHistory, remove them manually
            var histories = _context.StatusHistory.Where(sh => sh.ItemId == item.Id);
            _context.StatusHistory.RemoveRange(histories);

            _context.Item.Remove(item);
            await _context.SaveChangesAsync();

            return ArchiveResult<Archive_Item>.Archived(archive);
        }
    }
}
