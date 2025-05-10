#nullable disable
using System;
using Godot;

#pragma warning disable CS0618 // Type or member is obsolete
namespace YarnSpinnerGodot;

/// <summary>
/// Obsolete: Use <see cref="OptionItem"/>
/// </summary>
public partial class OptionView : BaseButton
{
    [Export] bool showCharacterName = false;
    [Export] private RichTextLabel label;
    public Action<DialogueOption> OnOptionSelected;
    public MarkupPalette palette;

    private DialogueOption _option;

    private bool hasSubmittedOptionSelection = false;

    public DialogueOption Option
    {
        get => _option;

        set
        {
            _option = value;

            hasSubmittedOptionSelection = false;

            // When we're given an Option, use its text and update our
            // disabled/enabled state 
            Yarn.Markup.MarkupParseResult line;
            if (showCharacterName)
            {
                line = value.Line.Text;
            }
            else
            {
                line = value.Line.TextWithoutCharacterName;
            }

            label.BbcodeEnabled = true;
            if (IsInstanceValid(palette))
            {
                label.Text =
                    $"[center]{LineView.PaletteMarkedUpText(line, palette)}[/center]";
            }
            else
            {
                label.Text = $"[center]{line.Text}[/center]";
            }

            Disabled = !value.IsAvailable;
        }
    }

    public override void _Ready()
    {
        Connect(BaseButton.SignalName.Pressed, Callable.From(InvokeOptionSelected));
    }

    /// <summary>
    /// Handler for when the option view is pressed. Will mark the option
    /// associated with this view as the one that was selected, to proceed
    /// with the dialogue.
    /// </summary> 
    public void InvokeOptionSelected()
    {
        // We only want to invoke this once, because it's an error to
        // submit an option when the Dialogue Runner isn't expecting it. To
        // prevent this, we'll only invoke this if the flag hasn't been cleared already.
        if (hasSubmittedOptionSelection)
        {
            return;
        }

        OnOptionSelected.Invoke(Option);
        hasSubmittedOptionSelection = true;
    }
}