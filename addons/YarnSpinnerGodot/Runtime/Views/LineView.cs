using System;
using System.Collections;
using System.Threading.Tasks;
using Godot;

namespace Yarn.GodotIntegration
{
    /// <summary>
    /// Contains coroutine methods that apply visual effects. This class is used
    /// by <see cref="LineView"/> to handle animating the presentation of lines.
    /// </summary>
    public static class Effects
    {
        /// <summary>
        /// An object that can be used to signal to a coroutine that it should
        /// terminate early.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Instances of this class may be passed as a parameter to a coroutine
        /// that they can periodically poll to see if they should terminate
        /// earlier than planned.
        /// </para>
        /// <para>
        /// To use this class, create an instance of it, and pass it as a
        /// parameter to your coroutine. In the coroutine, call <see
        /// cref="Start"/> to mark that the coroutine is running. During the
        /// coroutine's execution, periodically check the <see
        /// cref="WasInterrupted"/> property to determine if the coroutine
        /// should exit. If it is <see langword="true"/>, the coroutine should
        /// exit (via the <c>yield break</c> statement.) At the normal exit of
        /// your coroutine, call the <see cref="Complete"/> method to mark that the
        /// coroutine is no longer running. To make a coroutine stop, call the
        /// <see cref="Interrupt"/> method.
        /// </para>
        /// <para>
        /// You can also use the <see cref="CanInterrupt"/> property to
        /// determine if the token is in a state in which it can stop (that is,
        /// a coroutine that's using it is currently running.)
        /// </para>
        /// </remarks>
        public partial class CoroutineInterruptToken
        {

            /// <summary>
            /// The state that the token is in.
            /// </summary>
            enum State
            {
                NotRunning,
                Running,
                Interrupted,
            }
            private State state = State.NotRunning;

            public bool CanInterrupt => state == State.Running;
            public bool WasInterrupted => state == State.Interrupted;
            public void Start() => state = State.Running;
            public void Interrupt()
            {
                if (CanInterrupt == false)
                {
                    throw new InvalidOperationException($"Cannot stop {nameof(CoroutineInterruptToken)}; state is {state} (and not {nameof(State.Running)}");
                }
                state = State.Interrupted;
            }

            public void Complete() => state = State.NotRunning;
        }

        /// <summary>
        /// A coroutine that fades a <see cref="CanvasGroup"/> object's opacity
        /// from <paramref name="from"/> to <paramref name="to"/> over the
        /// course of <see cref="fadeTime"/> seconds, and then invokes <paramref
        /// name="onComplete"/>.
        /// </summary>
        /// <param name="from">The opacity value to start fading from, ranging
        /// from 0 to 1.</param>
        /// <param name="to">The opacity value to end fading at, ranging from 0
        /// to 1.</param>
        /// <param name="stopToken">A <see cref="CoroutineInterruptToken"/> that
        /// can be used to interrupt the coroutine.</param>
        public static async Task FadeAlpha(Control canvasGroup, float from, float to, float fadeTime, CoroutineInterruptToken stopToken = null)
        {

            var mainTree = (SceneTree)Engine.GetMainLoop();

            stopToken?.Start();

            var color = canvasGroup.Modulate;
            color.a = from;
            canvasGroup.Modulate = color;

            var timeElapsed = 0d;

            while (timeElapsed < fadeTime)
            {
                if (stopToken?.WasInterrupted ?? false)
                {
                    return;
                }

                var fraction = timeElapsed / fadeTime;
                timeElapsed += mainTree.Root.GetProcessDeltaTime();

                float a = Mathf.Lerp(from, to, (float)fraction);
                color.a = a;
                canvasGroup.Modulate = color;
                await DefaultActions.Wait(mainTree.Root.GetProcessDeltaTime());
            }

            color.a = to;
            canvasGroup.Modulate = color;

            // If our destination alpha is zero, disable interactibility,
            // because the canvas group is now invisible.
            // if (to == 0)
            // {
            //     canvasGroup. = false;
            //     canvasGroup.blocksRaycasts = false;
            // }
            // else
            // {
            //     // Otherwise, enable interactibility, because it's visible.
            //     canvasGroup.interactable = true;
            //     canvasGroup.blocksRaycasts = true;
            // }

            stopToken?.Complete();
        }

