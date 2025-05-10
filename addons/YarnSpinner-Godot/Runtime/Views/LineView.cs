#nullable disable
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using Yarn.Markup;

#pragma warning disable CS0618 // Type or member is obsolete

namespace YarnSpinnerGodot;

/// <summary>
/// A Dialogue View that presents lines of dialogue, using Godot UI Controls
/// elements.
/// Important: When implementing this interface, your class should extend from Godot.Node.
/// Obsolete Use <see cref="LinePresenter"/>
/// </summary>
[GlobalClass]
public partial class LineView : Node, DialogueViewBase
{
    /// <summary>
    /// The Control that is the parent of all UI elements in this line view.
    /// Used to modify the transparency/visibility of the UI.
    ///
    /// We don't want to constrain DialogueViewBase to only controls (in case
    /// you wanted to make a Node2D or 3D based dialogue view), so this example
    /// LineView uses a child control called <see cref="viewControl"/> as the parent
    /// of all the UI components for this view that can be hidden. 
    /// </summary>
    /// <remarks>
    /// If <see cref="useFadeEffect"/> is true, then the alpha value of this
    /// <see cref="Control"/> will be animated during line presentation
    /// and dismissal.
    /// </remarks>
    /// <seealso cref="useFadeEffect"/>
    [Export] public NodePath viewControlPath;

    /// <summary>
    /// If enabled, matched pairs of the characters '<' and `>`  will be replaced by
    /// [ and ] respectively, so that you can write, for example, 
    /// writing <b>my text</b> in your yarn script would be converted to
    /// [b]my text[/b] at runtime to take advantage of the RichTextLabel's
    /// BBCode feature. Turning this feature on, would prevent you from using the characters
    /// '<' or '>' in your dialogue.
    /// If you need a more advanced or nuanced way to use
    /// BBCode in your yarn scripts, it's recommended to implement your own custom
    /// dialogue view. 
    /// https://docs.godotengine.org/en/stable/tutorials/ui/bbcode_in_richtextlabel.html
    /// </summary>
    [Export] public bool ConvertHTMLToBBCode;

    public Control viewControl;

    /// <summary>
    /// Controls whether the line view should fade in when lines appear, and
    /// fade out when lines disappear.
    /// </summary>
    /// <remarks><para>If this value is <see langword="true"/>, the <see
    /// cref="viewControl"/> object's alpha property will animate from 0 to
    /// 1 over the course of <see cref="fadeInTime"/> seconds when lines
    /// appear, and animate from 1 to zero over the course of <see
    /// cref="fadeOutTime"/> seconds when lines disappear.</para>
    /// <para>If this value is <see langword="false"/>, the <see
    /// cref="viewControl"/> object will appear instantaneously.</para>
    /// </remarks>
    /// <seealso cref="viewControl"/>
    /// <seealso cref="fadeInTime"/>
    /// <seealso cref="fadeOutTime"/>
    [Export] public bool useFadeEffect = true;

    /// <summary>
    /// The time that the fade effect will take to fade lines in.
    /// </summary>
    /// <remarks>This value is only used when <see cref="useFadeEffect"/> is
    /// <see langword="true"/>.</remarks>
    /// <seealso cref="useFadeEffect"/>
    [Export] public float fadeInTime = 0.25f;

    /// <summary>
    /// The time that the fade effect will take to fade lines out.
    /// </summary>
    /// <remarks>This value is only used when <see cref="useFadeEffect"/> is
    /// <see langword="true"/>.</remarks>
    /// <seealso cref="useF`adeEffect"/>
    [Export] public float fadeOutTime = 0.05f;

    public const float FrameWaitTime = 0.16f;

    /// <summary>
    /// Node path from the inspector for <see cref="lineText"/>
    /// </summary>
    [Export] public NodePath lineTextPath;

    /// <summary>
    /// The <see cref="RichTextLabel"/> object that displays the text of
    /// dialogue lines.
    /// </summary>
    public RichTextLabel lineText = null;

