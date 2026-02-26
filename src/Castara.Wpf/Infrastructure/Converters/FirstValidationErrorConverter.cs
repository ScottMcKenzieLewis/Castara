using System;
using System.Collections;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace Castara.Wpf.Infrastructure.Converters;

/// <summary>
/// Converts a collection of WPF validation errors to the error content of the first error,
/// typically used to display a single validation message in a tooltip or error display.
/// </summary>
/// <remarks>
/// <para>
/// This converter is designed to work with WPF's validation system, particularly when
/// binding to <c>Validation.Errors</c> on a control. It extracts the first
/// <see cref="ValidationError"/> from the collection and returns its <see cref="ValidationError.ErrorContent"/>.
/// </para>
/// <para>
/// <strong>Common Usage:</strong> Displaying validation error messages in tooltips or
/// text blocks without showing the entire collection of errors.
/// </para>
/// </remarks>
/// <example>
/// Example usage in XAML:
/// <code>
/// &lt;UserControl.Resources&gt;
///     &lt;conv:FirstValidationErrorConverter x:Key="FirstErrorConverter"/&gt;
/// &lt;/UserControl.Resources&gt;
/// 
/// &lt;!-- Display first validation error in a tooltip --&gt;
/// &lt;TextBox Text="{Binding Value, ValidatesOnDataErrors=True}"&gt;
///     &lt;TextBox.Style&gt;
///         &lt;Style TargetType="TextBox"&gt;
///             &lt;Style.Triggers&gt;
///                 &lt;Trigger Property="Validation.HasError" Value="True"&gt;
///                     &lt;Setter Property="ToolTip" 
///                             Value="{Binding RelativeSource={RelativeSource Self},
///                                             Path=(Validation.Errors),
///                                             Converter={StaticResource FirstErrorConverter}}"/&gt;
///                 &lt;/Trigger&gt;
///             &lt;/Style.Triggers&gt;
///         &lt;/Style&gt;
///     &lt;/TextBox.Style&gt;
/// &lt;/TextBox&gt;
/// </code>
/// </example>
public sealed class FirstValidationErrorConverter : IValueConverter
{
    /// <summary>
    /// Converts a collection of validation errors to the error content of the first error.
    /// </summary>
    /// <param name="value">
    /// The value to convert, expected to be an <see cref="IEnumerable"/> collection of
    /// <see cref="ValidationError"/> objects (typically a <c>ReadOnlyObservableCollection&lt;ValidationError&gt;</c>).
    /// </param>
    /// <param name="targetType">
    /// The type of the binding target property (not used in this converter).
    /// </param>
    /// <param name="parameter">
    /// Optional converter parameter (not used in this converter).
    /// </param>
    /// <param name="culture">
    /// The culture to use in the converter (not used in this converter).
    /// </param>
    /// <returns>
    /// The <see cref="ValidationError.ErrorContent"/> of the first validation error in the collection,
    /// or <c>null</c> if the collection is empty or contains no valid <see cref="ValidationError"/> objects.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The converter iterates through the provided collection and returns the <see cref="ValidationError.ErrorContent"/>
    /// of the first <see cref="ValidationError"/> it encounters. This is typically a string message
    /// describing the validation failure.
    /// </para>
    /// <para>
    /// If the collection is empty, not enumerable, or contains no <see cref="ValidationError"/> objects,
    /// the converter returns <c>null</c>.
    /// </para>
    /// </remarks>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Value is typically ReadOnlyObservableCollection<ValidationError>
        if (value is IEnumerable errors)
        {
            foreach (var item in errors)
            {
                if (item is ValidationError ve)
                    return ve.ErrorContent;
            }
        }

        return null;
    }

    /// <summary>
    /// Converts a value back from the target type to the source type.
    /// </summary>
    /// <param name="value">The value produced by the binding target.</param>
    /// <param name="targetType">The type to convert to.</param>
    /// <param name="parameter">Optional converter parameter.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>
    /// <see cref="Binding.DoNothing"/> to indicate that two-way binding is not supported.
    /// </returns>
    /// <remarks>
    /// This converter only supports one-way binding from validation errors to display content.
    /// Converting back from error content to validation errors is not meaningful in this context.
    /// </remarks>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}