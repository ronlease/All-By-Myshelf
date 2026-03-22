namespace AllByMyshelf.Api.Infrastructure;

/// <summary>
/// Provides input sanitization and validation utilities for user-entered data.
/// </summary>
public static class InputSanitizer
{
    /// <summary>
    /// Escapes LIKE pattern wildcards (%, _, \) for safe use in SQL LIKE queries.
    /// </summary>
    /// <param name="input">The user input to escape.</param>
    /// <returns>The escaped string with %, _, and \ prefixed by backslash.</returns>
    public static string EscapeLikePattern(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return input
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");
    }

    /// <summary>
    /// Redacts a sensitive value for logging by showing the first few characters and masking the rest.
    /// </summary>
    /// <param name="input">The sensitive value to redact.</param>
    /// <param name="visibleChars">The number of characters to show (default: 4).</param>
    /// <returns>A redacted string showing the first N chars + "***", or "[empty]" if null/whitespace.</returns>
    public static string Redact(string? input, int visibleChars = 4)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "[empty]";

        if (input.Length <= visibleChars)
            return "***";

        return $"{input[..visibleChars]}***";
    }

    /// <summary>
    /// Sanitizes user input by trimming, stripping control characters, and enforcing a maximum length.
    /// </summary>
    /// <param name="input">The user input to sanitize.</param>
    /// <param name="maxLength">The maximum allowed length (default: 2000).</param>
    /// <param name="preserveNewlines">Whether to preserve newline characters (default: false).</param>
    /// <returns>A sanitized string, or empty string if input is null/whitespace.</returns>
    public static string Sanitize(string? input, int maxLength = 2000, bool preserveNewlines = false)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var trimmed = input.Trim();

        // Strip control characters (char < 32) except \n and \r when preserveNewlines is true
        var sanitized = new System.Text.StringBuilder(trimmed.Length);
        foreach (var c in trimmed)
        {
            if (c < 32)
            {
                if (preserveNewlines && (c == '\n' || c == '\r'))
                    sanitized.Append(c);
                // otherwise skip control char
            }
            else
            {
                sanitized.Append(c);
            }
        }

        var result = sanitized.ToString();

        // Truncate to maxLength
        if (result.Length > maxLength)
            result = result[..maxLength];

        return result;
    }

    /// <summary>
    /// Splits a comma-separated string of names, trims each, and removes empty entries.
    /// </summary>
    /// <param name="input">The comma-separated input string.</param>
    /// <returns>A list of trimmed, non-empty names.</returns>
    public static List<string> SplitNames(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return [];

        return input
            .Split(',')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }
}