        /// <summary>
        /// A coroutine that gradually reveals the text in a <see
        /// cref="RichTextLabel"/> object over time.
        /// </summary>
        /// <remarks>
        /// <para>This method works by adjusting the value of the <paramref name="text"/> parameter's <see cref="RichTextLabel.maxVisibleCharacters"/> property. This means that word wrapping will not change half-way through the presentation of a word.</para>
        /// <para style="note">Depending on the value of <paramref name="lettersPerSecond"/>, <paramref name="onCharacterTyped"/> may be called multiple times per frame.</para>
        /// <para>Due to an public implementation detail of RichTextLabel, this method will always take at least one frame to execute, regardless of the length of the <paramref name="text"/> parameter's text.</para>
        /// </remarks>
        /// <param name="text">A RichTextLabel object to reveal the text
        /// of.</param>
        /// <param name="lettersPerSecond">The number of letters that should be
        /// revealed per second.</param>
        /// <param name="onCharacterTyped">An <see cref="Action"/> that should be called for each character that was revealed.</param>
        /// <param name="stopToken">A <see cref="CoroutineInterruptToken"/> that
        /// can be used to interrupt the coroutine.</param>
        public static async Task Typewriter(RichTextLabel text, float lettersPerSecond, Action onCharacterTyped, CoroutineInterruptToken stopToken = null)
        {
            var mainTree = (SceneTree)Engine.GetMainLoop();
            stopToken?.Start();

            // Start with everything invisible
            text.PercentVisible = 0;

            // Wait a single frame to let the text component process its
            // content, otherwise text.textInfo.characterCount won't be
            // accurate
            await DefaultActions.Wait(LineView.FrameWaitTime);
            if (!Godot.Object.IsInstanceValid(text))
            {
                return;
            }
            if (stopToken?.WasInterrupted ?? false)
            {
                text.PercentVisible = 1f;
                return;
            }
            // How many visible characters are present in the text?
            var characterCount = text.Text.Length;

            // Early out if letter speed is zero, text length is zero
            if (lettersPerSecond <= 0 || characterCount == 0)
            {
                // Show everything and return
                text.PercentVisible = 1;
                stopToken?.Complete();
                return;
            }

            // Convert 'letters per second' into its inverse
            float secondsPerLetter = 1.0f / lettersPerSecond;

            // If lettersPerSecond is larger than the average framerate, we
            // need to show more than one letter per frame, so simply
            // adding 1 letter every secondsPerLetter won't be good enough
            // (we'd cap out at 1 letter per frame, which could be slower
            // than the user requested.)
            //
            // Instead, we'll accumulate time every frame, and display as
            // many letters in that frame as we need to in order to achieve
            // the requested speed.
            var deltaTime = mainTree.Root.GetProcessDeltaTime();
            var accumulator = deltaTime;


            var ratioPerLetter = 1f / text.Text.Length;
            while (text.PercentVisible < 1)
            {
                if (stopToken?.WasInterrupted ?? false)
                {
                    text.PercentVisible = 1f;
                    return;
                }

                // We need to show as many letters as we have accumulated
                // time for.
                while (accumulator >= secondsPerLetter)
                {
                    if (!Godot.Object.IsInstanceValid(text))
                    {
                        return;
                    }
                    text.PercentVisible += ratioPerLetter;
                    onCharacterTyped?.Invoke();
                    accumulator -= secondsPerLetter;
                }
                accumulator += deltaTime;

                await DefaultActions.Wait(deltaTime);
            }

            // We either finished displaying everything, or were
            // interrupted. Either way, display everything now.
            text.PercentVisible = 1;

            stopToken?.Complete();
        }
    }

