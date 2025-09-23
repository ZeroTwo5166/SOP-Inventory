using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using OtpNet;
using QRCoder;
using SOP.Archive.DTOs;
using SOP.DTOs;
using SOP.Encryption;
using SOP.Entities;
using SOP.Utils;
using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using static QRCoder.PayloadGenerator;

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

        // Initializes the controller with the address repository
        public UserController(IUserRepository userRepository, IJwtUtils jwtUtils)
        {
            _userRepository = userRepository;
            _jwtUtils = jwtUtils;
            _urlEncoder = UrlEncoder.Default;
        }

        [Authorize("Admin", "Drift", "Instruktør")]
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                List<User> user = await _userRepository.GetAllAsync();

                List<UserResponse> userResponses = user.Select(
                    user => MapUserToUserResponse(user)).ToList();
                return Ok(userResponses);
            }
            catch (Exception ex)
            {

                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [AllowAnonymous]
        [HttpGet]
        [Route("GetAllStudents")]
        public async Task<IActionResult> GetAllStudents()
        {
            try
            {
                List<User> users = await _userRepository.GetUsersByRoleAsync(3);

                List<UserResponse> userResponses = users.Select(user => MapUserToUserResponsePublic(user)).ToList();

                return Ok(userResponses);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("2fa")]
        public async Task<IActionResult> GetTwoFactorQrCode([FromQuery] string email)
        {
            try
            {

                string encryptedEmail = EncryptionHelper.Encrypt(email);

                var user = await _userRepository.GetByEmail(encryptedEmail);
                if (user == null)
                {
                    return NotFound(); //Status Code 404
                }

                // If user already has a secret, don't send anything sensitive
                if (!string.IsNullOrEmpty(user.TwoFactorSecretKey))
                {
                    return Ok(new { twoFactorEnabled = true });
                }

                string secretKey = user.TwoFactorSecretKey;
                if (string.IsNullOrEmpty(secretKey))
                {
                    // Generate new 2FA secret key
                    var keyBytes = KeyGeneration.GenerateRandomKey(20);
                    secretKey = Base32Encoding.ToString(keyBytes);

                    user.TwoFactorSecretKey = secretKey;
                    await _userRepository.UpdateByIdAsync(user.Id, user); // Save to DB
                }

                // Step 2: Build QR code URI
                string qrCodeUri = GenerateQrCodeUri(email, secretKey);

                // Step 3: Convert URI to base64 image
                string qrCodeBase64 = GenerateQrCodeBase64(qrCodeUri);

                // Step 4: Return QR image and shared key (for manual entry fallback)
                return Ok(new
                {
                    email = email,
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
        [HttpPost]
        [Route("2fa/verify")]
        public async Task<IActionResult> VerifyTwoFactorCode([FromBody] Verify2FaDto dto)
        {
            try
            {
                string encryptedEmail = EncryptionHelper.Encrypt(dto.Email);
                var user = await _userRepository.GetByEmail(encryptedEmail);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                if (string.IsNullOrEmpty(user.TwoFactorSecretKey))
                {
                    return BadRequest("2FA not configured for this user");
                }

                var totp = new Totp(Base32Encoding.ToBytes(user.TwoFactorSecretKey));
                bool isValid = totp.VerifyTotp(dto.Code, out long timeStepMatched, new VerificationWindow(2, 2));

                if (!isValid)
                {
                    return Unauthorized("Invalid 2FA code");
                }

                // Optional: Mark 2FA as completed/enabled
                user.TwoFactorAuthentication = true;
                await _userRepository.UpdateByIdAsync(user.Id, user);

                //return Ok(new { success = true, message = "2FA verified successfully" });
                return Ok(new
                {
                    success = true,
                    message = "2FA verified successfully",
                    token = _jwtUtils.GenerateJwtToken(user),
                    user = new
                    {
                        id = user.Id,
                        name = user.Name,
                        email = EncryptionHelper.Decrypt(user.Email),
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


        private string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            int currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
                currentPosition += 4;
            }
            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.AsSpan(currentPosition));
            }

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
            if (string.IsNullOrEmpty(uri))
            {
                return string.Empty; // Handle the null case
            }

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(5);
            return $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
        }

        [Authorize("Admin", "Instruktør", "Drift","Elev")]
        [HttpGet]
        [Route("GetUsersByRoleId/{Id}")]
        public async Task<IActionResult> GetUsersByRole([FromRoute] int Id)
        {
            try
            {
                List<User> users = await _userRepository.GetUsersByRoleAsync(Id);

                List<UserResponse> userResponses = users.Select(user => new UserResponse
                {
                    Id = user.Id,
                    RoleId = user.RoleId,
                    Email = EncryptionHelper.Decrypt(user.Email),
                    Name = user.Name
                }).ToList();

                return Ok(userResponses);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] UserRequest userRequest)
        {
            try
            {
                // ✅ Validate password before doing anything else
                if (!PasswordValidation.IsPasswordValid(userRequest.Password, out string errorMessage))
                {
                    return BadRequest(new { message = errorMessage });
                }


                string salt = BCrypt.Net.BCrypt.GenerateSalt(10);

                User newUser = new User
                {
                    Email = EncryptionHelper.Encrypt(userRequest.Email),
                    Name = userRequest.Name,
                    Password = BCrypt.Net.BCrypt.HashPassword(userRequest.Password, salt, true, BCrypt.Net.HashType.SHA512),
                    RoleId = userRequest.RoleId,
                    TwoFactorAuthentication = userRequest.TwoFactorAuthentication,
                    TwoFactorSecretKey = string.Empty,
                    ProfileImageUrl = userRequest.ProfileImageUrl // ✅ Store Cloudinary image URL

                };


                var user = await _userRepository.CreateAsync(newUser);

                UserResponse userResponse = MapUserToUserResponsePublic(user);

                return Ok(userResponse);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift", "Elev")]
        [HttpGet]
        [Route("{Id}")]
        public async Task<IActionResult> FindByIdAsync([FromRoute] int Id)
        {
            try
            {
                var user = await _userRepository.FindByIdAsync(Id);

                if (user == null)
                {
                    return NotFound(); //Status Code 404
                }

                return Ok(MapUserToUserResponse(user));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift", "Elev")]
        [HttpPut]
        [Route("{Id}")]
        public async Task<IActionResult> UpdateByIdAsync([FromRoute] int Id, [FromBody] UserUpdateRequest userRequest)
        {
            try
            {
                var existingUser = await _userRepository.FindByIdAsync(Id);
                if (existingUser == null) return NotFound();

                Console.WriteLine($"Received ProfileImageUrl: {userRequest.ProfileImageUrl}");


                // Update only if new value is provided, otherwise keep existing
                existingUser.Email = string.IsNullOrEmpty(userRequest.Email)
                    ? existingUser.Email
                    : EncryptionHelper.Encrypt(userRequest.Email);

                existingUser.Name = string.IsNullOrEmpty(userRequest.Name)
                    ? existingUser.Name
                    : userRequest.Name;

                existingUser.RoleId = userRequest.RoleId ?? existingUser.RoleId;
                existingUser.TwoFactorAuthentication = userRequest.TwoFactorAuthentication ?? existingUser.TwoFactorAuthentication;

                // Handle ProfileImageUrl with special deletion value
                if (userRequest.ProfileImageUrl == "DELETE_IMAGE")
                {
                    existingUser.ProfileImageUrl = null;
                }
                else if (!string.IsNullOrEmpty(userRequest.ProfileImageUrl))
                {
                    existingUser.ProfileImageUrl = userRequest.ProfileImageUrl;
                }
                // If ProfileImageUrl is null or empty (but not "DELETE_IMAGE"), keep existing value

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
        [HttpPut]
        [Route("updatePassword/{Id}")]
        public async Task<IActionResult> UpdatePasswordByIdAsync([FromRoute] int Id, [FromBody] UserRequest userRequest)
        {
            try
            {
                string salt = BCrypt.Net.BCrypt.GenerateSalt(10);

                User updateUserdPassword = new User
                {
                    // https://github.com/BcryptNet/bcrypt.net/issues/15
                    // The 3rd parameter is enhanced entropy which runs the password through SHA384 before using BCrypt hashing. Very secure!!!
                    // Double encryption!!
                    // Hashes the password from the request
                    Password = BCrypt.Net.BCrypt.HashPassword(userRequest.Password, salt, true, BCrypt.Net.HashType.SHA512),
                };


                var user = await _userRepository.UpdatePasswordByIdAsync(Id, updateUserdPassword);

                if (user == null)
                {
                    return NotFound();
                }

                return Ok(MapUserToUserResponse(user));
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [Authorize("Admin", "Instruktør", "Drift")]
        [HttpDelete]
        [Route("ArchiveById/{Id}")]
        public async Task<IActionResult> ArchiveByIdAsync([FromRoute] int Id, [FromBody] ArchiveNoteRequest archiveNoteRequest)
        {
            try
            {
                string archiveNote = archiveNoteRequest.ArchiveNote;
                var user = await _userRepository.ArchiveByIdAsync(Id, archiveNote);
                if (user == null)
                {
                    return NotFound();
                }

                Archive_UserResponse response = new Archive_UserResponse
                {
                    Id = user.Id,
                    DeleteTime = user.DeleteTime,
                    Email = EncryptionHelper.Decrypt(user.Email),
                    Name = user.Name,
                    Password = user.Password,
                    RoleId = user.RoleId,
                    TwoFactorAuthentication = user.TwoFactorAuthentication,
                    ArchiveNote = user.ArchiveNote,
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("authenticate")]
        public async Task<IActionResult> SignInAsync([FromBody] SignInRequest login)
        {
            try
            {
                // Encrypt the email before searching!
                string encryptedEmail = EncryptionHelper.Encrypt(login.Email);

                User user = await _userRepository.GetByEmail(encryptedEmail);
                if (user == null)
                {
                    return Unauthorized();
                }

                // BCrypt verification
                bool PasswordCheck = BCrypt.Net.BCrypt.Verify(login.Password, user.Password, true, BCrypt.Net.HashType.SHA512);

                if (PasswordCheck)
                {
                    SignInResponse SignInResponse = new()
                    {
                        Id = user.Id,
                        Token = _jwtUtils.GenerateJwtToken(user)
                    };
                    if (user.Role != null)
                    {
                        SignInResponse.Role = user.Role;
                    }
                    return Ok(SignInResponse);
                }

                return Unauthorized();
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        private static UserResponse MapUserToUserResponse(User user)
        {
            UserResponse response = new UserResponse
            {
                Id = user.Id,
                RoleId = user.RoleId,
                Email = EncryptionHelper.Decrypt(user.Email),
                Name = user.Name,
                Password = user.Password,
                TwoFactorAuthentication = user.TwoFactorAuthentication,
                ProfileImageUrl = user.ProfileImageUrl,
                UserRole = new UserRoleResponse
                {
                    Id = user.Role.Id,
                    Description = user.Role.Description,
                    Name = user.Role.Name,
                },
                UserLoans = user.Loans.Select(loan => new UserLoanResponse
                {
                    Id = loan.Id,
                    ItemId = loan.ItemId,
                    LoanDate = loan.LoanDate,
                    ReturnDate = loan.ReturnDate,
                    UserLoanItem = new UserLoanItemResponse
                    {
                        Id = loan.Item.Id,
                        ItemGroupId = loan.Item.ItemGroupId,
                        RoomId = loan.Item.RoomId,
                        SerialNumber = loan.Item.SerialNumber,
                        UserLoanItemItemGroup = new UserLoanItemItemGroupResponse
                        {
                            Id = loan.Item.ItemGroup.Id,
                            ItemTypeId = loan.Item.ItemGroup.ItemTypeId,
                            Manufacturer = loan.Item.ItemGroup.Manufacturer,
                            ModelName = loan.Item.ItemGroup.ModelName,
                            Price = loan.Item.ItemGroup.Price,
                            Quantity = loan.Item.ItemGroup.Quantity,
                            WarrantyPeriod = loan.Item.ItemGroup.WarrantyPeriod
                        }
                    }
                }).ToList()
            };
            return response;
        }


        private static User MapUserRequestToUser(UserRequest userRequest)
        {
            return new User
            {
                RoleId = userRequest.RoleId,
                Email = userRequest.Email,
                Name = userRequest.Name,
                Password = userRequest.Password,
                TwoFactorAuthentication = userRequest.TwoFactorAuthentication,
            };
        }

        public static UserResponse MapUserToUserResponsePublic(User user)
        {
            UserResponse response = new UserResponse
            {
                Id = user.Id,
                RoleId = user.RoleId,
                Email = EncryptionHelper.Decrypt(user.Email),
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

            // Er ikke helt sikker på det her. Skal den navigere hele vejen igennem ItemGroup?
            if (user.Loans != null)
            {
                response.UserLoans = user.Loans.Select(LoanItems => new UserLoanResponse
                {
                    Id = LoanItems.Id,
                    ItemId = LoanItems.ItemId,
                    LoanDate = LoanItems.LoanDate,
                    ReturnDate = LoanItems.ReturnDate,
                }).ToList();
            }

            return response;
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("extend-token")]
        public async Task<IActionResult> ExtendToken([FromBody] TokenRequest tokenRequest)
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