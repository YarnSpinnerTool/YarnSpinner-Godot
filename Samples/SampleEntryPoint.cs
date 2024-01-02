using Godot;
using System.Collections.Generic;

/// <summary>
/// Script for the entry point /home page of the YarnSpinner-Godot
/// samples.
/// </summary>
public partial class SampleEntryPoint : CanvasLayer
{
    [Export] private Button _spaceButton;
    [Export] private Button _visualNovelButton;
    [Export] private Button _markupPaletteButton;
    [Export] private Button _pausingTypewriterButton;
    [Export] private Button _roundedViewsButton;
    [Export] private Button _gdScriptButton;

    /// <summary>
    /// Resource path to the packed scene of entry point scene
    /// </summary>
    public const string ENTRY_POINT_PATH = "res://Samples/SampleEntryPoint.tscn";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _spaceButton.Pressed += () => LoadSample(
            "res://Samples/Space/SpaceSample.tscn"
        );
        _visualNovelButton.Pressed += () => LoadSample(
            "res://Samples/VisualNovel/VisualNovelSample.tscn"
        );
        _markupPaletteButton.Pressed += () => LoadSample(
            "res://Samples/MarkupPalette/PaletteSample.tscn"
        );
        _pausingTypewriterButton.Pressed += () => LoadSample(
            "res://Samples/PausingTypewriter/PauseSample.tscn"
        );
        _roundedViewsButton.Pressed += () => LoadSample(
            "res://Samples/RoundedViews/RoundedSample.tscn"
        );
        _gdScriptButton.Pressed += () =>
            LoadSample("res://Samples/GDScript/GDScriptSample.tscn");
        _spaceButton.GrabFocus();
    }

    public void LoadSample(string samplePath)
    {
        var samplePacked = ResourceLoader.Load<PackedScene>(samplePath);
        var sample = samplePacked.Instantiate();
        GetTree().Root.CallDeferred("add_child", sample);
        QueueFree();
    }

    /// <summary>
    /// Return to the entry point
    /// </summary>
    public static void Return()
    {
        var root = ((SceneTree) Engine.GetMainLoop()).Root;
        var nodesToFree = new List<Node>();
        for (var i = 0; i < root.GetChildCount(); i++)
        {
            nodesToFree.Add(root.GetChild(i));
        }

        foreach (var node in nodesToFree)
        {
            node.QueueFree();
        }

        var loadResult = root.GetTree().ChangeSceneToFile(ENTRY_POINT_PATH);
        if (loadResult != Error.Ok)
        {
            GD.PushError($"Failed to load the sample entry point: {loadResult}");
        }
    }
}