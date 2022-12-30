using System;
using System.Text;
using Godot;
using Godot.Collections;
using Yarn.Markup;
public partial class MarkupAttribute : GodotObject
{
    [Export]
    public int Position { get; private set; }
    [Export]
    public int Length { get; private set; }
    [Export]
    public string Name { get; private set; }
    [Export]
    public Dictionary Properties { get; private set; }

    public static MarkupAttribute fromMarkupAttribute(Yarn.Markup.MarkupAttribute e)
    {
        Dictionary properties = new Dictionary();
        foreach (var property in e.Properties)
        {
            properties.Add(property.Key, VariantFromMarkupValue(property.Value));
        }
        var result = new MarkupAttribute
        {
            Position = e.Position,
            Length = e.Length,
            Name = e.Name,
            Properties = properties
        };
        return result;
    }

    private static Variant VariantFromMarkupValue(MarkupValue value)
    {
        switch (value.Type)
        {
            case MarkupValueType.Integer:
                return value.IntegerValue;
            case MarkupValueType.String:
                return value.StringValue;
            case MarkupValueType.Float:
                return value.FloatValue;
            case MarkupValueType.Bool:
                return value.BoolValue;
            default:
                throw new Exception("Unknown markup value type!");
        }
    }

    public override string ToString()
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append($"[{Name}] - {Position}-{Position + Length} ({Length}");
        var properties = Properties;
        if (properties != null && properties.Count > 0)
        {
            stringBuilder.Append($", {Properties.Count} properties)");
        }

        stringBuilder.Append(")");
        return stringBuilder.ToString();
    }

}