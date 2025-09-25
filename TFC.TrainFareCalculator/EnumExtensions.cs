using System.ComponentModel;

namespace TFC.TrainFareCalculator;

/// <summary>
/// Extension methods for Enum types.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Extension method to get the <see cref="DescriptionAttribute"/> of an enum value.
    /// </summary>
    /// <param name="value">The enum value.</param>
    /// <returns>String contained in its <see cref="DescriptionAttribute"/>. Returns <see cref="string.Empty"/> if no attribute specified.</returns>
    /// <exception cref="ArgumentException"></exception>
    public static string GetDescription(this Enum value)
    {
        var type = value.GetType();
        var name = Enum.GetName(type, value);
        if (name == null)
            throw new ArgumentException("Enum name not found.", nameof(value));

        var field = type.GetField(name);
        if (field == null)
            throw new ArgumentException("Enum field not found.", nameof(value));

        return Attribute.GetCustomAttribute(field,
            typeof(DescriptionAttribute)) is DescriptionAttribute attr
            ? attr.Description
            : string.Empty;
    }
}