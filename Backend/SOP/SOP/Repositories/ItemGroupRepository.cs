using Microsoft.EntityFrameworkCore;
using SOP.Archive.Entities;
using SOP.Database;

namespace SOP.Repositories
{
    public interface IItemGroupRepository
    {
        Task<ItemGroup> CreateAsync(ItemGroup newItemGroup);
        Task<ItemGroup?> FindByIdAsync(int itemGroupId);
        Task<ItemGroup?> UpdateByIdAsync(int itemGroupId, ItemGroup updateItemGroup);
        Task<List<ItemGroup>> GetAllAsync();
        Task<ArchiveResult<Archive_ItemGroup>> ArchiveByIdAsync(int itemGroupId, string archiveNote);
    }

    public class ItemGroupRepository : IItemGroupRepository
    {
        private readonly DatabaseContext _context;

        // Initializes the repository with the database context for accessing data
        public ItemGroupRepository(DatabaseContext context)
        {
            _context = context;
        }

        // Adds a new ItemGroup, saves changes, retrieves, and returns it
        public async Task<ItemGroup> CreateAsync(ItemGroup newItemGroup)
        {
            _context.ItemGroup.Add(newItemGroup);
            await _context.SaveChangesAsync();
            newItemGroup = await FindByIdAsync(newItemGroup.Id);
            return newItemGroup;
        }

        // Please refer to the class diagram or ER diagram for entity relationships
        // Finds a ItemGroup by ID, including related entities and returns it
        public async Task<ItemGroup?> FindByIdAsync(int itemGroupId)
        {
            return await _context.ItemGroup.Include(i => i.ItemType)
                .FirstOrDefaultAsync(i => i.Id == itemGroupId);
        }

        // Updates a ItemGroup by ID and returns the updated entity
        public async Task<ItemGroup?> UpdateByIdAsync(int itemGroupId, ItemGroup updateItemGroup)
        {
            var itemGroup = await FindByIdAsync(itemGroupId);
            if (itemGroup != null)
            {
                itemGroup.Quantity = updateItemGroup.Quantity;
                itemGroup.Price = updateItemGroup.Price;
                itemGroup.Manufacturer = updateItemGroup.Manufacturer;
                itemGroup.ItemTypeId = updateItemGroup.ItemTypeId;
                itemGroup.ModelName = updateItemGroup.ModelName;
                itemGroup.WarrantyPeriod = updateItemGroup.WarrantyPeriod;

                await _context.SaveChangesAsync();

                itemGroup = await FindByIdAsync(itemGroupId);
            }
            return itemGroup;
        }

        // Please refer to the class diagram or ER diagram for entity relationships
        // Retrieves all ItemGroups, including related entities and returns them
        public async Task<List<ItemGroup>> GetAllAsync()
        {
            return await _context.ItemGroup.Include(i => i.ItemType)
                .ToListAsync();
        }

        // Archive an ItemGroup by ID, including all associated items (and status histories),
        // but block if any item is "in use" (active loan or bound Computer)
        public async Task<ArchiveResult<Archive_ItemGroup>> ArchiveByIdAsync(int itemGroupId, string archiveNote)
        {
            var itemGroup = await FindByIdAsync(itemGroupId);
            if (itemGroup is null)
                return ArchiveResult<Archive_ItemGroup>.NotFound();

            // (optional but recommended) block if any item is "in use"
            var itemIdsQuery = _context.Item
                .Where(i => i.ItemGroupId == itemGroupId)
                .Select(i => i.Id);

            var hasActiveLoans = await _context.Loan
                .AnyAsync(l => l.ReturnDate == null && itemIdsQuery.Contains(l.ItemId));
            if (hasActiveLoans)
                return ArchiveResult<Archive_ItemGroup>.InUse(null);

            var hasComputers = await _context.Computer
                .AnyAsync(c => itemIdsQuery.Contains(c.Id));
            if (hasComputers)
                return ArchiveResult<Archive_ItemGroup>.InUse(null);

            // Archive all items in the group first
            var itemsToArchive = await _context.Item
                .Include(item => item.StatusHistories)
                .Where(item => item.ItemGroupId == itemGroupId)
                .ToListAsync();

            foreach (var item in itemsToArchive)
            {
                await ArchiveItem(item, archiveNote);
            }

            var archiveItemGroup = new Archive_ItemGroup
            {
                Id = itemGroup.Id,
                DeleteTime = DateTime.Now,
                Quantity = itemGroup.Quantity,
                Price = itemGroup.Price,
                Manufacturer = itemGroup.Manufacturer,
                ItemTypeId = itemGroup.ItemTypeId,
                ModelName = itemGroup.ModelName,
                WarrantyPeriod = itemGroup.WarrantyPeriod,
                ArchiveNote = archiveNote,
            };

            _context.Archive_ItemGroup.Add(archiveItemGroup);
            _context.ItemGroup.Remove(itemGroup);
            await _context.SaveChangesAsync();

            return ArchiveResult<Archive_ItemGroup>.Archived(archiveItemGroup);
        }


        private async Task ArchiveItem(Item item, string archiveNote)
        {
            Archive_Item archiveItem = new Archive_Item
            {
                Id = item.Id,
                DeleteTime = DateTime.Now,
                ItemGroupId = item.ItemGroupId,
                RoomId = item.RoomId,
                SerialNumber = item.SerialNumber,
                ArchiveNote = archiveNote,
                StatusHistories = item.StatusHistories?
                    .Select(statusHistory => new Archive_StatusHistory
                    {
                        Id = statusHistory.Id,
                        ItemId = item.Id,
                        StatusId = statusHistory.StatusId,
                        StatusUpdateDate = statusHistory.StatusUpdateDate,
                        Note = statusHistory.Note,
                        ArchiveNote = archiveNote,
                        DeleteTime = DateTime.Now,
                    })
                    .ToList()
            };

            _context.Archive_Item.Add(archiveItem);
            _context.Item.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
}