    /// <summary>
    /// Controls whether the <see cref="lineText"/> object will show the
    /// character name present in the line or not.
    /// </summary>
    /// <remarks>
    /// <para style="note">This value is only used if <see
    /// cref="characterNameText"/> is <see langword="null"/>.</para>
    /// <para>If this value is <see langword="true"/>, any character names
    /// present in a line will be shown in the <see cref="lineText"/>
    /// object.</para>
    /// <para>If this value is <see langword="false"/>, character names will
    /// not be shown in the <see cref="lineText"/> object.</para>
    /// </remarks>
    [Export] public bool showCharacterNameInLineView = true;

    /// <summary>
    /// The <see cref="RichTextLabel"/> object that displays the character
    /// names found in dialogue lines.
    /// </summary>
    /// <remarks>
    /// If the <see cref="LineView"/> receives a line that does not contain
    /// a character name, this object will be left blank.
    /// </remarks>
    [Export] public RichTextLabel characterNameText = null;

    /// <summary>
    /// Controls whether the text of <see cref="lineText"/> should be
    /// gradually revealed over time.
    /// </summary>
    /// <remarks><para>If this value is <see langword="true"/>, the <see
    /// cref="lineText"/> object's <see
    /// cref="RichTextLabel.VisibleRatio"/> property will animate from 0
    /// to the length of the text, at a rate of <see
    /// cref="typewriterEffectSpeed"/> letters per second when the line
    /// appears. <see cref="onCharacterTyped"/> is called for every new
    /// character that is revealed.</para>
    /// <para>If this value is <see langword="false"/>, the <see
    /// cref="lineText"/> will all be revealed at the same time.</para>
    /// <para style="note">If <see cref="useFadeEffect"/> is <see
    /// langword="true"/>, the typewriter effect will run after the fade-in
    /// is complete.</para>
    /// </remarks>
    /// <seealso cref="lineText"/>
    /// <seealso cref="onCharacterTyped"/>
    /// <seealso cref="typewriterEffectSpeed"/>
    [Export] public bool useTypewriterEffect = true;

    /// <summary>
    /// A signal that is emitted each time a character is revealed
    /// during a typewriter effect.
    /// </summary>
    /// <remarks>
    /// This event is only invoked when <see cref="useTypewriterEffect"/> is
    /// <see langword="true"/>.
    /// </remarks>
    /// <seealso cref="useTypewriterEffect"/>
    [Signal]
    public delegate void onCharacterTypedEventHandler();

    /// <summary>
    /// A signal that is called when a pause inside of the typewriter effect occurs.
    /// </summary>
    /// <remarks>
    /// This event is only invoked when <see cref="useTypewriterEffect"/> is <see langword="true"/>.
    /// </remarks>
    /// <seealso cref="useTypewriterEffect"/>
    [Signal]
    public delegate void onPauseStartedEventHandler();

    /// <summary>
    /// A signal that is called when a pause inside of the typewriter effect finishes and the typewriter has started once again.
    /// </summary>
    /// <remarks>
    /// This event is only invoked when <see cref="useTypewriterEffect"/> is <see langword="true"/>.
    /// </remarks>
    /// <seealso cref="useTypewriterEffect"/>
    [Signal]
    public delegate void onPauseEndedEventHandler();

    /// <summary>
    /// The number of characters per second that should appear during a
    /// typewriter effect.
    /// </summary>
    /// <seealso cref="useTypewriterEffect"/>
    [Export] public float typewriterEffectSpeed = 0f;

