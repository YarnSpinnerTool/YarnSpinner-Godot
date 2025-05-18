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
public partial class SourcePatternAddButton : Button
{

    /// <summary>
    /// LineEdit containing the new source pattern to add
    /// </summary>
    public LineEdit ScriptPatternInput;

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
        if (!IsInstanceValid(Project) ||
            !IsInstanceValid(ScriptPatternInput))
        {
            return;
        }

        if (string.IsNullOrEmpty(ScriptPatternInput.Text))
        {
            return;
        }

        if (Project.JSONProject.SourceFilePatterns.Contains(
                ScriptPatternInput.Text))
        {
            GD.Print(
                $"Not adding duplicate pattern '{ScriptPatternInput.Text}");
        }
        else
        {
            Project.JSONProject.SourceFilePatterns =
                Project.JSONProject.SourceFilePatterns.Append(
                    ScriptPatternInput.Text);
            Project.SaveJSONProject();
            YarnSpinnerPlugin.editorInterface.GetResourceFilesystem()
                .ScanSources();
            Project.NotifyPropertyListChanged();
        }
    }
}
#endif