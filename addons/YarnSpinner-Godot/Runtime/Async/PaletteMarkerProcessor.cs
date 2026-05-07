#nullable enable
using System.Collections.Generic;
using System.Text;
using Godot;
using Yarn.Markup;

namespace YarnSpinnerGodot;

/// <summary>
/// An attribute marker processor that uses a <see cref="MarkupPalette"/> to
/// apply BBCode styling tags to a line.
/// </summary>
/// <remarks>This marker processor registers itself as a handler for markers
/// whose name is equal to the name of a style in the given palette. For
/// example, if the palette defines a style named "happy", this marker processor
/// will process tags in a Yarn line named <c>[happy]</c> by inserting the
/// appropriate BBCode style tags defined for the "happy" style.</remarks>
[GlobalClass]
public partial class PaletteMarkerProcessor : ReplacementMarkupHandler
{
    /// <summary>
    /// The <see cref="MarkupPalette"/> to use when applying styles.
    /// </summary>
    [Export] public MarkupPalette? palette;

    /// <summary>
    /// The line provider to register this markup processor with.
    /// </summary>
    [Export] public LineProviderBehaviour? lineProvider;

    /// <inheritdoc/>
    /// <summary>
    /// Processes a replacement marker by applying the style from the given
    /// palette.
    /// </summary>
    /// <param name="marker">The marker to process.</param>
    /// <param name="childBuilder">A StringBuilder to build the styled text in.</param>
    /// <param name="childAttributes">An optional list of child attributes to
    /// apply</param>
    /// <param name="localeCode">The locale code to use when formatting the style.</param>
    /// <returns>A list of markup diagnostics if there are any errors, otherwise an empty list.</returns>
    public override ReplacementMarkerResult ProcessReplacementMarker(MarkupAttribute marker,
        StringBuilder childBuilder, List<MarkupAttribute> childAttributes, string localeCode)
    {
        if (palette == null)
        {
            var error = new List<LineParser.MarkupDiagnostic>
            {
                new LineParser.MarkupDiagnostic(
                    $"can't apply palette for marker {marker.Name}, because a palette was not set")
            };
            return new ReplacementMarkerResult(error, 0);
        }


        if (palette.PaletteForMarker(marker.Name, out var format))
        {
            var childrenLength = childBuilder.Length;
            childBuilder.Insert(0, format.Start);
            childBuilder.Append(format.End);

            // finally we need to know if we have to offset the markers
            // most of the time we won't have to do anything
            if (format.MarkerOffset != 0)
            {
                // we now need to move any children attributes down by however many characters were added to the front
                // this is only the case if visible glyphs were added
                // as in for example adding <b> to the front doesn't add any visible glyphs so won't need to offset anything
                // and because markers are all 0-offset relative to parents
                for (int i = 0; i < childAttributes.Count; i++)
                {
                    childAttributes[i] = childAttributes[i].Shift(format.MarkerOffset);
                }
            }

            // finally we need to calculate the number of invisible characters we added
            // which is the difference between the new and original string lengths - the total number of visible characters inserted
            // we don't care WHERE those visible characters were added, just that they were
            // we can't just use the marker offset because that only worries about visible elements added at the front of the string
            // most of the time this is just gonna be 0 anyways and you don't have to think about it
            return new ReplacementMarkerResult(childBuilder.Length - childrenLength -
                                               format.TotalVisibleCharacterCount);
        }

        List<LineParser.MarkupDiagnostic> diagnostics =
                  [new($"was unable to find a matching marker for {marker.Name}")];

        return new ReplacementMarkerResult(diagnostics, 0);
    }

    /// <summary>
    /// Called by Godot when this node is fully set up in the scene tree
    /// to register itself with <see
    /// cref="lineProvider"/>.
    /// </summary>
    public override void _Ready()
    {
        if (!IsInstanceValid(lineProvider))
        {
            lineProvider = (LineProviderBehaviour)((DialogueRunner)(DialogueRunner.FindChild(nameof(DialogueRunner))))
                .LineProvider;
        }

        if (palette == null)
        {
            GD.PushError($"No palette is set on {nameof(PaletteMarkerProcessor)}");
            return;
        }

        if (palette == null)
        {
            return;
        }

        foreach (var marker in palette.BasicMarkers)
        {
            if (string.IsNullOrEmpty(marker.Marker))
            {
                GD.PushError(
                    $"A marker is added to {nameof(MarkupPalette.BasicMarkers)} without a marker name specified.");
                continue;
            }

            lineProvider.RegisterMarkerProcessor(marker.Marker, this);
        }

        foreach (var marker in palette.CustomMarkers)
        {
            if (string.IsNullOrEmpty(marker?.Marker))
            {
                GD.PushError(
                    $"A marker is added to {nameof(MarkupPalette.CustomMarkers)} without a marker name specified.");
                continue;
            }

            lineProvider.RegisterMarkerProcessor(marker.Marker, this);
        }
    }
}