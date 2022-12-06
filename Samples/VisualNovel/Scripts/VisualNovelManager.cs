using Godot;
using System;
using System.Net.Mime;
using Yarn.GodotIntegration;

public partial class VisualNovelManager : Node
{

	[Export] private NodePath dialogueRunnerPath;
	[Export] private NodePath backgroundPath;

	private DialogueRunner _dialogueRunner;
	private TextureRect _background;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_dialogueRunner = GetNode<DialogueRunner>(dialogueRunnerPath);
		_dialogueRunner.AddCommandHandler("Scene", (string backgroundImage) =>
		{
			GD.Print("Scene command invoked.");
		});		
		_dialogueRunner.AddCommandHandler("PlayAudio", (string streamName, float volume, string doLoop) =>
		{
			GD.Print("PlayAudio command invoked.");
		});		_dialogueRunner.AddCommandHandler("Act", (string actor, string spriteName, string positionX, string DispositionTypeNames, string color) =>
		{
			GD.Print("PlayAudio command invoked.");
		});
		
		_dialogueRunner.StartDialogue("Start");
	}

}
