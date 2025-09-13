/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/


#nullable enable

using Godot;

namespace YarnSpinnerGodot;

public partial class OptionItem : Control
{
    [Export] RichTextLabel? text;
    [Export] private BaseButton? button;
    public YarnTaskCompletionSource<DialogueOption?>? OnOptionSelected;
    public System.Threading.CancellationToken completionToken;

    private bool hasSubmittedOptionSelection = false;

    public void FocusButton()
    {
        if (!IsInstanceValid(button))
        {
            GD.PushError($"No {nameof(button)} is set on this {nameof(OptionItem)}");
            return;
        }

        button!.GrabFocus();
    }


    private DialogueOption? _option;

    public DialogueOption? Option
    {
        get => _option;

        set
        {
            if (value == null)
            {
                _option = null;
                return;
            }

            _option = value;

            hasSubmittedOptionSelection = false;

            // When we're given an Option, use its text and update our
            // interactibility.
            text!.Text = value.Line.TextWithoutCharacterName.Text;
            if (IsInstanceValid(button))
            {
                button.Disabled = !value.IsAvailable;
            }
        }
    }

    public override void _Ready()
    {
        if (!IsInstanceValid(text))
        {
            GD.PushError($"No {text} {nameof(RichTextLabel)} is set on this {nameof(OptionItem)}");
        }

        if (!IsInstanceValid(button))
        {
            GD.PushError($"No {nameof(button)} is set on this {nameof(OptionItem)}");
        }
        else
        {
            button!.Connect(BaseButton.SignalName.Pressed, Callable.From(InvokeOptionSelected));
        }
    }

    public void InvokeOptionSelected()
    {
        if (!Visible)
        {
            return;
        }

        // We only want to invoke this once, because it's an error to
        // submit an option when the Dialogue Runner isn't expecting it. To
        // prevent this, we'll only invoke this if the flag hasn't been cleared already.
        if (!hasSubmittedOptionSelection && !completionToken.IsCancellationRequested)
        {
            hasSubmittedOptionSelection = true;
            OnOptionSelected?.TrySetResult(this.Option);
        }
    }
}