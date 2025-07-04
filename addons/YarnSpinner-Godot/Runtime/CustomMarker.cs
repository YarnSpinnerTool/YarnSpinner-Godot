using Godot;

namespace YarnSpinnerGodot;

/// <summary>
/// Custom replacement marker for <see cref="MarkupPalette"/>
/// </summary>
[GlobalClass]
public partial class CustomMarker : Resource
{
    [Export] public string? Marker;
    [Export] public string? Start;
    [Export] public string? End;
    [Export] public int MarkerOffset = 0;
}