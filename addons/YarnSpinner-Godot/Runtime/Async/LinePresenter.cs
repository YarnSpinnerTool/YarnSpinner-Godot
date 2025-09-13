/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Godot;
using Godot.Collections;
using Yarn.Markup;

#nullable enable


namespace YarnSpinnerGodot;

/// <summary>
/// A Dialogue Presenter that presents lines of dialogue, using Godot UI
/// elements.
/// </summary>
[GlobalClass]
public partial class LinePresenter : Node, DialoguePresenterBase
{
    [Export] public DialogueRunner? dialogueRunner;

    /// <summary>
    /// The Control that contains the UI elements used by this Line
    /// Presenter.
    /// </summary>
    /// <remarks>
    /// If <see cref="useFadeEffect"/> is true, then the alpha value of this
    /// <see cref="CanvasGroup"/> will be animated during line presentation
    /// and dismissal.
    /// </remarks>
    /// <seealso cref="useFadeEffect"/>
    [Export] public Control? presenterControl;

    /// <summary>
    /// The <see cref="RichTextLabel"/> object that displays the text of
    /// dialogue lines.
    /// </summary>
    [Export] public RichTextLabel? lineText;


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
    [Export] public RichTextLabel? characterNameText;

    /// <summary>
    /// The game object that holds the <see cref="characterNameText"/> text
    /// field.
    /// </summary>
    /// <remarks>
    /// This is needed in situations where the character name is contained
    /// within an entirely different game object. Most of the time this will
    /// just be the same game object as <see cref="characterNameText"/>.
    /// </remarks>
    [Export] public CanvasItem? characterNameContainer = null;


    /// <summary>
    /// Controls whether the line presenter should fade in when lines appear, and
    /// fade out when lines disappear.
    /// </summary>
    /// <remarks><para>If this value is <see langword="true"/>, the <see
    /// cref="presenterControl"/> object's alpha property will animate from 0 to
    /// 1 over the course of <see cref="fadeUpDuration"/> seconds when lines
    /// appear, and animate from 1 to zero over the course of <see
    /// cref="fadeDownDuration"/> seconds when lines disappear.</para>
    /// <para>If this value is <see langword="false"/>, the <see
    /// cref="presenterControl"/> object will appear instantaneously.</para>
    /// </remarks>
    /// <seealso cref="presenterControl"/>
    /// <seealso cref="fadeUpDuration"/>
    /// <seealso cref="fadeDownDuration"/>
    [Export] public bool useFadeEffect = true;

    /// <summary>
    /// The time that the fade effect will take to fade lines in.
    /// </summary>
    /// <remarks>This value is only used when <see cref="useFadeEffect"/> is
    /// <see langword="true"/>.</remarks>
    /// <seealso cref="useFadeEffect"/>
    [Export] public float fadeUpDuration = 0.25f;

    /// <summary>
    /// The time that the fade effect will take to fade lines out.
    /// </summary>
    /// <remarks>This value is only used when <see cref="useFadeEffect"/> is
    /// <see langword="true"/>.</remarks>
    /// <seealso cref="useFadeEffect"/>
    [Export] public float fadeDownDuration = 0.1f;


    /// <summary>
    /// Controls whether this Line Presenter will automatically to the Dialogue
    /// Runner that the line is complete as soon as the line has finished
    /// appearing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If this value is true, the Line Presenter will 
    /// </para>
    /// <para style="note"><para>The <see cref="DialogueRunner"/> will not
    /// proceed to the next piece of content (e.g. the next line, or the
    /// next options) until all Dialogue Presenters have reported that they have
    /// finished presenting their lines. If a <see cref="LinePresenter"/>
    /// doesn't report that it's finished until it receives input, the <see
    /// cref="DialogueRunner"/> will end up pausing.</para>
    /// <para>
    /// This is useful for games in which you want the player to be able to
    /// read lines of dialogue at their own pace, and give them control over
    /// when to advance to the next line.</para></para>
    /// </remarks>
    [Export] public bool autoAdvance = false;

    /// <summary>
    /// The amount of time after the line finishes appearing before
    /// automatically ending the line, in seconds.
    /// </summary>
    /// <remarks>This value is only used when <see cref="autoAdvance"/> is
    /// <see langword="true"/>.</remarks>
    [Export] public float autoAdvanceDelay = 1f;


