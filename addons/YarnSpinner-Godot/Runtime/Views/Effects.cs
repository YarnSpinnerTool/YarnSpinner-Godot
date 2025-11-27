#nullable disable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace YarnSpinnerGodot;

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
        var mainTree = (SceneTree)Engine.GetMainLoop();

        var color = control.Modulate;
        color.A = from;
        control.Modulate = color;

        var destinationColor = color;
        destinationColor.A = to;

        var tween = control.CreateTween();
        tween.TweenProperty(control, "modulate", destinationColor, fadeTime);
        while (tween.IsRunning())
        {
            if (!GodotObject.IsInstanceValid(control))
            {
                // the control was deleted from the scene
                return;
            }

            if (stopToken?.WasInterrupted ?? false)
            {
                tween.CustomStep(fadeTime);
                tween.Kill();
                return;
            }

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
    public static async Task FadeAlphaAsync(Control control, float from, float to, float fadeTime,
        CancellationToken token)
    {
        var mainTree = (SceneTree)Engine.GetMainLoop();

        var color = control.Modulate;
        color.A = from;
        control.Modulate = color;

        var destinationColor = color;
        destinationColor.A = to;

        var tween = control.CreateTween();
        tween.TweenProperty(control, "modulate", destinationColor, fadeTime);
        while (tween.IsRunning())
        {
            if (!GodotObject.IsInstanceValid(control))
            {
                // the control was deleted from the scene
                return;
            }

            if (token.IsCancellationRequested)
            {
                tween.CustomStep(fadeTime);
                tween.Kill();
                return;
            }

            await DefaultActions.Wait(mainTree.Root.GetProcessDeltaTime());
        }

        color.A = to;
        if (color.A == 1f)
        {
            control.Visible = true;
        }

        control.Modulate = color;
    }
}