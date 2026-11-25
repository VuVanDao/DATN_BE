using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace DATN_BE.Shared.Utils
{
    // Tiện ích chuyển tên tiếng Việt thành Slug URL
    public static class SlugHelper
    {
        private const string AlphaNumericChars = "abcdefghijklmnopqrstuvwxyz0123456789";

        public static string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }
            // 1. Chuẩn hóa tiếng Việt có dấu thành không dấu
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();
            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }
            var cleanText = stringBuilder.ToString().Normalize(NormalizationForm.FormC);
            // 2. Chuyển thành chữ thường
            cleanText = cleanText.ToLowerInvariant();
            // 3. Thay thế khoảng trắng và ký tự đặc biệt thành dấu gạch ngang
            cleanText = Regex.Replace(cleanText, @"[^a-z0-9\s-]", "");
            cleanText = Regex.Replace(cleanText, @"\s+", "-").Trim('-');
            
            return cleanText;
        }

        // Tạo chuỗi mã ngắn ngẫu nhiên gồm chữ thường và số (vd: "a8f3b2")
        public static string GenerateShortCode(int length = 6)
        {
            return RandomNumberGenerator.GetString(AlphaNumericChars, length);
        }
    }
}
