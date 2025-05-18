using Godot;
using System.IO;
using System.Text.RegularExpressions;
using FileAccess = Godot.FileAccess;

[Tool]
public partial class SampleVersionNumberLabel : RichTextLabel
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        using var pluginFile = FileAccess.Open("res://addons/YarnSpinner-Godot/plugin.cfg", FileAccess.ModeFlags.Read);
        var pluginCfgText = pluginFile.GetAsText();
        var versionNumber = new Regex("version=\"(.*)\"");
        var versionMatch = versionNumber.Match(pluginCfgText);
        if (!versionMatch.Success)
        {
            return;
        }

        Text = versionMatch.Groups[1].ToString();
    }
}