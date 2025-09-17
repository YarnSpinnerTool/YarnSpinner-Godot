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
    [Export] public Array<BasicMarker> BasicMarkers = [];

    [Export] public Array<CustomMarker> CustomMarkers = [];

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
        foreach (var item in BasicMarkers)
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

    public bool PaletteForMarker(string markerName, out CustomMarker palette)
    {
        // we first check if we have a marker of that name in the basic markers
        foreach (var item in BasicMarkers)
        {
            if (item.Marker == markerName)
            {
                System.Text.StringBuilder front = new();
                System.Text.StringBuilder back = new();
                var closingTags = new Stack<string>();

                // do we have a custom colour set?
                if (item.CustomColor)
                {
                    front.AppendFormat("[color=#{0}]", item.Color.ToHtml());
                    closingTags.Push("[/color]");
                }

                // do we need to bold it?
                if (item.Boldened)
                {
                    front.Append("[b]");
                    closingTags.Push("[/b]");
                }

                // do we need to italicise it?
                if (item.Italicised)
                {
                    front.Append("[i]");
                    closingTags.Push("[/i]");
                }

                // do we need to underline it?
                if (item.Underlined)
                {
                    front.Append("[u]");
                    closingTags.Push("[/u]");
                }

                // do we need to strikethrough it?
                if (item.Strikedthrough)
                {
                    front.Append("[s]");
                    closingTags.Push("[/s]");
                }

                while (closingTags.Count > 0)
                {
                    back.Append(closingTags.Pop());
                }

                palette = new CustomMarker()
                {
                    Marker = item.Marker,
                    Start = front.ToString(),
                    End = back.ToString(),
                    MarkerOffset = 0,
                };
                return true;
            }
        }

        // we now check if we have one in the format markers
        foreach (var item in CustomMarkers)
        {
            if (item.Marker == markerName)
            {
                palette = item;
                return true;
            }
        }

        // we don't have anything for this marker
        // so we return false and a default marker
        palette = new();
        return false;
    }
}