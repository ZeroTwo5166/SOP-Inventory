using Microsoft.EntityFrameworkCore;
using SOP.Database;
using SOP.Entities;

namespace SOP.Repositories
{
    public interface IRoleRepository
    {
        Task<List<Role>> GetAllAsync();
        Task<Role?> FindByIdAsync(int id);

        // NEW: guarded delete (prevents deleting roles that are in use by users)
        Task<DeleteResult<Role>> DeleteByIdAsync(int id);
    }

    public class RoleRepository : IRoleRepository
    {
        private readonly DatabaseContext _context;

        public RoleRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<List<Role>> GetAllAsync()
        {
            return await _context.Role.ToListAsync();
        }

        public async Task<Role?> FindByIdAsync(int roleId)
        {
            return await _context.Role.FirstOrDefaultAsync(x => x.Id == roleId);
        }

        // NEW: Return DeleteResult to indicate NotFound/InUse/Deleted
        public async Task<DeleteResult<Role>> DeleteByIdAsync(int id)
        {
            var role = await FindByIdAsync(id);
            if (role == null)
            {
                return DeleteResult<Role>.NotFound();
            }

            // "In use" check: any users still assigned to this role?
            var inUse = await _context.User.AnyAsync(u => u.RoleId == id);
            if (inUse)
            {
                // Do not delete; let controller map this to HTTP 409
                return DeleteResult<Role>.InUse(null);
            }

            _context.Role.Remove(role);
            await _context.SaveChangesAsync();

            return DeleteResult<Role>.Deleted(role);
        }
    }
}
