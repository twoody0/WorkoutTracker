using System.Globalization;
using System.Text;

namespace WorkoutTracker.Helpers;

public static class InputSanitizer
{
    public const double MaxBodyWeight = 700;
    public const double MaxWorkoutWeight = 2000;
    public const int MaxReps = 100;
    public const int MaxSets = 20;
    public const int MaxDurationMinutes = 480;
    public const double MaxDistanceMiles = 100;
    public const int MaxSteps = 50000;
    public const double MaxResistanceAdjustment = 300;
    public const int MaxExerciseNameLength = 100;
    public const int MaxMuscleGroupLength = 40;

    public static string SanitizeName(string? value, int maxLength = MaxExerciseNameLength) =>
        NormalizeText(value, maxLength);

    public static string SanitizeMuscleGroup(string? value, int maxLength = MaxMuscleGroupLength) =>
        NormalizeText(value, maxLength);

    public static string SanitizePositiveIntegerText(string? value, int maxValue)
    {
        var cleaned = ExtractNumericCharacters(value, allowDecimal: false, allowNegative: false);
        if (string.IsNullOrEmpty(cleaned))
        {
            return string.Empty;
        }

        if (!int.TryParse(cleaned, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            parsed = maxValue;
        }

        return Math.Clamp(parsed, 0, maxValue).ToString(CultureInfo.InvariantCulture);
    }

    public static string SanitizeSignedIntegerText(string? value, int minValue, int maxValue)
    {
        var cleaned = ExtractNumericCharacters(value, allowDecimal: false, allowNegative: true);
        if (string.IsNullOrEmpty(cleaned) || cleaned == "-")
        {
            return string.Empty;
        }

        if (!int.TryParse(cleaned, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            parsed = cleaned.StartsWith("-", StringComparison.Ordinal) ? minValue : maxValue;
        }

        return Math.Clamp(parsed, minValue, maxValue).ToString(CultureInfo.InvariantCulture);
    }

    public static string SanitizePositiveDecimalText(string? value, double maxValue, int decimals = 1)
    {
        var cleaned = ExtractNumericCharacters(value, allowDecimal: true, allowNegative: false);
        if (string.IsNullOrEmpty(cleaned))
        {
            return string.Empty;
        }

        if (cleaned == ".")
        {
            return cleaned;
        }

        if (cleaned.EndsWith(".", StringComparison.Ordinal) &&
            double.TryParse(cleaned[..^1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var partialWhole))
        {
            var clampedWhole = Math.Clamp(partialWhole, 0, maxValue);
            return $"{clampedWhole.ToString("0", CultureInfo.InvariantCulture)}.";
        }

        if (!double.TryParse(cleaned, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed))
        {
            parsed = maxValue;
        }

        return FormatClampedDecimal(parsed, 0, maxValue, decimals);
    }

    public static string SanitizeSignedDecimalText(string? value, double minValue, double maxValue, int decimals = 0)
    {
        var cleaned = ExtractNumericCharacters(value, allowDecimal: true, allowNegative: true);
        if (string.IsNullOrEmpty(cleaned) || cleaned == "-" || cleaned == ".")
        {
            return string.Empty;
        }

        if (!double.TryParse(cleaned, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed))
        {
            parsed = cleaned.StartsWith("-", StringComparison.Ordinal) ? minValue : maxValue;
        }

        return FormatClampedDecimal(parsed, minValue, maxValue, decimals);
    }

    public static bool TryParseBodyWeight(string? value, out double bodyWeight)
    {
        bodyWeight = 0;
        var sanitized = SanitizePositiveDecimalText(value, MaxBodyWeight);
        return !string.IsNullOrWhiteSpace(sanitized) &&
               double.TryParse(sanitized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out bodyWeight) &&
               bodyWeight > 0;
    }

    public static string NormalizeText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var collapsed = string.Join(" ", value
            .Trim()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        return collapsed.Length <= maxLength
            ? collapsed
            : collapsed[..maxLength].TrimEnd();
    }

    private static string ExtractNumericCharacters(string? value, bool allowDecimal, bool allowNegative)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var hasDecimal = false;

        foreach (var character in value.Trim())
        {
            if (char.IsDigit(character))
            {
                builder.Append(character);
                continue;
            }

            if (allowDecimal && (character == '.' || character == ',') && !hasDecimal)
            {
                builder.Append('.');
                hasDecimal = true;
                continue;
            }

            if (allowNegative && character == '-' && builder.Length == 0)
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static string FormatClampedDecimal(double value, double minValue, double maxValue, int decimals)
    {
        var clamped = Math.Clamp(value, minValue, maxValue);
        var rounded = Math.Round(clamped, decimals, MidpointRounding.AwayFromZero);
        var format = decimals <= 0 ? "0" : $"0.{new string('#', decimals)}";
        return rounded.ToString(format, CultureInfo.InvariantCulture);
    }
}