    // typewriter fields

    /// <summary>
    /// Controls whether the text of <see cref="lineText"/> should be
    /// gradually revealed over time.
    /// </summary>
    /// <remarks><para>If this value is <see langword="true"/>, the <see
    /// cref="lineText"/> object's <see
    /// cref="RichTextLabel.maxVisibleCharacters"/> property will animate from 0
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
    /// The number of characters per second that should appear during a
    /// typewriter effect.
    /// </summary>
    [Export] public int typewriterEffectSpeed = 60;

    /// <summary>
    /// If enabled, matched pairs of the characters '<' and `>`  will be replaced by
    /// [ and ] respectively, so that you can write, for example, 
    /// writing <b>my text</b> in your yarn script would be converted to
    /// [b]my text[/b] at runtime to take advantage of the RichTextLabel's
    /// BBCode feature. Turning this feature on, would prevent you from using the characters
    /// '<' or '>' in your dialogue.
    /// If you need a more advanced or nuanced way to use
    /// BBCode in your yarn scripts, it's recommended to implement your own custom
    /// Dialogue Presenter. 
    /// https://docs.godotengine.org/en/stable/tutorials/ui/bbcode_in_richtextlabel.html
    /// </summary>
    [Export] public bool ConvertHTMLToBBCode;

    /// <summary>
    /// A list of <see cref="ActionMarkupHandler"/> objects that will be
    /// used to handle markers in the line.
    /// </summary>
    [Export] Array<ActionMarkupHandler> eventHandlers = [];

    public const string HtmlTagPattern = @"<(.*?)>";


    /// <inheritdoc/>
    public YarnTask OnDialogueCompleteAsync()
    {
        if (IsInstanceValid(presenterControl))
        {
            presenterControl!.Visible = false;
        }

        return YarnTask.CompletedTask;
    }

    public List<IActionMarkupHandler> ActionMarkupHandlers { get; } = [];

    /// <inheritdoc/>
    public YarnTask OnDialogueStartedAsync()
    {
        return YarnTask.CompletedTask;
    }


    public override void _Ready()
    {
        if (IsInstanceValid(presenterControl))
        {
            presenterControl!.Visible = false;
        }

        if (useTypewriterEffect)
        {
            // need to add a pause handler also
            // and add it to the front of the list
            // that way it always happens first
            var pauser = new PauseEventProcessor();
            ActionMarkupHandlers.Insert(0, pauser);
        }

        if (IsInstanceValid(lineText))
        {
            lineText.VisibleCharactersBehavior = TextServer.VisibleCharactersBehavior.CharsAfterShaping;
        }

        if (characterNameContainer == null && characterNameText != null)
        {
            characterNameContainer = characterNameText;
        }

        // we add all the Godot Node-derived handlers into the shared list
        ActionMarkupHandlers.AddRange(eventHandlers);
    }

    /// <summary>Presents a line using the configured text view.</summary>
    /// <inheritdoc cref="DialoguePresenterBase.RunLineAsync(LocalizedLine, LineCancellationToken)" path="/param"/>
    /// <inheritdoc cref="DialoguePresenterBase.RunLineAsync(LocalizedLine, LineCancellationToken)" path="/returns"/>
    public async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        if (lineText == null)
        {
            GD.PushError($"Line Presenter does not have a text view. Skipping line {line.TextID} (\"{line.RawText}\")");
            return;
        }

        MarkupParseResult text;

        // configuring the text fields
        if (showCharacterNameInLineView)
        {
            if (characterNameText == null)
            {
                GD.PushWarning(
                    $"{nameof(LinePresenter)} is configured to show character names, but no character name text view was provided.",
                    this);
            }
            else
            {
                characterNameText.Text = line.CharacterName;
            }

            text = line.TextWithoutCharacterName;

            if (line.Text.TryGetAttributeWithName("character", out var characterAttribute))
            {
                text.Attributes.Add(characterAttribute);
            }
        }
        else
        {
            // we don't want to show character names but do have a valid container for showing them
            // so we should just disable that and continue as if it didn't exist
            if (IsInstanceValid(characterNameContainer))
            {
                characterNameContainer!.Visible = false;
            }

            text = line.TextWithoutCharacterName;
        }

