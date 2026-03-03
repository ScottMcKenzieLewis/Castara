using System.Globalization;

namespace Castara.Wpf.Infrastructure.Components;

/// <summary>
/// Encapsulates text-based numeric input with parsing, formatting, and validation logic.
/// </summary>
/// <remarks>
/// This helper class provides a reusable pattern for numeric text fields that:
/// <list type="bullet">
///   <item><description>Store raw text separately from parsed numeric values</description></item>
///   <item><description>Validate parsed values against configurable rules</description></item>
///   <item><description>Generate user-friendly error messages for WPF validation</description></item>
///   <item><description>Support seeding from numeric values with consistent formatting</description></item>
/// </list>
/// This approach enables real-time validation feedback while the user types without
/// disrupting the editing experience.
/// </remarks>
public sealed class NumericTextField
{
    private readonly string _label;
    private readonly Func<double, string?> _validateNumeric;
    private readonly string _format;

    /// <summary>
    /// Initializes a new instance of the <see cref="NumericTextField"/> class.
    /// </summary>
    /// <param name="label">The display label for the field (used in error messages).</param>
    /// <param name="validateNumeric">
    /// A function that validates the parsed numeric value, returning an error message or null if valid.
    /// </param>
    /// <param name="format">The format string for converting numeric values to text (default: "0.###").</param>
    private NumericTextField(string label, Func<double, string?> validateNumeric, string format = "0.###")
    {
        _label = label;
        _validateNumeric = validateNumeric;
        _format = format;
    }

    /// <summary>
    /// Gets or sets the raw text value entered by the user.
    /// </summary>
    /// <value>
    /// The text as entered by the user, which may be empty, unparseable, or invalid.
    /// </value>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the current text represents a valid value.
    /// </summary>
    /// <value>
    /// <c>true</c> if the text is non-empty, parseable, and passes validation; otherwise, <c>false</c>.
    /// </value>
    public bool IsValid => string.IsNullOrEmpty(Error);

    /// <summary>
    /// Gets the error message for the current text value.
    /// </summary>
    /// <value>
    /// An error message describing the validation failure, or an empty string if valid.
    /// </value>
    /// <remarks>
    /// Error messages are generated for:
    /// <list type="bullet">
    ///   <item><description>Empty or whitespace-only text ("X is required.")</description></item>
    ///   <item><description>Unparseable text ("X must be a number.")</description></item>
    ///   <item><description>Out-of-range numeric values (custom message from validator)</description></item>
    /// </list>
    /// </remarks>
    public string Error
    {
        get
        {
            var raw = Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
                return $"{_label} is required.";

            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                return $"{_label} must be a number.";

            var numericError = _validateNumeric(v);
            return numericError ?? string.Empty;
        }
    }

    /// <summary>
    /// Attempts to parse and validate the current text, returning the numeric value if successful.
    /// </summary>
    /// <param name="value">
    /// When this method returns, contains the parsed and validated numeric value if successful;
    /// otherwise, contains the default value.
    /// </param>
    /// <returns>
    /// <c>true</c> if the text was successfully parsed and validated; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetValidValue(out double value)
    {
        value = default;

        var raw = Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return false;

        if (!string.IsNullOrEmpty(_validateNumeric(v) ?? string.Empty)) return false;

        value = v;
        return true;
    }

    /// <summary>
    /// Seeds the text field from a numeric value using the configured format.
    /// </summary>
    /// <param name="value">The numeric value to format and store as text.</param>
    /// <remarks>
    /// This is used during initialization and reset operations to set valid initial text
    /// without triggering validation errors.
    /// </remarks>
    public void Seed(double value)
        => Text = value.ToString(_format, CultureInfo.InvariantCulture);

    /// <summary>
    /// Creates a numeric text field with range validation.
    /// </summary>
    /// <param name="label">The display label for the field.</param>
    /// <param name="min">The minimum allowable value (inclusive).</param>
    /// <param name="max">The maximum allowable value (inclusive).</param>
    /// <returns>A configured <see cref="NumericTextField"/> with range validation.</returns>
    public static NumericTextField Range(string label, double min, double max)
        => new(label, v => (v < min || v > max)
            ? $"{label} must be between {min:0.##} and {max:0.##}."
            : null);

    /// <summary>
    /// Creates a numeric text field with minimum positive value validation.
    /// </summary>
    /// <param name="label">The display label for the field.</param>
    /// <param name="min">The minimum allowable value (must be positive).</param>
    /// <returns>A configured <see cref="NumericTextField"/> with minimum positive validation.</returns>
    public static NumericTextField MinPositive(string label, double min)
        => new(label, v =>
        {
            if (v <= 0) return $"{label} must be > 0.";
            if (v < min) return $"{label} must be >= {min:0.####}.";
            return null;
        });
    public static NumericTextField Range(string label, double min, double max, string format)
        => new(label, v => (v < min || v > max)
            ? $"{label} must be between {min:0.##} and {max:0.##}."
            : null,
            format);
}
