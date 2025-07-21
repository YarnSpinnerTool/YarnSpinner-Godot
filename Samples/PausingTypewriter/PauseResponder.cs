#nullable disable
using System;
using System.Collections.Generic;
using System.Threading;
using Godot;
using Yarn.Markup;
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
    [Export] public LinePresenter linePresenter;
    private PauseEventProcessor _pauseEventProcessor = new();

    private class PauseFaceChanger : IActionMarkupHandler
    {
        private Dictionary<int, float> pauses = new();
        private Action OnPauseStart;
        private Action OnPauseEnd;

        public PauseFaceChanger(Action onPauseStart, Action onPauseEnd)
        {
            OnPauseStart = onPauseStart;
            OnPauseEnd = onPauseEnd;
        }

        public void OnLineDisplayComplete()
        {
            pauses.Clear();
        }

        public void OnPrepareForLine(MarkupParseResult line, RichTextLabel text)
        {
        }

        public void OnLineDisplayBegin(MarkupParseResult line, RichTextLabel text)
        {
            OnPauseEnd?.Invoke();
            pauses = new();
            // grabbing out any pauses inside the line
            foreach (var attribute in line.Attributes)
            {
                if (attribute.Name != "pause")
                {
                    continue;
                }

                if (attribute.Properties.TryGetValue("pause", out MarkupValue value))
                {
                    // depending on the property value we need to take a different path this is because they have made it an integer or a float which are roughly the same.
                    // But they also might have done something weird and we need to handle that
                    switch (value.Type)
                    {
                        case MarkupValueType.Integer:
                            pauses.Add(attribute.Position, value.IntegerValue);
                            break;
                        case MarkupValueType.Float:
                            pauses.Add(attribute.Position, value.FloatValue * 1000);
                            break;
                        default:
                            GD.PushWarning(
                                $"Pause property is of type {value.Type}, which is not allowed. Defaulting to one second.");
                            pauses.Add(attribute.Position, 1000);

                            break;
                    }
                }
                else
                {
                    // they haven't set a duration, so we will instead use the
                    // default of one second
                    pauses.Add(attribute.Position, 1000);
                }
            }
        }


        public async YarnTask OnCharacterWillAppear(int currentCharacterIndex, MarkupParseResult line,
            CancellationToken cancellationToken)
        {
            if (pauses.TryGetValue(currentCharacterIndex, out var duration))
            {
                OnPauseStart?.Invoke();
                await YarnTask.Delay(System.TimeSpan.FromMilliseconds(duration), cancellationToken)
                    .SuppressCancellationThrow();
                OnPauseEnd?.Invoke();
            }
        }


        public void OnLineWillDismiss()
        {
        }
    }

    private DateTime _lastTyped = DateTime.UnixEpoch;

    public override void _Ready()
    {
        if (linePresenter.IsNodeReady())
        {
            OverridePauseResponder();
        }
        else
        {
            linePresenter.Ready += OverridePauseResponder;
        }

        return;

        void OverridePauseResponder()
        {
            linePresenter.ActionMarkupHandlers.RemoveAll(h => h is PauseEventProcessor);
            linePresenter.ActionMarkupHandlers.Add(new PauseFaceChanger(OnPauseStarted, OnPauseEnded));
        }
    }


    private void OnPauseStarted()
    {
        if (!IsInstanceValid(this))
        {
            return;
        }

        face.Texture = thinkingFace;
    }

    private void OnPauseEnded()
    {
        if (!IsInstanceValid(this))
        {
            return;
        }

        face.Texture = talkingFace;
    }
}