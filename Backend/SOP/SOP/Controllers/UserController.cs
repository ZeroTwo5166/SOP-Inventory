using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OtpNet;
using QRCoder;
using SOP.Archive.DTOs;
using SOP.DTOs;
using SOP.Encryption;
using SOP.Entities;
using SOP.Repositories; // ArchiveResult / DeleteResult
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;

namespace SOP.Controllers
{
    [EnableCors("CorsPolicy")]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtUtils _jwtUtils;
        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
        private readonly UrlEncoder _urlEncoder;

        public UserController(IUserRepository userRepository, IJwtUtils jwtUtils)
        {
            _userRepository = userRepository;
            _jwtUtils = jwtUtils;
            _urlEncoder = UrlEncoder.Default;
        }

        private static string? SafeDecrypt(string? v)
        {
            if (string.IsNullOrWhiteSpace(v)) return v;
            try { return EncryptionHelper.Decrypt(v); }
            catch { return v; }
        }

        [Authorize("Admin", "Drift", "Instruktør")]
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                var users = await _userRepository.GetAllAsync();
                var responses = users.Select(MapUserToUserResponse).ToList();
                return Ok(responses);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpGet("GetAllStudents")]
        public async Task<IActionResult> GetAllStudents()
        {
            try
            {
                var users = await _userRepository.GetUsersByRoleAsync(3);
                var responses = users.Select(MapUserToUserResponsePublic).ToList();
                return Ok(responses);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("2fa")]
        public async Task<IActionResult> GetTwoFactorQrCode([FromQuery] string email)
        {
            try
            {
                string encryptedEmail = EncryptionHelper.Encrypt(email);
                var user = await _userRepository.GetByEmail(encryptedEmail);
                if (user == null) return NotFound();

                string secretKey = user.TwoFactorSecretKey;
                if (string.IsNullOrEmpty(secretKey))
                {
                    var keyBytes = KeyGeneration.GenerateRandomKey(20);
                    secretKey = Base32Encoding.ToString(keyBytes);
                    user.TwoFactorSecretKey = secretKey;
                    await _userRepository.UpdateByIdAsync(user.Id, user);
                }

                string qrCodeUri = GenerateQrCodeUri(email, secretKey);
                string qrCodeBase64 = GenerateQrCodeBase64(qrCodeUri);

                return Ok(new
                {
                    email,
                    sharedKey = FormatKey(secretKey),
                    qrCodeImage = qrCodeBase64
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [AllowAnonymous]
        [HttpPost("2fa/verify")]
        public async Task<IActionResult> VerifyTwoFactorCode([FromBody] Verify2FaDto dto)
        {
            try
            {
                string encryptedEmail = EncryptionHelper.Encrypt(dto.Email);
                var user = await _userRepository.GetByEmail(encryptedEmail);
                if (user == null) return NotFound("User not found");
                if (string.IsNullOrEmpty(user.TwoFactorSecretKey)) return BadRequest("2FA not configured for this user");

                var totp = new Totp(Base32Encoding.ToBytes(user.TwoFactorSecretKey));
                bool isValid = totp.VerifyTotp(dto.Code, out _, new VerificationWindow(2, 2));
                if (!isValid) return Unauthorized("Invalid 2FA code");

                user.TwoFactorAuthentication = true;
                await _userRepository.UpdateByIdAsync(user.Id, user);

                return Ok(new
                {
                    success = true,
                    message = "2FA verified successfully",
                    token = _jwtUtils.GenerateJwtToken(user),
                    user = new
                    {
                        id = user.Id,
                        name = user.Name,
                        email = SafeDecrypt(user.Email),
                        roleId = user.RoleId,
                        twoFactorAuthentication = user.TwoFactorAuthentication
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] UserRequest userRequest)
        {
            try
            {
                string salt = BCrypt.Net.BCrypt.GenerateSalt(10);
                var newUser = new User
                {
                    Email = EncryptionHelper.Encrypt(userRequest.Email),
                    Name = userRequest.Name,
                    Password = BCrypt.Net.BCrypt.HashPassword(userRequest.Password, salt, true, BCrypt.Net.HashType.SHA512),
                    RoleId = userRequest.RoleId,
                    TwoFactorAuthentication = userRequest.TwoFactorAuthentication,
                    TwoFactorSecretKey = string.Empty,
                    ProfileImageUrl = userRequest.ProfileImageUrl
                };

                var user = await _userRepository.CreateAsync(newUser);
                return Ok(MapUserToUserResponsePublic(user));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift", "Elev")]
        [HttpGet("{Id}")]
        public async Task<IActionResult> FindByIdAsync([FromRoute] int Id)
        {
            try
            {
                var user = await _userRepository.FindByIdAsync(Id);
                if (user == null) return NotFound();
                return Ok(MapUserToUserResponse(user));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift", "Elev")]
        [HttpPut("{Id}")]
        public async Task<IActionResult> UpdateByIdAsync([FromRoute] int Id, [FromBody] UserUpdateRequest userRequest)
        {
            try
            {
                var existingUser = await _userRepository.FindByIdAsync(Id);
                if (existingUser == null) return NotFound();

                existingUser.Email = string.IsNullOrEmpty(userRequest.Email)
                    ? existingUser.Email
                    : EncryptionHelper.Encrypt(userRequest.Email);

                existingUser.Name = string.IsNullOrEmpty(userRequest.Name)
                    ? existingUser.Name
                    : userRequest.Name;

                existingUser.RoleId = userRequest.RoleId ?? existingUser.RoleId;
                existingUser.TwoFactorAuthentication = userRequest.TwoFactorAuthentication ?? existingUser.TwoFactorAuthentication;

                if (userRequest.ProfileImageUrl == "DELETE_IMAGE")
                    existingUser.ProfileImageUrl = null;
                else if (!string.IsNullOrEmpty(userRequest.ProfileImageUrl))
                    existingUser.ProfileImageUrl = userRequest.ProfileImageUrl;

                var updatedUser = await _userRepository.UpdateByIdAsync(Id, existingUser);
                if (updatedUser == null) return NotFound();
                return Ok(MapUserToUserResponse(updatedUser));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift", "Elev")]
        [HttpPut("updatePassword/{Id}")]
        public async Task<IActionResult> UpdatePasswordByIdAsync([FromRoute] int Id, [FromBody] UserRequest userRequest)
        {
            try
            {
                string salt = BCrypt.Net.BCrypt.GenerateSalt(10);
                var onlyPwd = new User
                {
                    Password = BCrypt.Net.BCrypt.HashPassword(userRequest.Password, salt, true, BCrypt.Net.HashType.SHA512),
                };

                var user = await _userRepository.UpdatePasswordByIdAsync(Id, onlyPwd);
                if (user == null) return NotFound();
                return Ok(MapUserToUserResponse(user));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        // Guarded archive: 200 when archived, 404 if not found, 409 if user has an active loan
        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpDelete("ArchiveById/{Id}")]
        public async Task<IActionResult> ArchiveByIdAsync([FromRoute] int Id, [FromBody] ArchiveNoteRequest archiveNoteRequest)
        {
            try
            {
                var encryptedNote = EncryptionHelper.Encrypt(archiveNoteRequest.ArchiveNote);
                var result = await _userRepository.ArchiveByIdAsync(Id, encryptedNote);

                return result.Status switch
                {
                    ArchiveStatus.NotFound => NotFound(),
                    ArchiveStatus.InUse => Conflict(new
                    {
                        code = "USER_IN_USE",
                        message = "User has an active loan and cannot be archived."
                    }),
                    ArchiveStatus.Archived => Ok(new Archive_UserResponse
                    {
                        Id = result.Entity!.Id,
                        DeleteTime = result.Entity.DeleteTime,
                        Email = SafeDecrypt(result.Entity.Email),
                        Name = result.Entity.Name,
                        Password = result.Entity.Password,
                        RoleId = result.Entity.RoleId,
                        TwoFactorAuthentication = result.Entity.TwoFactorAuthentication,
                        ArchiveNote = SafeDecrypt(result.Entity.ArchiveNote),
                    }),
                    _ => Problem("Unknown archive result.")
                };
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public async Task<IActionResult> SignInAsync([FromBody] SignInRequest login)
        {
            try
            {
                string encryptedEmail = EncryptionHelper.Encrypt(login.Email);
                var user = await _userRepository.GetByEmail(encryptedEmail);
                if (user == null) return Unauthorized();

                bool ok = BCrypt.Net.BCrypt.Verify(login.Password, user.Password, true, BCrypt.Net.HashType.SHA512);
                if (!ok) return Unauthorized();

                var resp = new SignInResponse
                {
                    Id = user.Id,
                    Token = _jwtUtils.GenerateJwtToken(user),
                    Role = user.Role
                };
                return Ok(resp);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        private static UserResponse MapUserToUserResponse(User user)
        {
            var response = new UserResponse
            {
                Id = user.Id,
                RoleId = user.RoleId,
                Email = SafeDecrypt(user.Email),
                Name = user.Name,
                Password = user.Password,
                TwoFactorAuthentication = user.TwoFactorAuthentication,
                ProfileImageUrl = user.ProfileImageUrl
            };

            if (user.Role != null)
            {
                response.UserRole = new UserRoleResponse
                {
                    Id = user.Role.Id,
                    Description = user.Role.Description,
                    Name = user.Role.Name,
                };
            }

            if (user.Loans != null)
            {
                response.UserLoans = user.Loans.Select(loan => new UserLoanResponse
                {
                    Id = loan.Id,
                    ItemId = loan.ItemId,
                    LoanDate = loan.LoanDate,
                    ReturnDate = loan.ReturnDate,
                    UserLoanItem = loan.Item == null ? null : new UserLoanItemResponse
                    {
                        Id = loan.Item.Id,
                        ItemGroupId = loan.Item.ItemGroupId,
                        RoomId = loan.Item.RoomId,
                        SerialNumber = SafeDecrypt(loan.Item.SerialNumber),
                        UserLoanItemItemGroup = loan.Item.ItemGroup == null ? null : new UserLoanItemItemGroupResponse
                        {
                            Id = loan.Item.ItemGroup.Id,
                            ItemTypeId = loan.Item.ItemGroup.ItemTypeId,
                            Manufacturer = SafeDecrypt(loan.Item.ItemGroup.Manufacturer),
                            ModelName = SafeDecrypt(loan.Item.ItemGroup.ModelName),
                            Price = loan.Item.ItemGroup.Price,
                            Quantity = loan.Item.ItemGroup.Quantity,
                            WarrantyPeriod = SafeDecrypt(loan.Item.ItemGroup.WarrantyPeriod)
                        }
                    }
                }).ToList();
            }

            return response;
        }

        public static UserResponse MapUserToUserResponsePublic(User user)
        {
            var response = new UserResponse
            {
                Id = user.Id,
                RoleId = user.RoleId,
                Email = SafeDecrypt(user.Email),
                Name = user.Name,
                Password = user.Password,
                ProfileImageUrl = user.ProfileImageUrl,
                TwoFactorAuthentication = user.TwoFactorAuthentication,
            };

            if (user.Role != null)
            {
                response.UserRole = new UserRoleResponse
                {
                    Id = user.Role.Id,
                    Description = user.Role.Description,
                    Name = user.Role.Name,
                };
            }

            if (user.Loans != null)
            {
                response.UserLoans = user.Loans.Select(l => new UserLoanResponse
                {
                    Id = l.Id,
                    ItemId = l.ItemId,
                    LoanDate = l.LoanDate,
                    ReturnDate = l.ReturnDate,
                }).ToList();
            }

            return response;
        }

        private string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            int i = 0;
            while (i + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.AsSpan(i, 4)).Append(' ');
                i += 4;
            }
            if (i < unformattedKey.Length)
                result.Append(unformattedKey.AsSpan(i));
            return result.ToString().ToLowerInvariant();
        }

        private string GenerateQrCodeUri(string email, string unformattedKey)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                AuthenticatorUriFormat,
                _urlEncoder.Encode("SOPInventar"),
                _urlEncoder.Encode(email),
                unformattedKey);
        }

        private string GenerateQrCodeBase64(string uri)
        {
            if (string.IsNullOrEmpty(uri)) return string.Empty;
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(5);
            return $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
        }

        [AllowAnonymous]
        [HttpPost("extend-token")]
        public IActionResult ExtendToken([FromBody] TokenRequest tokenRequest)
        {
            try
            {
                string newToken = _jwtUtils.ExtendJwtToken(tokenRequest.Token);
                return Ok(new { Token = newToken });
            }
            catch (SecurityTokenException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
    }
}
