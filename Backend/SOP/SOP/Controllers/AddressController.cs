using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOP.DTOs;
using SOP.Entities;
using SOP.Repositories;
using System.Net;

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

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                List<Address> address = await _addressRepository.GetAllAsync();

                List<AddressResponse> addressResponses = address.Select(
                    address => MapAddressToAddressResponse(address)).ToList();
                
                return Ok(addressResponses);
            }
            catch (Exception ex)
            {

                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] AddressRequest addressRequest)
        {
            try
            {
                Address newAddress = MapAddressRequestToAddress(addressRequest);

                var address = await _addressRepository.CreateAsync(newAddress);

                AddressResponse addressResponse = MapAddressToAddressResponse(address);

                return Ok(addressResponse);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet]
        [Route("{Id}")]
        public async Task<IActionResult> FindByIdAsync([FromRoute] int Id)
        {
            try
            {
                var address = await _addressRepository.FindByIdAsync(Id);

                if (address == null)
                {
                    return NotFound(); 
                }

                return Ok(MapAddressToAddressResponse(address));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]

        [HttpPut]
        [Route("{Id}")]
        public async Task<IActionResult> UpdateByIdAsync([FromRoute] int Id, [FromBody] AddressRequest addressRequest)
        {
            try
            {
                var updateAddress = MapAddressRequestToAddress(addressRequest);

                var address = await _addressRepository.UpdateByIdAsync(Id, updateAddress);

                if (address == null)
                {
                    return NotFound(); 
                }

                return Ok(MapAddressToAddressResponse(address));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin")]
        [HttpDelete]
        [Route("{Id}")]
        public async Task<IActionResult> DeleteByIdAsync([FromRoute] int Id)
        {
            try
            {
                var address = await _addressRepository.DeleteByIdAsync(Id);

                if (address == null)
                {
                    return NotFound(); 
                }

                return Ok(MapAddressToAddressResponse(address));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        private static AddressResponse MapAddressToAddressResponse(Address address)
        {
            AddressResponse response = new AddressResponse
            {
                Id = address.Id,
                ZipCode = address.ZipCode,
                Region = address.Region,
                City = address.City,
                Road = address.Road,
            };

            return response;
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
