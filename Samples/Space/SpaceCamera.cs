using Godot;
using System;

/// <summary>
/// Simple barebones camera following script for the YarnDonut Space sample
/// </summary>
public class SpaceCamera : Camera2D
{
    [Export] public NodePath followTargetPath;

    [Export] public int MinX;
    [Export] public int MaxX;
    private Node2D _followTarget;

    private int _cameraWidth;
    public override void _Ready()
    {
        _followTarget = GetNode<Node2D>(followTargetPath);
        _cameraWidth = (int)((int)ProjectSettings.GetSetting("display/window/size/width") / Zoom.x);
    }

    public override void _PhysicsProcess(float delta)
    {
        var idealX = _followTarget.GlobalPosition.x - _cameraWidth / 2;
        idealX = Math.Min(idealX, MaxX);
        idealX = Math.Max(idealX, MinX);
        GlobalPosition = new Vector2(idealX, GlobalPosition.y);
    }
}