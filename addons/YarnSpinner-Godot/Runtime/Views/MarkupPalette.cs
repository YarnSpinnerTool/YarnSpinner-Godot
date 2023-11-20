using System.Collections;
using System.Collections.Generic;
using Godot;

namespace YarnSpinnerGodot
{
    /// <summary>
    /// Represents a collection of marker names and colours.
    /// </summary>
    /// <remarks>
    /// This is intended to be used with the LineView, and also be a sample of using the markup system.
    /// </remarks>
    [GlobalClass][Tool]
    public partial class MarkupPalette : Resource
    {

        /// <summary>
        /// The collection of colour markers inside this
        /// </summary>
        [Export] public Godot.Collections.Dictionary<string, Color> ColourMarkers = new();

        /// <summary>
        /// Determines the colour for a particular marker inside this palette.
        /// </summary>
        /// <param name="Marker">The marker of which you are covetous of it's colour.</param>
        /// <param name="colour">The colour of the marker, or black if it doesn't exist.</param>
        /// <returns>True if the marker exists within this epalette.</returns>
        public bool ColorForMarker(string Marker, out Color colour)
        {
            foreach (var item in ColourMarkers)
            {
                if (item.Key == Marker)
                {
                    colour = item.Value;
                    return true;
                }
            }
            colour = Colors.Black;
            return false;
        }
    }
}