    /// <summary>
    /// A Dialogue View that presents lines of dialogue, using Unity UI
    /// elements.
    /// </summary>
    public partial class LineView : DialogueViewBase
    {
        /// <summary>
        /// The Control that is the parent of all UI elements in this line view.
        /// Used to modify the transparency/visibility of the UI.
        /// </summary>
        /// <remarks>
        /// If <see cref="useFadeEffect"/> is true, then the alpha value of this
        /// <see cref="CanvasGroup"/> will be animated during line presentation
        /// and dismissal.
        /// </remarks>
        /// <seealso cref="useFadeEffect"/>
        [Export]
        public NodePath viewControlPath;
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
        [Export]
        public bool useFadeEffect = true;

        /// <summary>
        /// The time that the fade effect will take to fade lines in.
        /// </summary>
        /// <remarks>This value is only used when <see cref="useFadeEffect"/> is
        /// <see langword="true"/>.</remarks>
        /// <seealso cref="useFadeEffect"/>
        [Export]
        public float fadeInTime = 0.25f;

        /// <summary>
        /// The time that the fade effect will take to fade lines out.
        /// </summary>
        /// <remarks>This value is only used when <see cref="useFadeEffect"/> is
        /// <see langword="true"/>.</remarks>
        /// <seealso cref="useFadeEffect"/>
        [Export]
        public float fadeOutTime = 0.05f;

        public const float FrameWaitTime = 0.16f;
        /// <summary>
        /// Node path from the inspector for <see cref="lineText"/>
        /// </summary>
        [Export]
        public NodePath lineTextPath;
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
        [Export]
        public bool showCharacterNameInLineView = true;

        /// <summary>
        /// The <see cref="RichTextLabel"/> object that displays the character
        /// names found in dialogue lines.
        /// </summary>
        /// <remarks>
        /// If the <see cref="LineView"/> receives a line that does not contain
        /// a character name, this object will be left blank.
        /// </remarks>
        [Export]
        public NodePath characterNameTextPath;

        public RichTextLabel characterNameText = null;

        /// <summary>
        /// Controls whether the text of <see cref="lineText"/> should be
        /// gradually revealed over time.
        /// </summary>
        /// <remarks><para>If this value is <see langword="true"/>, the <see
        /// cref="lineText"/> object's <see
        /// cref="TMP_Text.maxVisibleCharacters"/> property will animate from 0
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
        [Export]
        public bool useTypewriterEffect = false;

        public delegate void OnCharacterTypedHandler();
        /// <summary>
        /// A Unity Event that is called each time a character is revealed
        /// during a typewriter effect.
        /// </summary>
        /// <remarks>
        /// This event is only invoked when <see cref="useTypewriterEffect"/> is
        /// <see langword="true"/>.
        /// </remarks>
        /// <seealso cref="useTypewriterEffect"/>
        public OnCharacterTypedHandler onCharacterTyped;

        /// <summary>
        /// The number of characters per second that should appear during a
        /// typewriter effect.
        /// </summary>
        /// <seealso cref="useTypewriterEffect"/>
        [Export]
        public float typewriterEffectSpeed = 0f;

        /// <summary>
        /// The game object that represents an on-screen button that the user
        /// can click to continue to the next piece of dialogue.
        /// </summary>
        /// <remarks>
        /// <para>This game object will be made inactive when a line begins
        /// appearing, and active when the line has finished appearing.</para>
        /// <para>
        /// This field will generally refer to an object that has a <see
        /// cref="Button"/> component on it that, when clicked, calls <see
        /// cref="OnContinueClicked"/>. However, if your game requires specific
        /// UI needs, you can provide any object you need.</para>
        /// </remarks>
        /// <seealso cref="autoAdvance"/>
        [Export]
        public NodePath continueButtonPath;

        /// <summary>
        /// A node with a signal named "pressed" that advances the dialogue to the
        /// next line.
        /// </summary>
        public Control continueButton = null;

        /// <summary>
        /// The amount of time to wait after any line
        /// </summary>
        [Export]
        public float holdTime = 1f;

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
        [Export]
        public bool autoAdvance = false;

        /// <summary>
        /// The current <see cref="LocalizedLine"/> that this line view is
        /// displaying.
        /// </summary>
        LocalizedLine currentLine = null;

