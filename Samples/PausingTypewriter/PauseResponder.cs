using Godot;
using YarnSpinnerGodot;

public partial class PauseResponder : Control
{
    [Export] public TextureRect face;
    [Export] public Texture2D thinkingFace;
    [Export] public Texture2D talkingFace;
    [Export] public LineView lineView;

    public override void _Ready()
    {
        lineView.onPauseStarted += OnPauseStarted;
        lineView.onPauseEnded += OnPauseEnded;
    }

    public void OnPauseStarted()
    {
        face.Texture = thinkingFace;
    }

    public void OnPauseEnded()
    {
        face.Texture = talkingFace;
    }
}