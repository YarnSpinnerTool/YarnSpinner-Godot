using Godot;

namespace YarnSpinnerGodot.Editor.UI;

/// <summary>
/// Color pickup button used in the markup palette inspector.
/// </summary>
public partial class MarkupPaletteColorButton : ColorPickerButton
{
    /// <summary>
    /// The name of the markup tag this button chooses a color for.
    /// </summary>
    public string tagName;

    /// <summary>
    /// The markup palette associated with this button.
    /// </summary>
    public MarkupPalette palette;

    public override void _Ready()
    {
        Connect(ColorPickerButton.SignalName.PopupClosed, Callable.From(OnPopupClosed));
    }

    private void OnPopupClosed()
    {
        if (!IsInstanceValid(palette))
        {
            return;
        }

        palette.ColourMarkers[tagName] = Color;
        ResourceSaver.Save(palette, palette.ResourcePath);
        palette.NotifyPropertyListChanged();
    }
}