using System.ComponentModel;

namespace TFC.TrainFareCalculator;

public static class EnumExtensions
{
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