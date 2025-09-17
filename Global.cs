public static class Global
{
    public static string ToTitleCase(this string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return str;
        if (str.Length == 1) return str.ToUpperInvariant();
        return char.ToUpperInvariant(str[0]) + str[1..].ToLowerInvariant();
    }
}