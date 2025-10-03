using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOP.Archive.DTOs;
using SOP.DTOs;
using SOP.Encryption;
using SOP.Entities;
using SOP.Repositories;

namespace SOP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoanController : ControllerBase
    {
        private readonly ILoanRepository _loanRepository;

        public LoanController(ILoanRepository loanRepository)
        {
            _loanRepository = loanRepository;
        }

        private static string? SafeDecrypt(string? v)
        {
            if (string.IsNullOrWhiteSpace(v)) return v;
            try { return EncryptionHelper.Decrypt(v); } catch { return v; }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                List<Loan> loans = await _loanRepository.GetAllAsync();

                List<LoanResponse> responses = loans
                    .Select(loan => MapLoanToLoanResponse(loan))
                    .ToList();

                return Ok(responses);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] LoanRequest loanRequest)
        {
            try
            {
                Loan newLoan = MapLoanRequestToLoan(loanRequest);

                var loan = await _loanRepository.CreateAsync(newLoan);

                LoanResponse loanResponse = MapLoanToLoanResponse(loan);

                return Ok(loanResponse);
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
                var loan = await _loanRepository.FindByIdAsync(Id);
                if (loan == null)
                {
                    return NotFound();
                }

                return Ok(MapLoanToLoanResponse(loan));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPut("{Id}")]
        public async Task<IActionResult> UpdateByIdAsync([FromRoute] int Id, [FromBody] LoanRequest loanRequest)
        {
            try
            {
                var updateLoan = MapLoanRequestToLoan(loanRequest);

                var loan = await _loanRepository.UpdateByIdAsync(Id, updateLoan);

                if (loan == null)
                {
                    return NotFound();
                }

                return Ok(MapLoanToLoanResponse(loan));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        // Archive (soft-delete) with conflict handling via ArchiveResult<>
        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpDelete("{Id}")]
        public async Task<IActionResult> ArchiveByIdAsync([FromRoute] int Id, [FromBody] ArchiveNoteRequest archiveNoteRequest)
        {
            try
            {
                var encryptedNote = EncryptionHelper.Encrypt(archiveNoteRequest?.ArchiveNote ?? string.Empty);

                var result = await _loanRepository.ArchiveByIdAsync(Id, encryptedNote);

                switch (result.Status)
                {
                    case ArchiveStatus.NotFound:
                        return NotFound();

                    case ArchiveStatus.InUse:
                        // Convention: InUse means loan cannot be archived due to business rule
                        return Conflict(new { message = "Lånet er stadig aktivt og kan ikke arkiveres endnu." });

                    case ArchiveStatus.Archived:
                        var loan = result.Entity!;
                        var response = new Archive_LoanResponse
                        {
                            Id = loan.Id,
                            DeleteTime = loan.DeleteTime,
                            LoanDate = loan.LoanDate,
                            ReturnDate = loan.ReturnDate,
                            ItemId = loan.ItemId,
                            UserId = loan.UserId,
                            ArchiveNote = SafeDecrypt(loan.ArchiveNote),
                        };
                        return Ok(response);

                    default:
                        return Problem("Ukendt arkiveringsstatus.");
                }
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        private static LoanResponse MapLoanToLoanResponse(Loan loan)
        {
            LoanResponse response = new LoanResponse
            {
                Id = loan.Id,
                LoanDate = loan.LoanDate,
                ReturnDate = loan.ReturnDate,
                ItemId = loan.ItemId,
                UserId = loan.UserId,
            };

            if (loan.User != null)
            {
                response.LoanUser = new LoanUserResponse
                {
                    Id = loan.User.Id,
                    Email = SafeDecrypt(loan.User.Email),
                    Name = loan.User.Name,
                    TwoFactorAuthentication = loan.User.TwoFactorAuthentication,
                    RoleId = loan.User.RoleId,
                };
            }

            if (loan.Item != null)
            {
                response.LoanItem = new LoanItemResponse
                {
                    Id = loan.Item.Id,
                    ItemGroupId = loan.Item.ItemGroupId,
                    RoomId = loan.Item.RoomId,
                    SerialNumber = SafeDecrypt(loan.Item.SerialNumber),
                };
            }

            return response;
        }

        private static Loan MapLoanRequestToLoan(LoanRequest loanRequest)
        {
            return new Loan
            {
                LoanDate = loanRequest.LoanDate,
                ReturnDate = loanRequest.ReturnDate,
                ItemId = loanRequest.ItemId,
                UserId = loanRequest.UserId,
            };
        }
    }
}
