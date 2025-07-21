using System;
using System.Collections.Generic;
using System.Threading;
using Godot;


#nullable enable

namespace YarnSpinnerGodot;

/// <summary>
/// A <see cref="MonoBehaviour"/> that can present lines and options to the
/// user, when it receives them from a  <see cref="DialogueRunner"/>.
/// Important: When implementing this interface, your class should extend from Godot.Node.
/// </summary>
/// <remarks>
/// <para>When the Dialogue Runner encounters content that the user should
/// see - that is, lines or options - it sends that content to all of the
/// Dialogue Presenters stored in <see cref="DialogueRunner.dialoguePresenters"/>. The
/// Dialogue Runner then waits until all Dialogue Presenters have reported that
/// they have finished presenting the content.</para>
/// <para>
/// To use this class, subclass it, and implement its required methods. Once
/// you have written your subclass, attach it as a component to a <see
/// cref="GameObject"/>, and add this game object to the list of Dialogue
/// Presenters in your scene's <see cref="DialogueRunner"/>.
/// </para>
/// <para>Dialogue Presenter do not need to handle every kind of content that
/// the Dialogue Runner might produce. For example, you might have one
/// Dialogue Presenter that handles Lines, and another that handles Options. The
/// built-in <see cref="LinePresenter"/> class is an example of this, in that it
/// only handles Lines and does nothing when it receives Options.</para>
/// <para>
/// You may also have multiple Dialogue Presenters that handle the <i>same</i>
/// kind of content. For example, you may have a Dialogue Presenter that receives
/// Lines and uses them to play voice-over audio, and a second Dialogue Presenter
/// that also receives Lines and uses them to display on-screen subtitles.
/// </para>
/// </remarks>
/// <seealso cref="LineProviderBehaviour"/>
/// <seealso cref="DialogueRunner.dialoguePresenters"/>
public interface DialoguePresenterBase
{
    /// <summary>
    /// Called by the <see cref="DialogueRunner"/> to signal that a line
    /// should be displayed to the user.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When this method is called, the Dialogue Presenter should present the
    /// line to the user. The content to present is contained within the
    /// <paramref name="line"/> parameter, which contains information about
    /// the line in the user's current locale.
    /// </para>
    /// <para style="tip">
    /// It's up to the Dialogue Presenter to decide what "presenting" the line
    /// may mean; for example, showing on-screen text, playing voice-over
    /// audio, or updating on-screen portraits to show a picture of the
    /// speaking character.
    /// </para>
    /// <para>
    /// The <see cref="DialogueRunner"/> will wait until the tasks from all
    /// of its Dialogue Presenters have completed before continuing to the next
    /// piece of content. If your Dialogue Presenter does not need to handle the
    /// line, it should return immediately.
    /// </para>
    /// <para style="info">The value of the <paramref name="line"/>
    /// parameter is produced by the Dialogue Runner's <see
    /// cref="LineProviderBehaviour"/>.
    /// </para>
    /// <para style="info">
    /// The default implementation of this method takes no action and
    /// returns immediately.
    /// </para>
    /// </remarks>
    /// <param name="line">The line to present.</param>
    /// <param name="token">A <see cref="LineCancellationToken"/> that
    /// represents whether the Dialogue Presenter should hurry it its
    /// presentation of the line, or stop showing the current line.</param>
    /// <returns>A task that completes when the Dialogue Presenter has finished
    /// showing the line to the user.</returns>
    /// <seealso cref="RunOptionsAsync(DialogueOption[],
    /// CancellationToken)"/>
    public async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        // backwards compatibility with 0.2.*
#pragma warning disable CS0618 // Type or member is obsolete
        if (this is DialogueViewBase v2View)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            // phaseComplete is a flag that represents whether the current
            // 'phase' of a v2-style Dialogue Presenter (Run, Interrupt, Dismiss) is
            // complete or not.
            bool phaseComplete = false;
            void PhaseComplete() => phaseComplete = true;

            // Run the line, and make phaseComplete become true when it's done.
            v2View.RunLine(line, PhaseComplete);

            // Wait for one of the following things to happen:
            // 1. RunLine completes successfully and calls PhaseComplete.
            // 2. The line is cancelled.
            while (GodotObject.IsInstanceValid((GodotObject)this) && phaseComplete == false
                                                                  && token.IsNextLineRequested == false
                  )
            {
                await YarnTask.Yield();
            }

            if (!GodotObject.IsInstanceValid((GodotObject)this))
            {
                return;
            }

            // If the line was cancelled, tell the view that the line was
            // 'interrupted' and should finish presenting quickly. Wait for the
            // phase to complete.
            if (token.IsNextLineRequested)
            {
                phaseComplete = false;
                v2View.InterruptLine(line, PhaseComplete);
                while (GodotObject.IsInstanceValid((GodotObject)this) && phaseComplete == false)
                {
                    await YarnTask.Yield();
                }

                if (!GodotObject.IsInstanceValid((GodotObject)this))
                {
                    return;
                }
            }

            // Finally, signal that the line should be dismissed, and wait for
            // the dismissal to complete.
            phaseComplete = false;
            v2View.DismissLine(PhaseComplete);

