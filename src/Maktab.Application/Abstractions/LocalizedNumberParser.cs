namespace Maktab.Application.Abstractions;

public static class LocalizedNumberParser
{
    public static bool TryParseInt(string? text, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = NormalizeDigits(text.Trim());
        if (normalized.Length > 0 && normalized[0] == '-')
        {
            return false;
        }

        return int.TryParse(normalized, out value);
    }

    public static string NormalizeDigits(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return string.Concat(text.Select(character => character switch
        {
            >= '۰' and <= '۹' => (char)('0' + character - '۰'),
            >= '٠' and <= '٩' => (char)('0' + character - '٠'),
            _ => character
        }));
    }
}