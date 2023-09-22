using Godot;
using System;

/// <summary>
/// Simple barebones camera following script for the YarnSpinner-Godot Space sample
/// </summary>
public partial class SpaceCamera : Camera2D
{
    [Export] public NodePath followTargetPath;

    [Export] public int MinX;
    [Export] public int MaxX;
    private Node2D _followTarget;

    private int _cameraWidth;
    public override void _Ready()
    {
        _followTarget = GetNode<Node2D>(followTargetPath);
        _cameraWidth = (int)((int)ProjectSettings.GetSetting("display/window/size/viewport_width") / Zoom.X);
    }

    public override void _PhysicsProcess(double delta)
    {
        var idealX = _followTarget.GlobalPosition.X - _cameraWidth * 0.5f;
        idealX = Math.Min(idealX, MaxX);
        idealX = Math.Max(idealX, MinX);
        GlobalPosition = new Vector2(idealX, GlobalPosition.Y);
    }
}