    /// <summary>
    /// The Control that represents an on-screen button that the user
    /// can click to continue to the next piece of dialogue.
    /// </summary>
    /// <remarks>
    /// <para>This Control will be disabled if it is a Button when a line begins
    /// appearing, and enabled when the line has finished appearing.</para>
    /// <para>
    /// This field will generally refer to an object that has a <see
    /// cref="Button"/> component on it that, when clicked, calls <see
    /// cref="OnContinueClicked"/>. However, if your game requires specific
    /// UI needs, you can provide any object you need, as long as it emits a signal
    /// called  when you want the dialogue to continue.</para>
    /// </remarks>
    /// <seealso cref="autoAdvance"/>
    [Export] public Control continueButton = null;

    /// <summary>
    /// The amount of time to wait after any line
    /// </summary>
    [Export] public float holdTime = 1f;

    /// <summary>
    /// Optional MarkupPalette resource
    /// </summary>
    [Export] public MarkupPalette palette;

    /// <summary>
    /// Controls whether this Line View will wait for user input before
    /// indicating that it has finished presenting a line.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If this value is true, the Line View will not report that it has
    /// finished presenting its lines. Instead, it will wait until the <see
    /// cref="UserRequestedViewAdvancement"/> method is called.
    /// </para>
    /// <para style="note"><para>The <see cref="DialogueRunner"/> will not
    /// proceed to the next piece of content (e.g. the next line, or the
    /// next options) until all Dialogue Views have reported that they have
    /// finished presenting their lines. If a <see cref="LineView"/> doesn't
    /// report that it's finished until it receives input, the <see
    /// cref="DialogueRunner"/> will end up pausing.</para>
    /// <para>
    /// This is useful for games in which you want the player to be able to
    /// read lines of dialogue at their own pace, and give them control over
    /// when to advance to the next line.</para></para>
    /// </remarks>
    [Export] public bool autoAdvance = false;

    /// <summary>
    /// The current <see cref="LocalizedLine"/> that this line view is
    /// displaying.
    /// </summary>
    LocalizedLine currentLine = null;

    /// <summary>
    /// A stop token that is used to interrupt the current animation.
    /// </summary>
    Effects.TaskInterruptToken currentStopToken = new();

    public override void _Ready()
    {
        if (lineText == null)
        {
            lineText = GetNode<RichTextLabel>(lineTextPath);
            lineText.BbcodeEnabled = true;
        }

        lineText.VisibleCharactersBehavior = TextServer.VisibleCharactersBehavior.CharsAfterShaping;

        viewControl ??= GetNode(viewControlPath) as Control;

        continueButton?.Connect("pressed", new Callable(this, nameof(OnContinueClicked)));

        SetViewAlpha(0);
        SetCanvasInteractable(false);
        if (ConvertHTMLToBBCode)
        {
            if (characterNameText != null)
            {
                characterNameText.BbcodeEnabled = true;
            }

            if (lineText != null)
            {
                lineText.BbcodeEnabled = true;
            }
        }
    }

    private void SetViewAlpha(float alpha)
    {
        if (!IsInstanceValid(viewControl)) return;
        var color = viewControl.Modulate;
        color.A = alpha;
        viewControl.Modulate = color;
        viewControl.Visible = alpha != 0f;
    }

    /// <inheritdoc/>
    public void DismissLine(Action onDismissalComplete)
    {
        currentLine = null;

        DismissLineInternal(onDismissalComplete);
    }

    private async void DismissLineInternal(Action onDismissalComplete)
    {
        SetCanvasInteractable(false);

        // If we're using a fade effect, run it, and wait for it to finish.
        if (useFadeEffect)
        {
            await Effects.FadeAlpha(viewControl, 1, 0, fadeOutTime, currentStopToken);
            currentStopToken.Complete();
        }

        SetViewAlpha(0f);
        if (onDismissalComplete != null)
        {
            onDismissalComplete();
        }
    }