        /// <summary>
        /// A stop token that is used to interrupt the current animation.
        /// </summary>
        Effects.CoroutineInterruptToken currentStopToken = new Effects.CoroutineInterruptToken();

        public override void _Ready()
        {
            if (lineText == null)
            {
                lineText = GetNode<RichTextLabel>(lineTextPath);
                lineText.BbcodeEnabled = true;
            }
            if (viewControl == null)
            {
                viewControl = GetNode(viewControlPath) as Control;
            }
            if (continueButton == null && !string.IsNullOrEmpty(continueButtonPath.ToString()))
            {
                continueButton = (Control)GetNode(continueButtonPath);
            }
            if (continueButton != null)
            {
                continueButton.Connect("pressed", this, nameof(OnContinueClicked));
            }
            if (characterNameText == null && !string.IsNullOrEmpty(characterNameTextPath))
            {
                characterNameText = GetNode<RichTextLabel>(characterNameTextPath);
            }

            SetViewAlpha(0);
            SetCanvasInteractable(false);
        }

        private void SetViewAlpha(float alpha)
        {
            if (!IsInstanceValid(viewControl)) return;
            var color = viewControl.Modulate;
            color.a = alpha;
            viewControl.Modulate = color;
        }
        /// <inheritdoc/>
        public override void DismissLine(Action onDismissalComplete)
        {
            currentLine = null;

            DismissLineInternal(onDismissalComplete);
        }

        private async void DismissLineInternal(Action onDismissalComplete)
        {
            // disabling interaction temporarily while dismissing the line
            // we don't want people to interrupt a dismissal
            var interactable = viewControl.Visible;
            SetCanvasInteractable(false);

            // If we're using a fade effect, run it, and wait for it to finish.
            if (useFadeEffect)
            {
                await Effects.FadeAlpha(viewControl, 1, 0, fadeOutTime, currentStopToken);
                currentStopToken.Complete();
            }

            SetViewAlpha(0f);
            SetCanvasInteractable(interactable);
            onDismissalComplete();
        }

        /// <inheritdoc/>
        public override void InterruptLine(LocalizedLine dialogueLine, Action onInterruptLineFinished)
        {
            currentLine = dialogueLine;

            // Cancel all coroutines that we're currently running. This will
            // stop the RunLinepublic coroutine, if it's running.
            // TODO: equivalent to stop all coroutines
            // StopAllCoroutines();

            // for now we are going to just immediately show everything
            // later we will make it fade in
            lineText.Visible = true;
            viewControl.Visible = true;

            int length;

            if (characterNameText == null)
            {
                if (showCharacterNameInLineView)
                {
                    if (lineText.BbcodeEnabled)
                    {
                        lineText.BbcodeText = dialogueLine.Text.Text;
                    }
                    else
                    {
                        lineText.Text = dialogueLine.Text.Text;
                    }
                    length = dialogueLine.Text.Text.Length;
                }
                else
                {
                    if (lineText.BbcodeEnabled)
                    {
                        lineText.BbcodeText = dialogueLine.TextWithoutCharacterName.Text;
                    }
                    else
                    {
                        lineText.Text = dialogueLine.TextWithoutCharacterName.Text;
                    }
                    length = dialogueLine.TextWithoutCharacterName.Text.Length;
                }
            }
            else
            {
                if (characterNameText.BbcodeEnabled)
                {
                    characterNameText.BbcodeText = dialogueLine.CharacterName;
                }
                else
                {
                    characterNameText.Text = dialogueLine.CharacterName;
                }

                if (lineText.BbcodeEnabled)
                {
                    lineText.BbcodeText = dialogueLine.TextWithoutCharacterName.Text;
                }
                else
                {
                    lineText.Text = dialogueLine.TextWithoutCharacterName.Text;
                }
                length = dialogueLine.TextWithoutCharacterName.Text.Length;
            }

            // Show the entire line's text immediately.
            lineText.PercentVisible = 1;

            // Make the canvas group fully visible immediately, too.
            SetViewAlpha(1f);

            SetCanvasInteractable(true);

            onInterruptLineFinished();
        }

        /// <inheritdoc/>
        public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            // Stop any coroutines currently running on this line view (for
            // example, any other RunLine that might be running)
            // StopAllCoroutines();

