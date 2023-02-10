using Godot;
using System;
using Yarn.GodotIntegration;
public class SpaceSample : Node
{

    [Export] public NodePath dialogueRunnerPath;

    private DialogueRunner _dialogueRunner;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _dialogueRunner = GetNode<DialogueRunner>(dialogueRunnerPath);
        _dialogueRunner.onDialogueComplete += OnDialogueComplete;
    }

    private void OnDialogueComplete()
    {
        GD.Print("Space sample has completed!");
    }
//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
