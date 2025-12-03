using System.Collections.Generic;
using System.Text;
using Godot;
using Yarn.Markup;

#nullable disable
namespace YarnSpinnerGodot;

/// <summary>
/// Example of a custom ReplacementMarkupHandler which defines tags
/// that will be replaced with custom content before being sent
/// to presenters.
/// </summary>
public partial class ImageMarkupProcessor : ReplacementMarkupHandler
{
    [Export] public LineProviderBehaviour LineProvider;

    // example of having simple text replacements of markup tags
    private static readonly Dictionary<string, string> _aliasToSpritePath =
        new()
        {
            // you can use custom markers in a MarkupPalette, or make your own ReplacementMarkupHandler like this script.
            ["rabbit"] = "[img=60x60]res://Samples/Markup/images/rabbit.png[/img]",
        };

    public override void _Ready()
    {
        if (!IsInstanceValid(LineProvider))
        {
            GD.PushError($"No {nameof(LineProvider)} is set on this {nameof(ImageMarkupProcessor)}");
            return;
        }

        foreach (var marker in _aliasToSpritePath.Keys)
        {
            LineProvider.RegisterMarkerProcessor(marker, this);
        }


        LineProvider.RegisterMarkerProcessor("img", this);
    }

    public override ReplacementMarkerResult ProcessReplacementMarker(MarkupAttribute marker,
        StringBuilder childBuilder, List<MarkupAttribute> childAttributes,
        string localeCode)
    {
        if (_aliasToSpritePath.TryGetValue(marker.Name, out var value))
        {
            // replace with <image tag> 
            childBuilder.Insert(0, value);
            return new ReplacementMarkerResult(value.Length);
        }

        if (marker.Name == "img")
        {
            if (!marker.TryGetProperty("path", out MarkupValue imagePath))
            {
                var error = new List<LineParser.MarkupDiagnostic>
                {
                    new LineParser.MarkupDiagnostic("No path attribute specified for img markup tag.")
                };
                return new ReplacementMarkerResult(error, 0);
            }

            var widthString = "";
            if (marker.Properties.TryGetValue("width", out var widthProperty))
            {
                widthString = $"={widthProperty}";
            }

            var heightString = "";
            if (marker.Properties.TryGetValue("height", out var heightProperty))
            {
                heightString = $"x{heightProperty}";
            }

            var argsString = $"{widthString}{heightString}";

            // generic image markup
            var finalString = $"[img{argsString}]res://Samples/Markup/images/{imagePath.StringValue}[/img]";
            childBuilder.Insert(0, finalString);
            return new ReplacementMarkerResult(finalString.Length);

        }

        return new ReplacementMarkerResult();
    }
}