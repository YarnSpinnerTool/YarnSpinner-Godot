using Godot;

namespace YarnSpinnerGodot.Editor.UI;

/// <summary>
/// Inspector UI component to delete a tag from a
/// <see cref="MarkupPalette"/>
/// </summary>
public partial class MarkupPaletteDeleteTagButton : Button
{
    /// <summary>
    /// Reference to the MarkupPalette that we are inspecting. 
    /// </summary>
    public MarkupPalette palette;

    /// <summary>
    /// The name of the tag to remove if this button is pressed.
    /// </summary>
    public string tagName;

    public override void _Ready()
    {
        Connect(BaseButton.SignalName.Pressed, Callable.From(OnPressed));
    }

    /// <summary>
    /// Handler for the Pressed signal on this button. Removes the locale code from
    /// the yarn project via the inspector plugin.
    /// </summary>
    private void OnPressed()
    {
        if (!IsInstanceValid(palette))
        {
            return;
        }

        palette.ColourMarkers.Remove(tagName);
        ResourceSaver.Save(palette, palette.ResourcePath);
        palette.NotifyPropertyListChanged();
    }
}