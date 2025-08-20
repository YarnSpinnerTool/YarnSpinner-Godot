using Godot;

namespace YarnSpinnerGodot.Samples;

public partial class ReturnToMenuButton : Button
{
    public override void _Ready()
    {
        Pressed += SampleEntryPoint.Return;
    }
}