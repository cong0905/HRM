using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace HRM.Common.Helpers
{
    public static class StringHelper
    {
        public static string RemoveAccents(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            text = text.Normalize(NormalizationForm.FormD);
            var chars = text.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
            var result = new string(chars).Normalize(NormalizationForm.FormC);
            
            // Replace đ with d
            result = result.Replace('đ', 'd').Replace('Đ', 'D');
            
            return result;
        }

        public static string GenerateUsername(string hoTen)
        {
            if (string.IsNullOrWhiteSpace(hoTen)) return "user";

            var cleanName = RemoveAccents(hoTen.Trim()).ToLower();
            var parts = cleanName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length == 0) return "user";

            // Vietnamese convention: Surname + Middle + Name
            // Request: Name + Surname
            
            string surname = parts[0];
            string name = parts[parts.Length - 1];

            return name + surname;
        }
    }
}
