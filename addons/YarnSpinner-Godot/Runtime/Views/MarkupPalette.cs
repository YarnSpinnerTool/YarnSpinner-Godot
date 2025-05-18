#nullable disable
using System.Collections;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace YarnSpinnerGodot;

/// <summary>
/// Represents a collection of marker names and colours.
/// </summary>
/// <remarks>
/// This is intended to be used with the LineView, and also be a sample of using the markup system.
/// </remarks>
[GlobalClass]
[Tool]
public partial class MarkupPalette : Resource
{
    /// <summary>
    /// A list containing all the color markers defined in this palette.
    /// </summary>
    [Export] public Array<FormatMarker> FormatMarkers;

    /// <summary>
    /// Determines the colour for a particular marker inside this palette.
    /// </summary>
    /// <param name="Marker">The marker you want to get a colour
    /// for.</param>
    /// <param name="colour">The colour of the marker, or <see
    /// cref="Color.black"/> if it doesn't exist in the <see
    /// cref="MarkupPalette"/>.</param>
    /// <returns><see langword="true"/> if the marker exists within this
    /// palette; <see langword="false"/> otherwise.</returns>
    public bool ColorForMarker(string Marker, out Color colour)
    {
        FormatMarkers ??= new Array<FormatMarker>(); // default to empty array
        foreach (var item in FormatMarkers)
        {
            if (item.Marker == Marker)
            {
                colour = item.Color;
                return true;
            }
        }

        colour = Colors.Black;
        return false;
    }

    /// <summary>
    /// Gets formatting information. for a particular marker inside this
    /// palette.
    /// </summary>
    /// <param name="markerName">The marker you want to get formatting
    /// information for.</param>
    /// <param name="palette">The <see cref="FormatMarker"/> for the given
    /// marker name, or a default format if a marker named <paramref
    /// name="markerName"/> was not found.</param>
    /// <returns><see langword="true"/> if the marker exists within this
    /// palette; <see langword="false"/> otherwise.</returns>
    public bool FormatForMarker(string markerName, out FormatMarker palette)
    {
        FormatMarkers ??= new Array<FormatMarker>(); // default to empty array
        foreach (var item in FormatMarkers)
        {
            if (item.Marker == markerName)
            {
                palette = item;
                return true;
            }
        }

        palette = new FormatMarker()
        {
            Color = Colors.Black,
            Boldened = false,
            Italicised = false,
            Strikedthrough = false,
            Underlined = false,
            Marker = "undefined",
        };

        return false;
    }
}