            // Begin running the line as a coroutine.
            RunLineInternal(dialogueLine, onDialogueLineFinished);
        }

        private async Task RunLineInternal(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            async Task PresentLine()
            {
                lineText.Visible = true;
                viewControl.Visible = true;
                var color = viewControl.Modulate;
                color.a = 1f;
                viewControl.Modulate = color;

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

                if (characterNameText != null)
                {
                    // If we have a character name text view, show the character
                    // name in it, and show the rest of the text in our main
                    // text view.
                    if (characterNameText.BbcodeEnabled)
                    {
                        characterNameText.BbcodeText = dialogueLine.CharacterName;
                    }
                    else
                    {
                        characterNameText.Text = dialogueLine.CharacterName;
                    }

                    if (lineText.BbcodeEnabled)
                    {
                        lineText.BbcodeText = dialogueLine.TextWithoutCharacterName.Text;
                    }
                    else
                    {
                        lineText.Text = dialogueLine.TextWithoutCharacterName.Text;
                    }

                }
                else
                {
                    // We don't have a character name text view. Should we show
                    // the character name in the main text view?
                    if (showCharacterNameInLineView)
                    {
                        // Yep! Show the entire text.
                        lineText.Text = dialogueLine.Text.Text;
                    }
                    else
                    {
                        // Nope! Show just the text without the character name.
                        lineText.Text = dialogueLine.TextWithoutCharacterName.Text;
                    }
                }

                if (useTypewriterEffect)
                {
                    // If we're using the typewriter effect, hide all of the
                    // text before we begin any possible fade (so we don't fade
                    // in on visible text).
                    lineText.PercentVisible = 0;
                }
                else
                {
                    // Show all characters
                    lineText.PercentVisible = 100;
                }

                // If we're using the fade effect, start it, and wait for it to
                // finish.
                if (useFadeEffect)
                {
                    await Effects.FadeAlpha(viewControl, 0, 1, fadeInTime, currentStopToken);
                    if (currentStopToken.WasInterrupted)
                    {
                        // The fade effect was interrupted. Stop this entire
                        // coroutine.
                        return;
                    }
                }

                // If we're using the typewriter effect, start it, and wait for
                // it to finish.
                if (useTypewriterEffect)
                {
                    // setting the canvas all back to its defaults because if we didn't also fade we don't have anything visible
                    color = viewControl.Modulate;
                    color.a = 1f;
                    viewControl.Modulate = color;
                    SetCanvasInteractable(true);
                    await Effects.Typewriter(
                        lineText,
                        typewriterEffectSpeed,
                        () => onCharacterTyped?.Invoke(),
                        currentStopToken
                    );
                    if (currentStopToken.WasInterrupted)
                    {
                        // The typewriter effect was interrupted. Stop this
                        // entire coroutine.
                        return;
                    }
                }
            }

            currentLine = dialogueLine;

            // Run any presentations as a single coroutine. If this is stopped,
            // which UserRequestedViewAdvancement can do, then we will stop all
            // of the animations at once.
            await PresentLine();

            currentStopToken.Complete();

            // All of our text should now be visible.
            lineText.PercentVisible = 100;

            // Our view should at be at full opacity.
            SetViewAlpha(1f);
            SetCanvasInteractable(true);

            // Show the continue button, if we have one.
            if (continueButton != null)
            {
                if (continueButton is Button button)
                {
                    button.Disabled = false;
                }
                else if (continueButton is TextureButton textureButton)
                {
                    textureButton.Disabled = false;
                }
                continueButton.Visible = true;
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
                // coroutine.
                return;
            }

            // Our presentation is complete; call the completion handler.
            onDialogueLineFinished();
        }

        private void SetCanvasInteractable(bool b)
        {
            if (!IsInstanceValid(viewControl))
            {
                return;
            }
            viewControl.SetProcessInput(b);
            viewControl.Visible = b;
        }

        /// <inheritdoc/>
        public override void UserRequestedViewAdvancement()
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
            // animation coroutine is what actually interrupts
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
    }
}