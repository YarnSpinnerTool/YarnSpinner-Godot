using Godot;
using System;
using YarnSpinnerGodot;

public partial class StopButton : Button
{
	[Export] public DialogueRunner DialogueRunner;
	public override void _Ready()
	{
		Connect("pressed", Callable.From(() =>
		{
			StopButtonPressed();
		}));
	}
	public void StopButtonPressed()
	{
		DialogueRunner.Stop();
	}
}
