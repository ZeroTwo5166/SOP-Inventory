using Microsoft.EntityFrameworkCore;
using SOP.Database;
using SOP.Entities;

namespace SOP.Repositories
{
    public interface IAddressRepository
    {
        Task<List<Address>> GetAllAsync();
        Task<Address> CreateAsync(Address address);
        Task<Address> FindByIdAsync(int id);
        Task<Address> UpdateByIdAsync(int id, Address address);
        Task<Address> DeleteByIdAsync(int id);
    }
    public class AddressRepository : IAddressRepository
    {
        private readonly DatabaseContext _context;

        public AddressRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<List<Address>> GetAllAsync()
        {
            return await _context.Address.ToListAsync();
        }

        public async Task<Address> CreateAsync(Address newAddress)
        {
            _context.Address.Add(newAddress);

            await _context.SaveChangesAsync();

            return newAddress;
        }

        public async Task<Address?> FindByIdAsync(int id)
        {
            return await _context.Address
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Address> UpdateByIdAsync(int id, Address newAddress)
        {
            var address = await FindByIdAsync(id);

            if (address != null)
            {
                address.Region = newAddress.Region;
                address.City = newAddress.City;
                address.Road = newAddress.Road;
                address.ZipCode = newAddress.ZipCode;


                await _context.SaveChangesAsync();

                address = await FindByIdAsync(id);
            }
            return address;
        }

        public async Task<Address> DeleteByIdAsync(int addressId)
        {
            var address = await FindByIdAsync(addressId);
            if (address != null)
            {
                _context.Address.Remove(address);

                await _context.SaveChangesAsync();
            }
            return address;
        }
    }
}
