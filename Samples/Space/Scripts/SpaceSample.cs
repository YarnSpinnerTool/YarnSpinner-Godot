using Godot;
using YarnSpinnerGodot;
public partial class SpaceSample : Node
{
	[Export] public Texture2D ShipHappySprite;
	[Export] public Texture2D ShipNeutralSprite;
	[Export] public  DialogueRunner dialogueRunner;
	[Export] public  Sprite2D shipFace;
	private static SpaceSample _instance;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_instance = this;
		dialogueRunner.onDialogueComplete += OnDialogueComplete;
	}

	[YarnCommand("setsprite")]
	public static void SetSprite(string character, string spriteName)
	{
		// assume ShipFace character as only one character uses this command right now 
		if (spriteName.ToLower().Equals("happy"))
		{
			_instance.shipFace.Texture = _instance.ShipHappySprite;
		} else if (spriteName.ToLower().Equals("neutral"))
		{
			_instance.shipFace.Texture = _instance.ShipNeutralSprite;
		}
	}

	private void OnDialogueComplete()
	{
		GD.Print("Space sample has completed!");
	}

}