        lineText.Text = text.Text;
        
        lineText.VisibleRatio = 0;
        // letting every action markup handler know that fade up (if set) is about to begin
        foreach (var processor in ActionMarkupHandlers)
        {
            processor.OnPrepareForLine(text, lineText);
        }

        if (IsInstanceValid(presenterControl))
        {
            // fading up the UI
            presenterControl!.Visible = true;
            if (useFadeEffect)
            {
                await Effects.FadeAlphaAsync(presenterControl, 0, 1, fadeUpDuration, token.HurryUpToken);
                if (!IsInstanceValid(this))
                {
                    return;
                }
            }
            else
            {
                // We're not fading up, so set the presenter control's alpha to 1 immediately.
                var oldModulate = presenterControl.Modulate;
                presenterControl.Modulate = new Color(oldModulate.R, oldModulate.G, oldModulate.B, 1.0f);
            }
        }

        if (useTypewriterEffect)
        {
            var typewriter = new BasicTypewriter()
            {
                ActionMarkupHandlers = this.ActionMarkupHandlers,
                Text = this.lineText,
                CharactersPerSecond = this.typewriterEffectSpeed,
                ConvertHTMLToBBCode = this.ConvertHTMLToBBCode,
            };

            await typewriter.RunTypewriter(text, token.HurryUpToken);
            if (!IsInstanceValid(this))
            {
                return;
            }
        }

        // if we are set to autoadvance how long do we hold for before continuing?
        if (autoAdvance)
        {
            await YarnTask.Delay((int)(autoAdvanceDelay * 1000), token.NextLineToken).SuppressCancellationThrow();
            if (!IsInstanceValid(this))
            {
                return;
            }
        }
        else
        {
            await YarnTask.WaitUntilCanceled(token.NextLineToken).SuppressCancellationThrow();
            if (!IsInstanceValid(this))
            {
                return;
            }
        }

        // we tell all action processors that the line is finished and is about to go away
        foreach (var processor in ActionMarkupHandlers)
        {
            processor.OnLineWillDismiss();
        }

        if (IsInstanceValid(presenterControl))
        {
            // we fade down the UI
            if (useFadeEffect)
            {
                await Effects.FadeAlphaAsync(presenterControl, 1, 0, fadeDownDuration, token.HurryUpToken);
                if (!IsInstanceValid(this))
                {
                    return;
                }
            }

            presenterControl!.Visible = false;
        }
    }

    /// <inheritdoc cref="DialoguePresenterBase.RunOptionsAsync(DialogueOption[], CancellationToken)" path="/summary"/> 
    /// <inheritdoc cref="DialoguePresenterBase.RunOptionsAsync(DialogueOption[], CancellationToken)" path="/param"/> 
    /// <inheritdoc cref="DialoguePresenterBase.RunOptionsAsync(DialogueOption[], CancellationToken)" path="/returns"/> 
    /// <remarks>
    /// This Dialogue Presenter does not handle any options.
    /// </remarks>
    public YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions,
        CancellationToken cancellationToken)
    {
        return YarnTask<DialogueOption?>.FromResult(null);
    }

    private void OnContinuePressed()
    {
        if (!IsInstanceValid(dialogueRunner))
        {
            GD.PushWarning($"Continue button was clicked, but {nameof(dialogueRunner)} is invalid!", this);
            return;
        }

        dialogueRunner!.RequestNextLine();
    }

    /// <summary>
    /// If <see cref="ConvertHTMLToBBCode"/> is true, replace any HTML tags in the line text and
    /// character name text with BBCode tags.
    /// </summary>
    private void ConvertHTMLToBBCodeIfConfigured()
    {
        if (ConvertHTMLToBBCode)
        {
            if (IsInstanceValid(characterNameText))
            {
                characterNameText!.Text = Regex.Replace(characterNameText.Text, HtmlTagPattern, "[$1]");
            }

            lineText!.Text = Regex.Replace(lineText.Text, HtmlTagPattern, "[$1]");
        }
    }
}