using System;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace PaletixDesktop.Tools
{
    public static class InputValidation
    {
        private static readonly Regex PhonePattern = new(@"^\+?[0-9][0-9\s().-]{5,24}$", RegexOptions.Compiled);
        private static readonly Regex ReferencePattern = new(@"^[A-Za-z0-9][A-Za-z0-9._/-]{0,49}$", RegexOptions.Compiled);

        public static bool IsValidEmail(string value)
        {
            var normalized = value.Trim();
            if (normalized.Contains(' ') || !normalized.Contains('@'))
            {
                return false;
            }

            try
            {
                var address = new MailAddress(normalized);
                return string.Equals(address.Address, normalized, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsValidPhone(string value, int maxLength)
        {
            var normalized = value.Trim();
            var digits = normalized.Count(char.IsDigit);
            return normalized.Length <= maxLength
                && digits is >= 6 and <= 15
                && PhonePattern.IsMatch(normalized);
        }

        public static bool IsValidHttpUrl(string value)
        {
            var normalized = value.Trim();
            return normalized.Length > 0
                && !normalized.Any(char.IsWhiteSpace)
                && Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                && !string.IsNullOrWhiteSpace(uri.Host);
        }

        public static bool IsValidImageUrl(string value)
        {
            var normalized = value.Trim();
            return IsValidHttpUrl(normalized)
                || normalized.StartsWith("ms-appx:///", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsValidReference(string value)
        {
            return ReferencePattern.IsMatch(value.Trim());
        }

        public static bool TryParseDecimal(string value, out decimal result)
        {
            var normalized = value.Trim();
            return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out result)
                || decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.GetCultureInfo("ca-ES"), out result)
                || decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.GetCultureInfo("es-ES"), out result);
        }

        public static decimal ParseDecimalOrDefault(string value, decimal fallback = 0m)
        {
            return TryParseDecimal(value, out var result) ? result : fallback;
        }

        public static bool IsValidSpanishTaxId(string value)
        {
            var normalized = value
                .Trim()
                .Replace(" ", "", StringComparison.Ordinal)
                .Replace("-", "", StringComparison.Ordinal)
                .ToUpperInvariant();

            return IsValidSpanishTaxIdFormat(normalized);
        }

        public static bool IsValidSpanishTaxIdFormat(string value)
        {
            var normalized = value
                .Trim()
                .Replace(" ", "", StringComparison.Ordinal)
                .Replace("-", "", StringComparison.Ordinal)
                .ToUpperInvariant();

            return Regex.IsMatch(normalized, @"^[0-9]{8}[A-Z]$")
                || Regex.IsMatch(normalized, @"^[XYZ][0-9]{7}[A-Z]$")
                || Regex.IsMatch(normalized, @"^[ABCDEFGHJKLMNPQRSUVW][0-9]{7}[0-9A-J]$");
        }

        private static bool IsValidDni(string value)
        {
            if (value.Length != 9 || !value[..8].All(char.IsDigit) || !char.IsLetter(value[8]))
            {
                return false;
            }

            const string letters = "TRWAGMYFPDXBNJZSQVHLCKE";
            var number = int.Parse(value[..8], CultureInfo.InvariantCulture);
            return value[8] == letters[number % 23];
        }

        private static bool IsValidNie(string value)
        {
            if (value.Length != 9 || !"XYZ".Contains(value[0]) || !value[1..8].All(char.IsDigit) || !char.IsLetter(value[8]))
            {
                return false;
            }

            var prefix = value[0] switch
            {
                'X' => '0',
                'Y' => '1',
                _ => '2'
            };

            return IsValidDni(prefix + value[1..]);
        }

        private static bool IsValidCif(string value)
        {
            if (value.Length != 9 ||
                !"ABCDEFGHJKLMNPQRSUVW".Contains(value[0]) ||
                !value[1..8].All(char.IsDigit) ||
                !char.IsLetterOrDigit(value[8]))
            {
                return false;
            }

            var digits = value.Substring(1, 7).Select(character => character - '0').ToArray();
            var evenSum = digits[1] + digits[3] + digits[5];
            var oddSum = 0;
            foreach (var index in new[] { 0, 2, 4, 6 })
            {
                var doubled = digits[index] * 2;
                oddSum += doubled / 10 + doubled % 10;
            }

            var controlDigit = (10 - ((evenSum + oddSum) % 10)) % 10;
            var controlLetter = "JABCDEFGHI"[controlDigit];
            var control = value[8];

            return value[0] switch
            {
                'A' or 'B' or 'E' or 'H' => control == (char)('0' + controlDigit),
                'K' or 'P' or 'Q' or 'S' => control == controlLetter,
                _ => control == (char)('0' + controlDigit) || control == controlLetter
            };
        }
    }
}
