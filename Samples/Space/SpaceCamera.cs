#nullable disable
using Godot;
using System;

/// <summary>
/// Simple barebones camera following script for the YarnSpinner-Godot Space sample
/// </summary>
public partial class SpaceCamera : Camera2D
{
    [Export] public Node2D followTarget;

    [Export] public int MinX;
    [Export] public int MaxX;

    private int _cameraWidth;
    public override void _Ready()
    {
        _cameraWidth = (int)((int)ProjectSettings.GetSetting("display/window/size/viewport_width") / Zoom.X);
    }

    public override void _PhysicsProcess(double delta)
    {
        var idealX = followTarget.GlobalPosition.X - _cameraWidth * 0.5f;
        idealX = Math.Min(idealX, MaxX);
        idealX = Math.Max(idealX, MinX);
        GlobalPosition = new Vector2(idealX, GlobalPosition.Y);
    }
}