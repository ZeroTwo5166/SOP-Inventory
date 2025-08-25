namespace SOP.Authorization
{
    public interface IJwtUtils
    {
        // Method to generate JWT token for a user
        string GenerateJwtToken(User user);

        // Method to validate a JWT token and return user ID
        int? ValidateJwtToken(string token);

        // Method to extend the JWT token expiration
        string ExtendJwtToken(string token);
    }

    public class JwtUtils : IJwtUtils
    {
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;

        // Constructor to inject configuration and user repository
        public JwtUtils(IConfiguration configuration, IUserRepository userRepository)
        {
            _configuration = configuration;
            _userRepository = userRepository;
        }

        public string GenerateJwtToken(User user)
        {
            // Create a token handler to generate the JWT
            JwtSecurityTokenHandler tokenHandler = new();

            // Get the secret key from appsettings
            byte[] key = Encoding.ASCII.GetBytes(_configuration.GetSection("Secret").Value);

            // Define the token descriptor with user claims and expiration time
            SecurityTokenDescriptor tokenDescriptor = new()
            {
                Subject = new ClaimsIdentity(new[] { new Claim("Id", user.Id.ToString()) }),
                Expires = DateTime.UtcNow.AddSeconds(30), // Token valid for 30 minutes
                SigningCredentials = new(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            // Create and return the token as a string
            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public int? ValidateJwtToken(string token)
        {
            // Return null if token is null
            if (token == null)
            {
                return null;
            }

            // Create a token handler for validation
            JwtSecurityTokenHandler tokenHandler = new();

            // Get the secret key from appsettings for validation
            byte[] key = Encoding.ASCII.GetBytes(_configuration.GetSection("Secret").Value);

            try
            {
                // Validate the token with the secret key and settings
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero // Tokens expire exactly at expiration time
                }, out SecurityToken validatedToken);

                // Extract and return the user ID from the validated token
                JwtSecurityToken jwtToken = (JwtSecurityToken)validatedToken;
                int userId = int.Parse(jwtToken.Claims.First(x => x.Type == "Id").Value);
                return userId;
            }
            catch (Exception)
            {
                // Return null if validation fails
                return null;
            }
        }

        public string ExtendJwtToken(string token)
        {
            // Use a token handler and key as usual.
            JwtSecurityTokenHandler tokenHandler = new();
            byte[] key = Encoding.ASCII.GetBytes(_configuration.GetSection("Secret").Value);

            // Define validation parameters, but skip lifetime validation.
            TokenValidationParameters validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false, // <--- disable lifetime checks for extension
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                // Validate token ignoring lifetime
                tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                // Obtain userId as in your implementation.
                JwtSecurityToken jwtToken = (JwtSecurityToken)validatedToken;
                int userId = int.Parse(jwtToken.Claims.First(x => x.Type == "Id").Value);

                // Retrieve user asynchronously (consider using await instead of .Result)
                var userTask = _userRepository.FindByIdAsync(userId);
                userTask.Wait();
                User user = userTask.Result;
                if (user == null)
                {
                    throw new SecurityTokenException("User not found");
                }

                // Generate new token (with proper expiration, e.g. 30 minutes).
                return GenerateJwtToken(user);
            }
            catch (Exception ex)
            {
                throw new SecurityTokenException("Invalid token", ex);
            }
        }
    }
}