using Godot;
using YarnDonut;
public class SpaceSample : Node
{
	[Export] public Texture ShipHappySprite;
	[Export] public Texture ShipNeutralSprite;
	[Export] public NodePath dialogueRunnerPath;
	private DialogueRunner _dialogueRunner;
	[Export] public NodePath shipFaceSpritePath;
	private Sprite _shipFace;
	private static SpaceSample _instance;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_instance = this;
		_dialogueRunner = GetNode<DialogueRunner>(dialogueRunnerPath);
		_dialogueRunner.onDialogueComplete += OnDialogueComplete;
		_shipFace = GetNode<Sprite>(shipFaceSpritePath);
	}

	[YarnCommand("setsprite")]
	public static void SetSprite(string character, string spriteName)
	{
		// assume ShipFace character as only one character uses this command right now 
		if (spriteName.ToLower().Equals("happy"))
		{
			_instance._shipFace.Texture = _instance.ShipHappySprite;
		} else if (spriteName.ToLower().Equals("neutral"))
		{
			_instance._shipFace.Texture = _instance.ShipNeutralSprite;
		}
	}

	private void OnDialogueComplete()
	{
		GD.Print("Space sample has completed!");
	}

}
