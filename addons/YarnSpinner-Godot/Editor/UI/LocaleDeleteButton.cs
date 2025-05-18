#nullable disable
#if TOOLS
using Godot;

namespace YarnSpinnerGodot;

/// <summary>
/// This class is used to save the locale code associated
/// with a delete button in the <see cref="YarnProjectInspectorPlugin"/>
/// To avoid capturing variables in Callables, which can lead to an assembly unloading
/// bug.
/// </summary>
public partial class LocaleDeleteButton : Button
{
    /// <summary>
    /// The locale that will be removed from the YarnProject
    /// if this button is clicked.
    /// </summary>
    public string LocaleCode;

    /// <summary>
    /// Reference to the plugin to allow us to remove the locale. 
    /// </summary>
    public YarnProjectInspectorPlugin Plugin;

    /// <summary>
    /// Handler for the Pressed signal on this button. Removes the locale code from
    /// the yarn project via the inspector plugin.
    /// </summary>
    public void OnPressed()
    {
        if (IsInstanceValid(Plugin))
        {
            Plugin.RemoveLocale(LocaleCode);
        }
    }
}
#endif