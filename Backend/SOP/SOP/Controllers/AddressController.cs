using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOP.DTOs;
using SOP.Entities;
using SOP.Repositories;
using SOP.Encryption; 

namespace SOP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddressController : ControllerBase
    {
        private readonly IAddressRepository _addressRepository;

        public AddressController(IAddressRepository adressRepository)
        {
            _addressRepository = adressRepository;
        }

        private static string? SafeDecrypt(string? v)
        {
            if (string.IsNullOrWhiteSpace(v)) return v;
            try { return EncryptionHelper.Decrypt(v); }
            catch { return v; }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            var addresses = await _addressRepository.GetAllAsync();
            var dto = addresses.Select(MapAddressToAddressResponse).ToList();
            return Ok(dto);
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] AddressRequest addressRequest)
        {
            var newAddress = MapAddressRequestToAddress(addressRequest);

            // encrypt PII-ish strings
            newAddress.City = EncryptionHelper.Encrypt(newAddress.City);
            newAddress.Region = EncryptionHelper.Encrypt(newAddress.Region);
            newAddress.Road = EncryptionHelper.Encrypt(newAddress.Road);

            var saved = await _addressRepository.CreateAsync(newAddress);
            return Ok(MapAddressToAddressResponse(saved));
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet("{Id}")]
        public async Task<IActionResult> FindByIdAsync([FromRoute] int Id)
        {
            var address = await _addressRepository.FindByIdAsync(Id);
            if (address is null) return NotFound();
            return Ok(MapAddressToAddressResponse(address));
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPut("{Id}")]
        public async Task<IActionResult> UpdateByIdAsync([FromRoute] int Id, [FromBody] AddressRequest addressRequest)
        {
            var updateAddress = MapAddressRequestToAddress(addressRequest);

            // encrypt PII-ish strings
            updateAddress.City = EncryptionHelper.Encrypt(updateAddress.City);
            updateAddress.Region = EncryptionHelper.Encrypt(updateAddress.Region);
            updateAddress.Road = EncryptionHelper.Encrypt(updateAddress.Road);

            var updated = await _addressRepository.UpdateByIdAsync(Id, updateAddress);
            if (updated is null) return NotFound();

            return Ok(MapAddressToAddressResponse(updated));
        }

        [Authorize("Admin")]
        [HttpDelete("{Id}")]
        public async Task<IActionResult> DeleteByIdAsync([FromRoute] int Id)
        {
            var result = await _addressRepository.DeleteByIdAsync(Id);

            return result.Status switch
            {
                DeleteStatus.NotFound => NotFound(),
                DeleteStatus.InUse => Conflict(new
                {
                    code = "ADDRESS_IN_USE",
                    message = "Address is referenced by one or more buildings and cannot be deleted."
                }),
                DeleteStatus.Deleted => Ok(MapAddressToAddressResponse(result.Entity!))
            };
        }

        private static AddressResponse MapAddressToAddressResponse(Address address)
        {
            return new AddressResponse
            {
                Id = address.Id,
                ZipCode = address.ZipCode,
                Region = SafeDecrypt(address.Region),
                City = SafeDecrypt(address.City),
                Road = SafeDecrypt(address.Road),
            };
        }

        private static Address MapAddressRequestToAddress(AddressRequest addressRequest)
        {
            return new Address
            {
                ZipCode = addressRequest.ZipCode,
                Region = addressRequest.Region,
                City = addressRequest.City,
                Road = addressRequest.Road,
            };
        }
    }
}
