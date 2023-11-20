using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace YarnSpinnerGodot
{
    /// <summary>
    /// Contains async methods that apply visual effects. This class is used
    /// by <see cref="LineView"/> to handle animating the presentation of lines.
    /// </summary>
    public static class Effects
    {
        /// <summary>
        /// An object that can be used to signal to a Task that it should
        /// terminate early.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Instances of this class may be passed as a parameter to a Task
        /// that they can periodically poll to see if they should terminate
        /// earlier than planned.
        /// </para>
        /// <para>
        /// To use this class, create an instance of it, and pass it as a
        /// parameter to your task. In the task, call <see
        /// cref="Start"/> to mark that the task is running. During the
        /// task's execution, periodically check the <see
        /// cref="WasInterrupted"/> property to determine if the task
        /// should exit. If it is <see langword="true"/>, the task should
        /// exit (via the <c>yield break</c> statement.) At the normal exit of
        /// your task, call the <see cref="Complete"/> method to mark that the
        /// task is no longer running. To make a task stop, call the
        /// <see cref="Interrupt"/> method.
        /// </para>
        /// <para>
        /// You can also use the <see cref="CanInterrupt"/> property to
        /// determine if the token is in a state in which it can stop (that is,
        /// a task that's using it is currently running.)
        /// </para>
        /// </remarks>
        public class TaskInterruptToken
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
                    throw new InvalidOperationException(
                        $"Cannot stop {nameof(TaskInterruptToken)}; state is {state} (and not {nameof(State.Running)}");
                }

                state = State.Interrupted;
            }

            public void Complete() => state = State.NotRunning;
        }

        /// <summary>
        /// A Task that fades a <see cref="CanvasGroup"/> object's opacity
        /// from <paramref name="from"/> to <paramref name="to"/> over the
        /// course of <see cref="fadeTime"/> seconds, and then returns.
        /// </summary>
        /// <param name="from">The opacity value to start fading from, ranging
        /// from 0 to 1.</param>
        /// <param name="to">The opacity value to end fading at, ranging from 0
        /// to 1.</param>
        /// <param name="stopToken">A <see cref="TaskInterruptToken"/> that
        /// can be used to interrupt the task.</param>
        public static async Task FadeAlpha(Control control, float from, float to, float fadeTime,
            TaskInterruptToken stopToken = null)
        {
            var mainTree = (SceneTree) Engine.GetMainLoop();

            stopToken?.Start();

            var color = control.Modulate;
            color.A = from;
            control.Modulate = color;

            var timeElapsed = 0d;

            while (timeElapsed < fadeTime)
            {
                if (stopToken?.WasInterrupted ?? false)
                {
                    return;
                }

                var fraction = timeElapsed / fadeTime;
                timeElapsed += mainTree.Root.GetProcessDeltaTime();

                float a = Mathf.Lerp(from, to, (float) fraction);
                color.A = a;
                control.Modulate = color;
                await DefaultActions.Wait(mainTree.Root.GetProcessDeltaTime());
            }

            color.A = to;
            if (color.A == 1f)
            {
                control.Visible = true;
            }

            control.Modulate = color;
            stopToken?.Complete();
        }

        public static async Task Typewriter(RichTextLabel text, float lettersPerSecond, 
            Action onCharacterTyped, TaskInterruptToken stopToken = null)
        {
            await PausableTypewriter(
                text,
                lettersPerSecond,
                onCharacterTyped,
                null,
                null,
                null,
                stopToken
            );
        }

        /// <summary>
        /// A basic wait task that can be interrupted.
        /// </summary>
        /// <remarks>
        /// This is designed to be used as part of the <see cref="PausableTypewriter"/> but theoretically anything can use it.
        /// </remarks>
        /// <param name="duration">The length of the pause</param>
        /// <param name="stopToken">An interrupt token for this wait</param>
        private static async Task InterruptableWait(float duration, TaskInterruptToken stopToken = null)
        {
            double accumulator = 0;
            while (accumulator < duration)
            {
                if (stopToken?.WasInterrupted ?? false)
                {
                    return;
                }

                await DefaultActions.Wait(0.01);
                var mainTree = (SceneTree) Engine.GetMainLoop();
                var deltaTime = mainTree.Root.GetProcessDeltaTime();
                accumulator += deltaTime;
            }
        }

        /// <summary>
        /// A Task that gradually reveals the text in a <see
        /// cref="RichTextLabel"/> object over time.
        /// </summary>
        /// <remarks>
        /// <para>This method works by adjusting the value of the <paramref name="text"/> parameter's <see cref="RichTextLabel.VisibleRatio"/> property. This means that word wrapping will not change half-way through the presentation of a word.</para>
        /// <para style="note">Depending on the value of <paramref name="lettersPerSecond"/>, <paramref name="onCharacterTyped"/> may be called multiple times per frame.</para>
        /// <para>Due to an public implementation detail of RichTextLabel, this method will always take at least one frame to execute, regardless of the length of the <paramref name="text"/> parameter's text.</para>
        /// </remarks>
        /// <param name="text">A RichTextLabel object to reveal the text
        /// of.</param>
        /// <param name="lettersPerSecond">The number of letters that should be
        /// revealed per second.</param>
        /// <param name="onCharacterTyped">An <see cref="Action"/> that should be called for each character that was revealed.</param>
        /// <param name="onPauseStarted">An <see cref="Action"/> that will be called when the typewriter effect is paused.</param>
        /// <param name="onPauseEnded">An <see cref="Action"/> that will be called when the typewriter effect is restarted.</param>
        /// <param name="pausePositions">A stack of character position and pause duration tuples used to pause the effect. Generally created by <see cref="LineView.GetPauseDurationsInsideLine"/></param>
        /// <param name="stopToken">A <see cref="TaskInterruptToken"/> that
        /// can be used to interrupt the task.</param>
        public static async Task PausableTypewriter(RichTextLabel text, float lettersPerSecond, Action onCharacterTyped,
            Action onPauseStarted, Action onPauseEnded, Stack<(int position, float duration)> pausePositions,
            TaskInterruptToken stopToken = null)
        {
            var mainTree = (SceneTree) Engine.GetMainLoop();
            stopToken?.Start();

            // Start with everything invisible
            text.VisibleRatio = 0;

            // Wait a single frame to let the text component process its
            // content, otherwise text.textInfo.characterCount won't be
            // accurate
            await DefaultActions.Wait(LineView.FrameWaitTime);
            if (!GodotObject.IsInstanceValid(text))
            {
                return;
            }

            if (stopToken?.WasInterrupted ?? false)
            {
                text.VisibleRatio = 1f;
                return;
            }

            // How many characters are present in the text?
            var characterCount = text.GetTotalCharacterCount();

            // Early out if letter speed is zero, text length is zero
            if (lettersPerSecond <= 0 || characterCount == 0)
            {
                // Show everything and return
                text.VisibleRatio = 1;
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
            
            while (text.VisibleRatio < 1)
            {
                if (!GodotObject.IsInstanceValid(text))
                {
                    return;
                }

                if (stopToken?.WasInterrupted ?? false)
                {
                    text.VisibleRatio = 1f;
                    return;
                }

                // ok so the change needs to be that if at any point we hit the pause position
                // we instead stop worrying about letters
                // and instead we do a normal wait for the necessary duration
                if (pausePositions != null && pausePositions.Count != 0)
                {
                    if (text.VisibleCharacters == pausePositions.Peek().Item1)
                    {
                        var pause = pausePositions.Pop();
                        onPauseStarted?.Invoke();
                        await Effects.InterruptableWait(pause.Item2, stopToken);
                        onPauseEnded?.Invoke();

                        // need to reset the accumulator
                        accumulator = deltaTime;
                    }
                }

                // We need to show as many letters as we have accumulated
                // time for.
                while (accumulator >= secondsPerLetter)
                {
                    if (!GodotObject.IsInstanceValid(text))
                    {
                        return;
                    }

                    text.VisibleCharacters += 1;
                    onCharacterTyped?.Invoke();
                    accumulator -= secondsPerLetter;
                }

                accumulator += deltaTime;

                await DefaultActions.Wait(deltaTime);
            }

            // We either finished displaying everything, or were
            // interrupted. Either way, display everything now.
            text.VisibleRatio = 1;

            stopToken?.Complete();
        }
    }
}