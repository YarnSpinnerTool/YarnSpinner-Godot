using System;
using Godot;

namespace YarnSpinnerGodot;

/// <summary>
/// Wrapper which allows you to implement a YarnSpinner DialogueViewBase via
/// GDScript by calling snake_case versions of the 
/// </summary>
public partial class GDScriptDialogueViewAdapter : Node, DialogueViewBase
{
    /// <summary>
    /// Assign this node to the node with the GDScript attached to your 
    /// </summary>
    [Export] public Node GDScriptView;

    public Action requestInterrupt { get; set; }

    /// <inheritdoc/>
    public void DialogueStarted()
    {
        if (!IsInstanceValid(GDScriptView))
        {
            return;
        }

        const string gdScriptName = "dialogue_started";
        if (GDScriptView.HasMethod(gdScriptName))
        {
            GDScriptView.Call(gdScriptName);
        }
    }

    /// <inheritdoc/>
    public void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
    {
        if (!IsInstanceValid(GDScriptView))
        {
            return;
        }

        const string gdScriptName = "run_line";
        if (!GDScriptView.HasMethod(gdScriptName))
        {
            // The default implementation does nothing, and immediately calls
            // onDialogueLineFinished.
            onDialogueLineFinished?.Invoke();
        }
        else
        {
            GDScriptView.Call(gdScriptName, dialogueLine,
                Callable.From(onDialogueLineFinished));
        }
    }

    /// <inheritdoc/>
    public void InterruptLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
    {
        if (!IsInstanceValid(GDScriptView))
        {
            return;
        }

        const string gdScriptName = "interrupt_line";
        if (!GDScriptView.HasMethod(gdScriptName))
        {
            // the default implementation does nothing
            onDialogueLineFinished?.Invoke();
        }
        else
        {
            GDScriptView.Call(gdScriptName, dialogueLine,
                Callable.From(onDialogueLineFinished));
        }
    }

    /// <inheritdoc/>
    public void DismissLine(Action onDismissalComplete)
    {
        if (!IsInstanceValid(GDScriptView))
        {
            return;
        }

        const string gdScriptName = "dismiss_line";
        if (!GDScriptView.HasMethod(gdScriptName))
        {
            // The default implementation does nothing, and immediately calls
            // onDialogueLineFinished.
            onDismissalComplete?.Invoke();
        }
        else
        {
            GDScriptView.Call(gdScriptName, Callable.From(onDismissalComplete));
        }
    }

    /// <inheritdoc/>
    public void RunOptions(DialogueOption[] dialogueOptions,
        Action<int> onOptionSelected)
    {
        if (!IsInstanceValid(GDScriptView))
        {
            return;
        }

        const string gdScriptName = "run_options";
        if (GDScriptView.HasMethod(gdScriptName))
        {
            GDScriptView.Call(gdScriptName, dialogueOptions, Callable.From(onOptionSelected));
        }
    }

    /// <inheritdoc/>
    public void DialogueComplete()
    {
        if (!IsInstanceValid(GDScriptView))
        {
            return;
        }
        const string gdScriptName = "dialogue_complete";
        if (GDScriptView.HasMethod(gdScriptName))
        {
            GDScriptView.Call(gdScriptName);
        }
    }

    /// <inheritdoc/>
    public void UserRequestedViewAdvancement()
    {
        if (!IsInstanceValid(GDScriptView))
        {
            return;
        }
        const string gdScriptName = "user_requested_view_advancement";
        if (GDScriptView.HasMethod(gdScriptName))
        {
            GDScriptView.Call(gdScriptName);
        }
    }
}