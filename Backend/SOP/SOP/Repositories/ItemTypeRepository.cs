using Microsoft.EntityFrameworkCore;
using SOP.Archive.Entities;
using SOP.Database;
using SOP.Entities;

namespace SOP.Repositories
{
    public interface IItemTypeRepository
    {
        Task<ItemType> CreateAsync(ItemType newItemType);
        Task<ItemType?> FindByIdAsync(int itemTypeId);
        Task<List<ItemType>> GetAllAsync();
        Task<ArchiveResult<Archive_ItemType>> ArchiveByIdAsync(int itemTypeId, string archiveNote);
    }

    public class ItemTypeRepository : IItemTypeRepository
    {
        private readonly DatabaseContext _context;

        public ItemTypeRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<ItemType> CreateAsync(ItemType newItemType)
        {
            _context.ItemType.Add(newItemType);
            await _context.SaveChangesAsync();
            newItemType = await FindByIdAsync(newItemType.Id);
            return newItemType;
        }

        public async Task<ItemType?> FindByIdAsync(int itemTypeId)
        {
            return await _context.ItemType.FindAsync(itemTypeId);
        }

        public async Task<List<ItemType>> GetAllAsync()
        {
            return await _context.ItemType.ToListAsync();
        }

        /// <summary>
        /// Archive an ItemType only if it's not in use.
        /// "In use" = any ItemGroup exists with this ItemTypeId.
        /// </summary>
        public async Task<ArchiveResult<Archive_ItemType>> ArchiveByIdAsync(int itemTypeId, string archiveNote)
        {
            var itemType = await FindByIdAsync(itemTypeId);
            if (itemType == null)
            {
                return ArchiveResult<Archive_ItemType>.NotFound();
            }

            // In-use guard: if any ItemGroup references this type, block archiving
            var hasGroups = await _context.ItemGroup.AnyAsync(ig => ig.ItemTypeId == itemTypeId);
            if (hasGroups)
            {
                // We can return null as the entity for InUse; the controller will map this to 409 Conflict
                return ArchiveResult<Archive_ItemType>.InUse(null);
            }

            var archive = new Archive_ItemType
            {
                Id = itemType.Id,
                DeleteTime = DateTime.Now,
                TypeName = itemType.TypeName,
                ArchiveNote = archiveNote,
            };

            _context.Archive_ItemType.Add(archive);
            _context.ItemType.Remove(itemType);
            await _context.SaveChangesAsync();

            return ArchiveResult<Archive_ItemType>.Archived(archive);
        }
    }
}
