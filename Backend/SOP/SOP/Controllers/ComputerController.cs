using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOP.DTOs;
using SOP.Encryption;
using SOP.Entities;
using SOP.Repositories;

namespace SOP.Controllers
{
    //create the route for our angular to call
    [Route("api/[controller]")]
    [ApiController]
    public class ComputerController : ControllerBase
    {
        // Injecting the IRoomRepository interface and storing it in a private readonly variable
        // This allows access to the room repository methods throughout the class
        private readonly IComputerRepository _computerRepository;

        // Initializes the controller with the address repository
        public ComputerController(IComputerRepository computerRepository)
        {
            // Assigning the repository to the private variable
            _computerRepository = computerRepository;
        }

        private static string? SafeDecrypt(string? v)
        {
            if (string.IsNullOrWhiteSpace(v)) return v;
            try { return EncryptionHelper.Decrypt(v); } catch { return v; }
        }

        [Authorize("Admin", "Instruktør")]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                var computers = await _computerRepository.GetAllAsync();

                List<ComputerResponse> computerResponses = computers.Select(
                    computer => MapComputerToComputerResponse(computer)).ToList();

                return Ok(computerResponses);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør")]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] ComputerRequest computerRequest)
        {
            try
            {
                Computer newComputer = MapComputerRequestToComputer(computerRequest);

                var computer = await _computerRepository.CreateAsync(newComputer);

                ComputerResponse computerResponse = MapComputerToComputerResponse(computer);

                return Ok(computerResponse);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør")]
        [HttpGet("{computerId}")]
        public async Task<IActionResult> FindByIdAsync([FromRoute] int computerId)
        {
            try
            {
                var computer = await _computerRepository.FindByIdAsync(computerId);
                if (computer == null)
                {
                    return NotFound();
                }

                return Ok(MapComputerToComputerResponse(computer));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        // Guarded delete: 404 if not found, 409 if in use, 200 if deleted
        [Authorize("Admin", "Instruktør")]
        [HttpDelete("{computerId}")]
        public async Task<IActionResult> DeleteByIdAsync([FromRoute] int computerId)
        {
            try
            {
                var result = await _computerRepository.DeleteByIdAsync(computerId);

                return result.Status switch
                {
                    DeleteStatus.NotFound => NotFound(),
                    DeleteStatus.InUse => Conflict(new
                    {
                        code = "COMPUTER_IN_USE",
                        message = "Computer has attached parts and cannot be deleted."
                    }),
                    DeleteStatus.Deleted => Ok(MapComputerToComputerResponse(result.Entity!)),
                    _ => Problem("Unknown delete result.")
                };
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        // Guarded delete that also removes the underlying Item when safe
        [Authorize("Admin")]
        [HttpDelete("deleteComputerAndItem/{computerId}")]
        public async Task<IActionResult> DeleteComputerAndItemByIdAsync([FromRoute] int computerId)
        {
            try
            {
                var result = await _computerRepository.DeleteComputerAndItemByIdAsync(computerId);

                return result.Status switch
                {
                    DeleteStatus.NotFound => NotFound(),
                    DeleteStatus.InUse => Conflict(new
                    {
                        code = "COMPUTER_OR_ITEM_IN_USE",
                        message = "Computer (or its Item) is in use and cannot be deleted."
                    }),
                    DeleteStatus.Deleted => Ok(MapComputerToComputerResponse(result.Entity!)),
                    _ => Problem("Unknown delete result.")
                };
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        private ComputerResponse MapComputerToComputerResponse(Computer computer)
        {
            // Initialize the response object
            ComputerResponse response = new ComputerResponse
            {
                Id = computer.Id,
            };

            // Item
            if (computer.Item != null)
            {
                response.Item = new ComputerItemResponse
                {
                    Id = computer.Item.Id,
                    RoomId = computer.Item.RoomId,
                    ItemGroupId = computer.Item.ItemGroupId,
                    SerialNumber = SafeDecrypt(computer.Item.SerialNumber)
                };

                // ItemGroup
                if (computer.Item.ItemGroup != null)
                {
                    response.Item.ItemGroup = new ComputerItemItemGroupResponse
                    {
                        Id = computer.Item.ItemGroup.Id,
                        ItemTypeId = computer.Item.ItemGroup.ItemTypeId,
                        Manufacturer = SafeDecrypt(computer.Item.ItemGroup.Manufacturer),
                        ModelName = SafeDecrypt(computer.Item.ItemGroup.ModelName),
                        WarrantyPeriod = SafeDecrypt(computer.Item.ItemGroup.WarrantyPeriod),
                        Price = computer.Item.ItemGroup.Price,
                        Quantity = computer.Item.ItemGroup.Quantity,
                    };

                    // ItemType
                    if (computer.Item.ItemGroup.ItemType != null)
                    {
                        response.Item.ItemGroup.ItemType = new ComputerItemGroupItemTypeResponse
                        {
                            Id = computer.Item.ItemGroup.ItemType.Id,
                            TypeName = computer.Item.ItemGroup.ItemType.TypeName,
                        };
                    }
                }
            }

            // Parts
            if (computer.Computer_ComputerParts != null)
            {
                response.Computer_ComputerParts = computer.Computer_ComputerParts
                    .Select(computer_computerPart =>
                    {
                        var partResponse = new ComputerComputer_ComputerPartResponse
                        {
                            Id = computer_computerPart.Id,
                            ComputerId = computer_computerPart.ComputerId,
                            ComputerPartId = computer_computerPart.ComputerPartId
                        };

                        if (computer_computerPart.ComputerPart != null)
                        {
                            partResponse.ComputerPart = new ComputerComputer_ComputerPartComputerPartResponse
                            {
                                Id = computer_computerPart.ComputerPart.Id,
                                PartGroupId = computer_computerPart.ComputerPart.PartGroupId,
                                SerialNumber = SafeDecrypt(computer_computerPart.ComputerPart.SerialNumber),
                                ModelNumber = SafeDecrypt(computer_computerPart.ComputerPart.ModelNumber)
                            };

                            if (computer_computerPart.ComputerPart.PartGroup != null)
                            {
                                partResponse.ComputerPart.group = new ComputerComputer_ComputerPartComputerPartPartGroupResponse
                                {
                                    Id = computer_computerPart.ComputerPart.PartGroup.Id,
                                    PartTypeId = computer_computerPart.ComputerPart.PartGroup.PartTypeId,
                                    PartName = SafeDecrypt(computer_computerPart.ComputerPart.PartGroup.PartName),
                                    Price = computer_computerPart.ComputerPart.PartGroup.Price,
                                    Manufacturer = SafeDecrypt(computer_computerPart.ComputerPart.PartGroup.Manufacturer),
                                    WarrantyPeriod = SafeDecrypt(computer_computerPart.ComputerPart.PartGroup.WarrantyPeriod),
                                    ReleaseDate = computer_computerPart.ComputerPart.PartGroup.ReleaseDate,
                                    Quantity = computer_computerPart.ComputerPart.PartGroup.Quantity
                                };

                                if (computer_computerPart.ComputerPart.PartGroup.PartType != null)
                                {
                                    partResponse.ComputerPart.group.PartType = new ComputerPartTypeResponse
                                    {
                                        Id = computer_computerPart.ComputerPart.PartGroup.PartType.Id,
                                        partTypeName = computer_computerPart.ComputerPart.PartGroup.PartType.PartTypeName,
                                    };
                                }
                            }
                        }

                        return partResponse;
                    })
                    .ToList();
            }

            return response;
        }

        private Computer MapComputerRequestToComputer(ComputerRequest computerRequest)
        {
            return new Computer
            {
                Id = computerRequest.Id,
            };
        }
    }
}
