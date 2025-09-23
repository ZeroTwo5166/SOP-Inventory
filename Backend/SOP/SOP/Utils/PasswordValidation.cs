using System.Text.RegularExpressions;

namespace SOP.Utils
{
    public class PasswordValidation
    {
        public static bool IsPasswordValid(string password, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(password) || password.Length < 15)
            {
                errorMessage = "Password must be at least 15 characters long.";
                return false;
            }

            if (!Regex.IsMatch(password, "[A-Z]"))
            {
                errorMessage = "Password must contain at least one uppercase letter.";
                return false;
            }

            if (!Regex.IsMatch(password, "[a-z]"))
            {
                errorMessage = "Password must contain at least one lowercase letter.";
                return false;
            }

            if (!Regex.IsMatch(password, "[0-9]"))
            {
                errorMessage = "Password must contain at least one number.";
                return false;
            }

            if (!Regex.IsMatch(password, "[!@#$%^&*(),\\-\\.\\?\":{}|<>]"))
            {
                errorMessage = "Password must contain at least one special character.";
                return false;
            }

            return true;
        }

    }
}
