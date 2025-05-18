#nullable disable
using Godot;

namespace YarnSpinnerGodot;

/// <summary>
/// Contains information describing the formatting style of text within
/// a named marker.
/// </summary>
[GlobalClass]
public partial class FormatMarker : Resource
{
    /// <summary>
    /// The name of the marker which can be used in text to indicate
    /// specific formatting.
    /// </summary>
    [Export] public string Marker;

    /// <summary>
    /// The color to use for text associated with this marker.
    /// </summary>
    [Export] public Color Color = Colors.Black;

    /// <summary>
    /// Indicates whether the text associated with this marker should be
    /// bolded.
    /// </summary>
    [Export] public bool Boldened;

    /// <summary>
    /// Indicates whether the text associated with this marker should be
    /// italicized.
    /// </summary>
    [Export] public bool Italicised;

    /// <summary>
    /// Indicates whether the text associated with this marker should be
    /// underlined.
    /// </summary>
    [Export] public bool Underlined;

    /// <summary>
    /// Indicates whether the text associated with this marker should
    /// have a strikethrough effect.
    /// </summary>
    [Export] public bool Strikedthrough;
}