using Godot;
using System;
using YarnSpinnerGodot;

public partial class StopButton : Button
{
    [Export] public DialogueRunner DialogueRunner;

    public override void _Ready()
    {
        Pressed += StopButtonPressed;
    }

    public void StopButtonPressed()
    {
        DialogueRunner.Stop();
    }
}