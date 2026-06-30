namespace Web.Helpers;

public static class CpfFormatter
{
    public static string Format(string? value)
    {
        var digits = new string((value ?? "").Where(char.IsDigit).Take(11).ToArray());

        return digits.Length switch
        {
            <= 3 => digits,
            <= 6 => $"{digits[..3]}.{digits[3..]}",
            <= 9 => $"{digits[..3]}.{digits[3..6]}.{digits[6..]}",
            _ => $"{digits[..3]}.{digits[3..6]}.{digits[6..9]}-{digits[9..]}"
        };
    }

    public static string Strip(string? value) =>
        new string((value ?? "").Where(char.IsDigit).ToArray());
}
