using Godot;

namespace YarnSpinnerGodot.Editor.UI;

/// <summary>
/// Inspector UI component for adding new markup tags
/// to a <see cref="MarkupPalette"/>
/// </summary>
public partial class MarkupPaletteAddTagButton : Button
{
    /// <summary>
    ///  Text entry which contains the name of the new tag to add to
    /// the markup palette.
    /// </summary>
    public LineEdit newTagNameInput;

    /// <summary>
    /// Reference to the MarkupPalette that we are inspecting. 
    /// </summary>
    public MarkupPalette palette;

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
        if (!IsInstanceValid(palette) ||
            !IsInstanceValid(newTagNameInput))
        {
            return;
        }

        var newTagName = newTagNameInput.Text?
            .Replace("[", "").Replace("]", "");
        if (string.IsNullOrEmpty(newTagName))
        {
            // button should be enabled, but just in case
            GD.Print(
                "Enter a markup tag name in order to add a color mapping.");
            return;
        }

        palette.ColourMarkers.Add(newTagName, Colors.Black);
        palette.NotifyPropertyListChanged();
    }
}