using System.Text.RegularExpressions;

namespace HRM.Common.Helpers;

public static class PasswordPolicy
{
    public static bool IsValid(string password, out string reason)
    {
        reason = string.Empty;
        if (string.IsNullOrEmpty(password) || password.Length < 8)
        {
            reason = "Mật khẩu phải có ít nhất 8 ký tự.";
            return false;
        }

        if (!Regex.IsMatch(password, "[A-Z]"))
        {
            reason = "Mật khẩu phải chứa ít nhất một chữ hoa.";
            return false;
        }

        if (!Regex.IsMatch(password, "[a-z]"))
        {
            reason = "Mật khẩu phải chứa ít nhất một chữ thường.";
            return false;
        }

        if (!Regex.IsMatch(password, "[0-9]"))
        {
            reason = "Mật khẩu phải chứa ít nhất một chữ số.";
            return false;
        }

        if (!Regex.IsMatch(password, "[!@#\\$%\\^&\\*]"))
        {
            reason = "Mật khẩu nên chứa ít nhất một ký tự đặc biệt (!@#$%^&*).";
            return false;
        }

        return true;
    }
}
