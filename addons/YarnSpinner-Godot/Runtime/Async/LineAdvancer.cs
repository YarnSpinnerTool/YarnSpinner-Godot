using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Godot;


#nullable enable

namespace YarnSpinnerGodot;

/// <summary>
/// A dialogue view that listens for user input and sends requests to a <see
/// cref="DialogueRunner"/> to advance the presentation of the current line,
/// either by asking a dialogue runner to hurry up its delivery, advance to
/// the next line, or cancel the entire dialogue session.
/// </summary>
[GlobalClass]
public partial class LineAdvancer : Node, DialoguePresenterBase
{
    [Export] DialogueRunner? runner;

    /// <summary>
    /// If <see langword="true"/>, repeatedly signalling that the line
    /// should be hurried up will cause the line advancer to request that
    /// the next line be shown.
    /// </summary>
    /// <seealso cref="advanceRequestsBeforeCancellingLine"/>
    [Export] public bool multiAdvanceIsCancel = false;

    /// <summary>
    /// The number of times that a 'hurry up' signal occurs before the line
    /// advancer requests that the next line be shown.
    /// </summary>
    /// <seealso cref="multiAdvanceIsCancel"/>
    [Export] public int advanceRequestsBeforeCancellingLine = 2;

    /// <summary>
    /// The number of times that this object has received an indication that
    /// the line should be advanced.
    /// </summary>
    /// <remarks>
    /// This value is reset to zero when a new line is run. When the line is
    /// advanced, this value is incremented. If this value ever meets or
    /// exceeds <see cref="advanceRequestsBeforeCancellingLine"/>, the line
    /// will be cancelled.
    /// </remarks>
    private int numberOfAdvancesThisLine = 0;

    /// <summary>
    /// The input action that triggers a request to advance to the
    /// next piece of content.
    /// </summary>
    [Export] public string hurryUpAction = "ui_accept";

    /// <summary>
    /// The input action that triggers an instruction to cancel the
    /// current line.
    /// </summary>
    [Export] public string nextLineAction = "ui_accept";

    /// <summary>
    /// The input action that triggers an instruction to cancel the
    /// entire dialogue.
    /// </summary>
    [Export] public string cancelDialogueAction = "ui_cancel";

    /// <summary>
    /// Called by a dialogue runner when dialogue starts to add input action
    /// handlers for advancing the line.
    /// </summary>
    /// <returns>A completed task.</returns>
    public YarnTask OnDialogueStartedAsync()
    {
        return YarnTask.CompletedTask;
    }

    /// <summary>
    /// Called by a dialogue runner when dialogue ends to remove the input
    /// action handlers.
    /// </summary>
    /// <returns>A completed task.</returns>
    public YarnTask OnDialogueCompleteAsync()
    {
        return YarnTask.CompletedTask;
    }

    public List<IActionMarkupHandler> ActionMarkupHandlers { get; } = [];

    /// <summary>
    /// Called by a dialogue view to signal that a line is running.
    /// </summary>
    /// <inheritdoc cref="LinePresenter.RunLineAsync" path="/param"/>
    /// <returns>A completed task.</returns>
    public YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        // A new line has come in, so reset the number of times we've seen a
        // request to skip.
        numberOfAdvancesThisLine = 0;

        return YarnTask.CompletedTask;
    }

    /// <summary>
    /// Called by a dialogue view to signal that options are running.
    /// </summary>
    /// <inheritdoc cref="LinePresenter.RunOptionsAsync" path="/param"/>
    /// <returns>A completed task indicating that no option was selected by
    /// this view.</returns>
    public YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions,
        CancellationToken cancellationToken)
    {
        // This line view doesn't take any actions when options are
        // presented.
        return YarnTask<DialogueOption?>.FromResult(null);
    }

    /// <summary>
    /// Requests that the line be hurried up.
    /// </summary>
    /// <remarks>If this method has been called more times for a single line
    /// than <see cref="numberOfAdvancesThisLine"/>, this method requests
    /// that the dialogue runner proceed to the next line. Otherwise, it
    /// requests that the dialogue runner instruct all line views to hurry
    /// up their presentation of the current line.
    /// </remarks>
    public void RequestLineHurryUp()
    {
        // Increment our counter of line advancements, and depending on the
        // new count, request that the runner 'soft-cancel' the line or
        // cancel the entire line

        numberOfAdvancesThisLine += 1;

        if (multiAdvanceIsCancel && numberOfAdvancesThisLine >= advanceRequestsBeforeCancellingLine)
        {
            RequestNextLine();
        }
        else
        {
            if (runner != null)
            {
                runner.RequestHurryUpLine();
            }
            else
            {
                GD.PushError($"{nameof(LineAdvancer)} dialogue runner is null", this);
                return;
            }
        }
    }

    /// <summary>
    /// Requests that the dialogue runner proceeds to the next line.
    /// </summary>
    public void RequestNextLine()
    {
        if (runner != null)
        {
            runner.RequestNextLine();
        }
        else
        {
            GD.PushError($"{nameof(LineAdvancer)} dialogue runner is null", this);
            return;
        }
    }

    /// <summary>
    /// Requests that the dialogue runner to instruct all line views to
    /// dismiss their content, and then stops the dialogue.
    /// </summary>
    public void RequestDialogueCancellation()
    {
        // Stop the dialogue runner, which will cancel the current line as
        // well as the entire dialogue.
        if (runner != null)
        {
            runner.Stop();
        }
    }

    /// <summary>
    /// Called by Godot every frame to check if the <see cref="LineAdvancer"/> should take
    /// action.
    /// </summary>
    public override void _Process(double delta)
    {
        if (!string.IsNullOrWhiteSpace(hurryUpAction) && Input.IsActionJustReleased(hurryUpAction))
        {
            this.RequestLineHurryUp();
        }

        if (!string.IsNullOrWhiteSpace(nextLineAction) && Input.IsActionJustReleased(nextLineAction))
        {
            this.RequestNextLine();
        }

        if (!string.IsNullOrWhiteSpace(cancelDialogueAction) && Input.IsActionJustReleased(cancelDialogueAction))
        {
            this.RequestDialogueCancellation();
        }
    }
}