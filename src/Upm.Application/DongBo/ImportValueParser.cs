using System.Globalization;

namespace Upm.Application.DongBo;

/// <summary>Đọc giá trị import độc lập culture; cả dấu phẩy và dấu chấm đều là dấu thập phân.</summary>
public static class ImportValueParser
{
    public static bool TryDecimal(string? value, out decimal result)
    {
        var normalized = value?.Trim().Replace(',', '.') ?? string.Empty;
        return decimal.TryParse(normalized, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out result);
    }

    public static bool TryDate(string? value, out DateOnly result)
    {
        if (DateOnly.TryParse(value, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out result)) return true;
        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)) return true;
        if (double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var serial) && serial is >= 20000 and <= 80000)
        {
            result = DateOnly.FromDateTime(new DateTime(1899, 12, 30, 0, 0, 0, DateTimeKind.Utc).AddDays(serial));
            return true;
        }
        result = default;
        return false;
    }
}
