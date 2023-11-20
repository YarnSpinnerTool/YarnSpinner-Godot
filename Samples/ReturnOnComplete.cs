using Godot;
using YarnSpinnerGodot;

/// <summary>
/// Reloads the main menu when a sample's dialogue completes.
/// </summary>
public partial class ReturnOnComplete : Node
{
	[Export] public DialogueRunner dialogueRunner;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		dialogueRunner.onDialogueComplete += SampleEntryPoint.Return;
	}
}
