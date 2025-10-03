using Microsoft.EntityFrameworkCore;
using SOP.Database;
using SOP.Entities;
using System.Data;
using SOP.Encryption;

namespace SOP.Repositories
{
    public interface IUserRepository
    {
        Task<List<User>> GetAllAsync();
        Task<User> CreateAsync(User user);
        Task<User> FindByIdAsync(int id);
        Task<User> UpdateByIdAsync(int id, User user);
        Task<ArchiveResult<Archive_User>> ArchiveByIdAsync(int id, string archiveNote);
        Task<User?> GetByEmail(string email);
        Task<User> UpdatePasswordByIdAsync(int id, User user);
        Task<List<User>> GetUsersByRoleAsync(int RoleId);
    }
    public class UserRepository : IUserRepository
{
        private readonly DatabaseContext _context;

        public UserRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _context.User
                .Include(x => x.Loans)
                .ThenInclude(x => x.Item)
                .ThenInclude(x => x.ItemGroup)
                .ThenInclude(x => x.ItemType)
                .Include(x => x.Role)
                .ToListAsync();
        }

        public async Task<User> CreateAsync(User newUser)
        {
            _context.User.Add(new User()
            {
                Id = newUser.Id,
                Name = newUser.Name,
                Email = newUser.Email,
                Password = newUser.Password,
                Loans = newUser.Loans,
                Role = newUser.Role,
                RoleId = newUser.RoleId,
                TwoFactorAuthentication = newUser.TwoFactorAuthentication,
                ProfileImageUrl = newUser.ProfileImageUrl
            });
            
            await _context.SaveChangesAsync();
            return newUser;
        }

        public async Task<User?> FindByIdAsync(int userId)
        {
            return await _context.User
                .Include(x => x.Loans)
                .ThenInclude(x => x.Item)
                .ThenInclude(x => x.ItemGroup)
                .ThenInclude(x => x.ItemType)
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == userId);
        }

        public async Task<User> UpdateByIdAsync(int id, User updatedUser)
        {
            var user = await FindByIdAsync(id);

            if (user == null) return null;

            // Update only fields from updatedUser (already filtered in controller)
            user.RoleId = updatedUser.RoleId;
            user.Name = updatedUser.Name;
            user.Email = updatedUser.Email;
            user.TwoFactorAuthentication = updatedUser.TwoFactorAuthentication;
            user.ProfileImageUrl = updatedUser.ProfileImageUrl;

            await _context.SaveChangesAsync();
            Console.WriteLine($"Updating ProfileImageUrl: {updatedUser.ProfileImageUrl ?? "null"}");

            return await FindByIdAsync(id);
        }

        // Updates ONLY the password in User by ID and returns the updated entity. 
        public async Task<User> UpdatePasswordByIdAsync(int id, User newUser)
        {
            var user = await FindByIdAsync(id);

            if (user != null)
            {
                user.Password = newUser.Password;

                await _context.SaveChangesAsync();

                user = await FindByIdAsync(id);
            }
            return user;
        }

        // Finds a User by Email, including related entities and returns it
        public async Task<User?> GetByEmail(string email)
        {
            return await _context.User.Include(c => c.Role).FirstOrDefaultAsync(x => x.Email == email);
        }

        // Finds a User by Role, including related entities and returns it
        public async Task<List<User>> GetUsersByRoleAsync(int RoleId)
        {
            return await _context.User.Where(u => u.RoleId == RoleId).ToListAsync();
        }

        // Archive a User by ID, including all associated Loans and Requests

        public async Task<ArchiveResult<Archive_User>> ArchiveByIdAsync(int userId, string archiveNote)
        {
            var user = await FindByIdAsync(userId);
            if (user == null)
                return ArchiveResult<Archive_User>.NotFound();

            // guard: block archive if the user has an active loan
            bool hasActiveLoan = await _context.Loan.AnyAsync(l => l.UserId == userId && l.ReturnDate == null);
            if (hasActiveLoan)
                return ArchiveResult<Archive_User>.InUse(null); // or pass a minimal entity if you prefer

            // … your existing archive logic …
            var archiveUser = new Archive_User
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                RoleId = user.RoleId,
                DeleteTime = DateTime.Now,
                Password = user.Password,
                ArchiveNote = archiveNote,
                TwoFactorAuthentication = user.TwoFactorAuthentication,
                TwoFactorSecretKey = user.TwoFactorSecretKey,
            };

            _context.Archive_User.Add(archiveUser);
            _context.User.Remove(user);
            await _context.SaveChangesAsync();

            return ArchiveResult<Archive_User>.Archived(archiveUser);
        }

        private async Task ArchiveLoan(Loan loan, string archiveNote)
        {
            Archive_Loan archiveLoan = new Archive_Loan
            {
                Id = loan.Id,
                DeleteTime = DateTime.Now,
                UserId = loan.UserId,
                ItemId = loan.ItemId,
                LoanDate = loan.LoanDate,
                ReturnDate = loan.ReturnDate,
                ArchiveNote = archiveNote,
            };

            _context.Archive_Loan.Add(archiveLoan);
            _context.Loan.Remove(loan);
            await _context.SaveChangesAsync();
        }

        private async Task ArchiveRequest(Request request, string archiveNote)
        {
            Archive_Request archiveRequest = new Archive_Request
            {
                Id = request.Id,
                DeleteTime = DateTime.Now,
                UserId = request.UserId,
                RecipientEmail = request.RecipientEmail,
                Item = request.Item,
                Message = request.Message,
                Date = request.Date,
                Status = request.Status,
                ArchiveNote = archiveNote,
            };

            _context.Archive_Request.Add(archiveRequest);
            _context.Request.Remove(request);
            await _context.SaveChangesAsync();
        }
    }
}