            while (GodotObject.IsInstanceValid((GodotObject)this) && phaseComplete == false)
            {
                await YarnTask.Yield();
            }
        }
    }


    /// <summary>
    /// Called by the <see cref="DialogueRunner"/> to signal that a set of
    /// options should be displayed to the user.
    /// </summary>
    /// <remarks>
    /// <para>This method is called when the Dialogue Runner wants to show a
    /// collection of options that the user should choose from. Each option
    /// is represented by a <see cref="DialogueOption"/> object, which
    /// contains information about the option.</para>
    /// <para>When this method is called, the Dialogue Presenter should display
    /// appropriate user interface elements that let the user choose among
    /// the options.</para>
    /// <para>This method should await until an option is selected, and then
    /// return the selected option. If this view doesn't handle options, or
    /// is otherwise unable to select an option, it should return <see
    /// cref="DialogueRunner.NoOptionSelected"/>. The dialogue runner will wait
    /// for all Dialogue Presenters to return, so if a Dialogue Presenter doesn't or
    /// can't handle options, it's good practice to return as soon as
    /// possible. 
    /// </para>
    /// <para>If the <paramref name="cancellationToken"/> becomes cancelled,
    /// this means that the dialogue runner no longer needs this dialogue
    /// view to make a selection, and this method should return as soon as
    /// possible; its return value will not be used.
    /// </para>
    /// <para style="note">
    /// The default implementation of this method returns <see
    /// cref="DialogueRunner.NoOptionSelected"/>. 
    /// </para>
    /// </remarks>
    /// <param name="dialogueOptions">The set of options that should be
    /// displayed to the user.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>
    /// that becomes cancelled when the dialogue runner no longer needs this
    /// Dialogue Presenter to return an option.</param>
    /// <returns>A task that indicates which option was selected, or that this Dialogue Presenter did not select an option.</returns>
    /// <seealso cref="RunLineAsync(LocalizedLine, LineCancellationToken)"/>
    /// <seealso cref="DialogueRunner.NoOptionSelected"/> 
    public async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions,
        CancellationToken cancellationToken)
    {
        // backwards compatibility with 0.2.*
#pragma warning disable CS0618 // Type or member is obsolete
        if (this is DialogueViewBase v2View)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            int selectedOptionID = -1;

            // Run the options, and wait for either a selection to be made, or
            // for this view to be cancelled.
            v2View.RunOptions(dialogueOptions, (selectedID) => { selectedOptionID = selectedID; });

            while (GodotObject.IsInstanceValid((GodotObject)this) &&
                   selectedOptionID == -1 && cancellationToken.IsCancellationRequested == false)
            {
                await YarnTask.Yield();
            }

            if (!GodotObject.IsInstanceValid((GodotObject)this) || cancellationToken.IsCancellationRequested)
            {
                // We were cancelled or are exiting the game. Return null.
                return null;
            }

            // Find the option that has the same ID as the one that was
            // selected, and return that.
            for (int i = 0; i < dialogueOptions.Length; i++)
            {
                if (dialogueOptions[i].DialogueOptionID == selectedOptionID)
                {
                    return dialogueOptions[i];
                }
            }

            // If we got here, we weren't cancelled, but we also didn't select
            // an option that was valid. Throw an error.
            throw new InvalidOperationException($"Option view selected an invalid option ID ({selectedOptionID})");
        }

        // otherwise, default implementation.
        return await DialogueRunner.NoOptionSelected;
    }

    /// <summary>Called by the <see cref="DialogueRunner"/> to signal that
    /// dialogue has started.</summary>
    /// <remarks>
    /// <para>This method is called before any content (that is, lines,
    /// options or commands) are delivered.</para>
    /// <para>This method is a good place to perform tasks like preparing
    /// on-screen dialogue UI (for example, turning on a letterboxing
    /// effect, or making dialogue UI elements visible.)
    /// </para>
    /// <para style="note">The default implementation of this method does
    /// nothing.</para>
    /// </remarks>
    /// <returns>A task that represents any work done by this Dialogue Presenter in order to get ready for dialogue to run.</returns>
    public YarnTask OnDialogueStartedAsync()
    {
        // backwards compatibility with 0.2.*
#pragma warning disable CS0618 // Type or member is obsolete
        if (this is DialogueViewBase v2View)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            // Invoke the synchronous version of 'dialogue started'
            v2View.DialogueStarted();
        }

        return YarnTask.CompletedTask;
    }

    /// <summary>
    /// Called by the <see cref="DialogueRunner"/> to signal that the
    /// dialogue has ended, and no more lines will be delivered.
    /// </summary>
    /// <remarks>
    /// <para>This method is called after the last piece of content (that
    /// is, lines, options or commands) finished running.</para>
    /// <para>This method is a good place to perform tasks like dismissing
    /// on-screen dialogue UI (for example, turning off a letterboxing
    /// effect, or hiding dialogue UI elements.)
    /// </para>
    /// <para style="note">The default implementation of this method does
    /// nothing.</para>
    /// </remarks>
    /// <returns>A task that represents any work done by this Dialogue Presenter
    /// in order to clean up after running dialogue.</returns>
    public YarnTask OnDialogueCompleteAsync()
    {
        // backwards compatibility with 0.2.*
#pragma warning disable CS0618 // Type or member is obsolete
        if (this is DialogueViewBase v2View)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            // Invoke the synchronous version of 'dialogue started'
            v2View.DialogueComplete();
        }

        return YarnTask.CompletedTask;
    }

    /// The collection of action markup handlers that the dialogue presenter
    /// uses when presenting content.
    /// </summary>
    public List<IActionMarkupHandler> ActionMarkupHandlers { get; }
}