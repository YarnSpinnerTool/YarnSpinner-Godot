using Godot;
using System;
using Array = Godot.Collections.Array;

public partial class SampleEntryPoint : CanvasLayer
{
    [Export] private NodePath _spaceButtonPath;
    private Button _spaceButton;
    [Export] private NodePath _visualNovelButtonPath;

    private Button _visualNovelButton;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _spaceButton = GetNode<Button>(_spaceButtonPath);
        _visualNovelButton = GetNode<Button>(_visualNovelButtonPath);
        _spaceButton.Connect("pressed", Callable.From(() => LoadSample(
            "res://Samples/Space/SpaceSample.tscn"
        )));
        _visualNovelButton.Connect("pressed", Callable.From(() => LoadSample(
            "res://Samples/VisualNovel/VisualNovelSample.tscn"
        )));
        _spaceButton.GrabFocus();
    }

    public void LoadSample(string samplePath)
    {
        var samplePacked = ResourceLoader.Load<PackedScene>(samplePath);
        GetTree().Root.AddChild(samplePacked.Instantiate());
        QueueFree();
    }
}