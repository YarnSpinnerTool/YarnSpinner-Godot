#nullable disable
using System;
using Godot;
using YarnSpinnerGodot;

/// <summary>
/// Change a sprite when the AsyncLineView typewriter pauses. Used in the pausable typewriter sample.
/// 
/// In 0.2.*, the LineView had onPauseStarted and onPauseEnded signals.
/// Due to restructuring the Typewriter effect in AsyncLineView, those signals are no longer available.
/// A similar effect is achieved by tracking the frequency of characters being revealed.
/// </summary>
public partial class PauseResponder : Control
{
    [Export] public TextureRect face;
    [Export] public Texture2D thinkingFace;
    [Export] public Texture2D talkingFace;
    [Export] public LinePresenter lineView;

    private DateTime  _lastTyped = DateTime.UnixEpoch; 
    public override void _Ready()
    {
        lineView!.onCharacterTyped += OnCharacterTyped;
    }

    public void OnCharacterTyped()
    {
        _lastTyped = DateTime.Now;
    }

    public override void _PhysicsProcess(double delta)
    {
        if ((DateTime.Now - _lastTyped).TotalMilliseconds > 500)
        {
            OnPauseStarted();
        }
        else
        {
            OnPauseEnded();
        }
    }

    private void OnPauseStarted()
    {
        face.Texture = thinkingFace;
    }

    private void OnPauseEnded()
    {
        face.Texture = talkingFace;
    }
}