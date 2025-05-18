#nullable disable
#if TOOLS
using System.Linq;
using Godot;

namespace YarnSpinnerGodot;

/// <summary>
/// This class is used to save the locale code associated
/// with a delete button in the <see cref="YarnProjectInspectorPlugin"/>
/// To avoid capturing variables in Callables, which can lead to an assembly unloading
/// bug.
/// </summary>
public partial class SourcePatternDeleteButton : Button
{
    /// <summary>
    /// The source pattern that will be removed from the YarnProject
    /// if this button is clicked.
    /// </summary>
    public string Pattern;

    /// <summary>
    /// Reference to the YarnProject to delete the source pattern from.
    /// </summary>
    public YarnProject Project;

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
        if (!IsInstanceValid(Project))
        {
            return;
        }

        Project.JSONProject.SourceFilePatterns =
            Project.JSONProject.SourceFilePatterns.Where(
                existingPattern =>
                    !existingPattern.Equals(Pattern));
        Project.SaveJSONProject();
        YarnSpinnerPlugin.editorInterface.GetResourceFilesystem()
            .ScanSources();
        Project.NotifyPropertyListChanged();
    }
}
#endif