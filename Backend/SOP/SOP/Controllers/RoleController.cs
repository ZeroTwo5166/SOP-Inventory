using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOP.DTOs;
using SOP.Entities;
using SOP.Repositories;

namespace SOP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly IRoleRepository _roleRepository;

        public RoleController(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                var roles = await _roleRepository.GetAllAsync();
                var roleResponses = roles.Select(MapRoleToRoleResponse).ToList();
                return Ok(roleResponses);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet("{Id}")]
        public async Task<IActionResult> FindByIdAsync([FromRoute] int Id)
        {
            try
            {
                var role = await _roleRepository.FindByIdAsync(Id);
                if (role == null) return NotFound();

                return Ok(MapRoleToRoleResponse(role));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        // NEW: Delete with "in-use" guard (maps to 409 Conflict)
        [Authorize("Admin")]
        [HttpDelete("{Id}")]
        public async Task<IActionResult> DeleteByIdAsync([FromRoute] int Id)
        {
            try
            {
                var result = await _roleRepository.DeleteByIdAsync(Id);

                switch (result.Status)
                {
                    case DeleteStatus.NotFound:
                        return NotFound();

                    case DeleteStatus.InUse:
                        // Role is assigned to one or more users -> 409 Conflict
                        return Conflict(new
                        {
                            message = "Rollen kan ikke slettes, fordi den er i brug af en eller flere brugere."
                        });

                    case DeleteStatus.Deleted:
                        // Return the deleted role (mapped to DTO)
                        return Ok(MapRoleToRoleResponse(result.Entity!));

                    default:
                        // Fallback (shouldn't happen)
                        return Problem("Ukendt sletningsstatus.");
                }
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        private static RoleResponse MapRoleToRoleResponse(Role role) => new RoleResponse
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
        };

        private static Role MapRoleRequestToRole(RoleRequest roleRequest) => new Role
        {
            Name = roleRequest.Name,
            Description = roleRequest.Description,
        };
    }
}
