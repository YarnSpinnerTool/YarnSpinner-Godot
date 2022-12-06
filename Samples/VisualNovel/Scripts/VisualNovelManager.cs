using Godot;
using System;
using System.Collections.Generic;
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
		_background = GetNode<TextureRect>(backgroundPath);
		_dialogueRunner = GetNode<DialogueRunner>(dialogueRunnerPath);
		_dialogueRunner.AddCommandHandler("Scene", Scene);		
		_dialogueRunner.AddCommandHandler("PlayAudio", (string streamName, float volume, string doLoop) =>
		{
			GD.Print("PlayAudio command invoked.");
			PlayAudio(streamName, volume, doLoop);
		});		_dialogueRunner.AddCommandHandler("Act", (string actor, string spriteName, string positionX, string DispositionTypeNames, string color) =>
		{
			GD.Print("Act command invoked.");
		});
		
		_dialogueRunner.StartDialogue("Start");
	}
	
	private Dictionary<string, string> bgShortNameToUUID = new Dictionary<string, string>
	{
		{"bg_office", "res://Samples/VisualNovel/Sprites/bg_office.png"}
	};
	private void Scene(string backgroundImage)
	{
		if (!bgShortNameToUUID.ContainsKey(backgroundImage))
		{
			GD.PrintErr($"The audio stream name {backgroundImage} was not defined in {nameof(bgShortNameToUUID)}");
			return;
		}

		var texture = ResourceLoader.Load<Texture2D>(bgShortNameToUUID[backgroundImage]);
		_background.Texture = texture;
	}
	private Dictionary<string, string> audioShortNameToUUID = new Dictionary<string, string>
	{
		{"music_funny", "res://Samples/VisualNovel/Sounds/music_funny.mp3"},
		{"music_romantic", "res://Samples/VisualNovel/Sounds/music_romantic.mp3"},
		{"ambient_birds", "res://Samples/VisualNovel/Sounds/ambient_birds.ogg"}
	};
	private async void PlayAudio(string streamName, float volume, string doLoop)
	{
		if (!audioShortNameToUUID.ContainsKey(streamName))
		{
			GD.PrintErr($"The audio stream name {streamName} was not defined in {nameof(audioShortNameToUUID)}");
			return;
		}
		var stream = ResourceLoader.Load<AudioStream>(audioShortNameToUUID[streamName]);
		var player = new AudioStreamPlayer2D();
		player.VolumeDb = GD.LinearToDb(volume);
		player.Stream = stream;
		AddChild(player);
		player.Play();
		if (doLoop != "loop")
		{
			await DefaultActions.Wait(stream.GetLength());
			player.Stop();
			player.QueueFree();
		}
	}
}