    /// <inheritdoc/>
    public void InterruptLine(LocalizedLine dialogueLine, Action onInterruptLineFinished)
    {
        currentLine = dialogueLine;

        if (currentStopToken is {CanInterrupt: true})
        {
            currentStopToken.Interrupt();
        }

        // for now we are going to just immediately show everything
        // later we will make it fade in
        lineText.Visible = true;
        viewControl.Visible = true;
        if (!IsInstanceValid(characterNameText))
        {
            if (showCharacterNameInLineView)
            {
                lineText.Text = dialogueLine.Text.Text;
            }
            else
            {
                lineText.Text = dialogueLine.TextWithoutCharacterName.Text;
            }
        }
        else
        {
            characterNameText.Text = dialogueLine.CharacterName;
            lineText.Text = dialogueLine.TextWithoutCharacterName.Text;
        }

        ConvertHTMLToBBCodeIfConfigured();

        // Show the entire line's text immediately.
        lineText.VisibleRatio = 1;

        // Make the canvas group fully visible immediately, too.
        SetViewAlpha(1f);

        SetCanvasInteractable(true);

        onInterruptLineFinished();
    }

    public Action requestInterrupt { get; set; }

    /// <inheritdoc/>
    public void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
    {
        // Begin running the line asynchronously
        RunLineInternal(dialogueLine, onDialogueLineFinished)
            .ContinueWith(failedTask =>
                {
                    var errorMessage = "";
                    if (failedTask.Exception != null)
                    {
                        errorMessage = failedTask.Exception.ToString();
                    }

                    GD.PushError($"Error while running {nameof(RunLineInternal)}: {errorMessage}");
                },
                TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task RunLineInternal(LocalizedLine dialogueLine, Action onDialogueLineFinished)
    {
        async Task PresentLine()
        {
            lineText.Visible = true;
            SetCanvasInteractable(true);
            SetViewAlpha(1f);

            // Hide the continue button until presentation is complete (if
            // we have one).
            if (continueButton != null)
            {
                continueButton.Visible = false;
                if (continueButton is Button button)
                {
                    button.Disabled = true;
                }
                else if (continueButton is TextureButton textureButton)
                {
                    textureButton.Disabled = true;
                }
            }

            MarkupParseResult text = dialogueLine.TextWithoutCharacterName;
            if (IsInstanceValid(characterNameText))
            {
                // we are set up to show a character name, but there isn't one
                // so just hide the container
                if (string.IsNullOrWhiteSpace(dialogueLine.CharacterName))
                {
                    characterNameText.Visible = false;
                }
                else
                {
                    // we have a character name text view, show the character name
                    characterNameText.Text = dialogueLine.CharacterName;
                    characterNameText.Visible = true;
                }
            }
            else
            {
                // We don't have a character name text view. Should we show
                // the character name in the main text view?
                if (showCharacterNameInLineView)
                {
                    // Yep! Show the entire text.
                    text = dialogueLine.Text;
                }
            }

            // if we have a palette file need to add those colours into the text
            if (IsInstanceValid(palette))
            {
                lineText.Text = LineView.PaletteMarkedUpText(text, palette);
            }
            else
            {
                lineText.Text = text.Text;
            }

            ConvertHTMLToBBCodeIfConfigured();
            if (useTypewriterEffect)
            {
                // If we're using the typewriter effect, hide all of the
                // text before we begin any possible fade (so we don't fade
                // in on visible text).
                lineText.VisibleRatio = 0;
            }
            else
            {
                // Show all characters
                lineText.VisibleRatio = 1;
            }

            // If we're using the fade effect, start it, and wait for it to
            // finish.
            if (useFadeEffect)
            {
                await Effects.FadeAlpha(viewControl, 0, 1, fadeInTime, currentStopToken);
                if (currentStopToken.WasInterrupted)
                {
                    // The fade effect was interrupted. Stop this entire
                    // task.
                    return;
                }
            }

            // If we're using the typewriter effect, start it, and wait for
            // it to finish.
            if (useTypewriterEffect)
            {
                var pauses = LineView.GetPauseDurationsInsideLine(text);
                // setting the canvas all back to its defaults because if we didn't also fade we don't have anything visible
                SetViewAlpha(1f);
                SetCanvasInteractable(true);
                await Effects.PausableTypewriter(
                    lineText,
                    typewriterEffectSpeed,
                    () => EmitSignal(SignalName.onCharacterTyped),
                    () => EmitSignal(SignalName.onPauseStarted),
                    () => EmitSignal(SignalName.onPauseEnded),
                    pauses,
                    currentStopToken
                );
            }
        }

        currentLine = dialogueLine;

        // Run any presentations as a single async Task. If this is stopped,
        // which UserRequestedViewAdvancement can do, then we will stop all
        // the animations at once.
        await PresentLine();

        if (!IsInstanceValid(this))
        {
            return;
        }

        currentStopToken.Complete();

        // All of our text should now be visible.
        lineText.VisibleRatio = 1;

        // Our view should at be at full opacity.
        SetViewAlpha(1f);
        SetCanvasInteractable(true);

        // Show the continue button, if we have one.
        if (continueButton != null)
        {
            if (continueButton is BaseButton button)
            {
                button.Disabled = false;
            }

            continueButton.Visible = true;
            continueButton.GrabFocus();
        }

        // If we have a hold time, wait that amount of time, and then
        // continue.
        if (holdTime > 0)
        {
            await DefaultActions.Wait(holdTime);
        }

        if (autoAdvance == false)
        {
            // The line is now fully visible, and we've been asked to not
            // auto-advance to the next line. Stop here, and don't call the
            // completion handler - we'll wait for a call to
            // UserRequestedViewAdvancement, which will interrupt this
            // Task.
            return;
        }

        // Our presentation is complete; call the completion handler.
        onDialogueLineFinished();
    }

    /// <summary>
    /// If <see cref="ConvertHTMLToBBCode"/> is true, replace any HTML tags in the line text and
    /// character name text with BBCode tags.
    /// </summary>
    private void ConvertHTMLToBBCodeIfConfigured()
    {
        if (ConvertHTMLToBBCode)
        {
            const string htmlTagPattern = @"<(.*?)>";
            if (characterNameText != null)
            {
                characterNameText.Text = Regex.Replace(characterNameText.Text, htmlTagPattern, "[$1]");
            }

            lineText.Text = Regex.Replace(lineText.Text, htmlTagPattern, "[$1]");
        }
    }

    private void SetCanvasInteractable(bool b)
    {
        if (!IsInstanceValid(viewControl))
        {
            return;
        }

        viewControl.MouseFilter = b ? Control.MouseFilterEnum.Pass : Control.MouseFilterEnum.Ignore;
        viewControl.SetProcessInput(b);
    }

    /// <inheritdoc/>
    public void UserRequestedViewAdvancement()
    {
        // We received a request to advance the view. If we're in the middle of
        // an animation, skip to the end of it. If we're not current in an
        // animation, interrupt the line so we can skip to the next one.

        // we have no line, so the user just mashed randomly
        if (currentLine == null)
        {
            return;
        }

        // we may want to change this later so the interrupted
        // animation Task is what actually interrupts
        // for now this is fine.
        // Is an animation running that we can stop?
        if (currentStopToken.CanInterrupt)
        {
            // Stop the current animation, and skip to the end of whatever
            // started it.
            currentStopToken.Interrupt();
            return;
        }

        // No animation is now running. Signal that we want to
        // interrupt the line instead.
        requestInterrupt?.Invoke();
    }

    /// <summary>
    /// Called when the <see cref="continueButton"/> is clicked.
    /// </summary>
    public void OnContinueClicked()
    {
        // When the Continue button is clicked, we'll do the same thing as
        // if we'd received a signal from any other part of the game (for
        // example, if a DialogueAdvanceInput had signalled us.)
        UserRequestedViewAdvancement();
    }

    /// <inheritdoc />
    public void DialogueComplete()
    {
        // do we still have a line lying around?
        if (currentLine != null)
        {
            currentLine = null;
            DismissLineInternal(null);
        }
    }

    /// <summary>
    /// Applies the <paramref name="palette"/> to the line based on it's markup.
    /// </summary>
    /// <remarks>
    /// This is static so that other dialogue views can reuse this code.
    /// While this is simplistic it is useful enough that multiple pieces might well want it.
    /// </remarks>
    /// <param name="line">The parsed marked up line with it's attributes.</param>
    /// <param name="palette">The palette mapping attributes to colours.</param>
    /// <returns>A TMP formatted string with the palette markup values injected within.</returns>
    public static string PaletteMarkedUpText(Yarn.Markup.MarkupParseResult line, MarkupPalette palette)
    {
        string lineOfText = line.Text;
        line.Attributes.Sort((a, b) => (b.Position.CompareTo(a.Position)));
        foreach (var attribute in line.Attributes)
        {
            // we have a colour that matches the current marker
            Color markerColour;
            if (palette.ColorForMarker(attribute.Name, out markerColour))
            {
                // we use the range on the marker to insert the TMP <color> tags
                // not the best approach but will work ok for this use case
                lineOfText = lineOfText.Insert(attribute.Position + attribute.Length, "[/color]");
                lineOfText = lineOfText.Insert(attribute.Position, $"[color=#{markerColour.ToHtml()}]");
            }
        }

        return lineOfText;
    }

    /// <summary>
    /// Creates a stack of typewriter pauses to use to temporarily halt the typewriter effect.
    /// </summary>
    /// <remarks>
    /// This is intended to be used in conjunction with the <see cref="Effects.PausableTypewriter"/> effect.
    /// The stack of tuples created are how the typewriter effect knows when, and for how long, to halt the effect.
    /// <para>
    /// The pause duration property is in milliseconds but all the effects code assumes seconds
    /// So here we will be dividing it by 1000 to make sure they interconnect correctly.
    /// </para>
    /// </remarks>
    /// <param name="line">The line from which we covet the pauses</param>
    /// <returns>A stack of positions and duration pause tuples from within the line</returns>
    public static Stack<(int position, float duration)> GetPauseDurationsInsideLine(
        Yarn.Markup.MarkupParseResult line)
    {
        var pausePositions = new Stack<(int, float)>();
        var label = "pause";

        // sorting all the attributes in reverse positional order
        // this is so we can build the stack up in the right positioning
        var attributes = line.Attributes;
        attributes.Sort((a, b) => (b.Position.CompareTo(a.Position)));
        foreach (var attribute in line.Attributes)
        {
            // if we aren't a pause skip it
            if (attribute.Name != label)
            {
                continue;
            }

            // did they set a custom duration or not, as in did they do this:
            //     Alice: this is my line with a [pause = 1000 /]pause in the middle
            // or did they go:
            //     Alice: this is my line with a [pause /]pause in the middle
            if (attribute.Properties.TryGetValue(label, out Yarn.Markup.MarkupValue value))
            {
                // depending on the property value we need to take a different path
                // this is because they have made it an integer or a float which are roughly the same
                // note to self: integer and float really ought to be convertible...
                // but they also might have done something weird and we need to handle that
                switch (value.Type)
                {
                    case Yarn.Markup.MarkupValueType.Integer:
                        float duration = value.IntegerValue;
                        pausePositions.Push((attribute.Position, duration / 1000));
                        break;
                    case Yarn.Markup.MarkupValueType.Float:
                        pausePositions.Push((attribute.Position, value.FloatValue / 1000));
                        break;
                    default:
                        GD.PrintErr(
                            $"Pause property is of type {value.Type}, which is not allowed. Defaulting to one second.");
                        pausePositions.Push((attribute.Position, 1));
                        break;
                }
            }
            else
            {
                // they haven't set a duration, so we will instead use the default of one second
                pausePositions.Push((attribute.Position, 1));
            }
        }

        return pausePositions;
    }
}