/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Threading;
using Godot;
using Yarn.Markup;
#nullable enable
namespace YarnSpinnerGodot;

public partial class LinePresenterButtonHandler : ActionMarkupHandler
{
    /// <summary>
    /// The <see cref="Button"/> that represents an on-screen button that
    /// the user can click to continue to the next piece of dialogue or skip the
    /// any animations that are playing.
    /// </summary>
    /// <seealso cref="autoAdvance"/>
    [Export] Button? continueButton;

    /// <summary>
    /// Reference to the dialogue runner.
    /// </summary>
    [Export] DialogueRunner? dialogueRunner;

    private bool _displayComplete;

    public override void _Ready()
    {
        if (!IsInstanceValid(continueButton))
        {
            GD.PushWarning($"The {nameof(continueButton)} is null, is it not connected in the inspector?", this);
            return;
        }

        _displayComplete = false;
        continueButton.Disabled = true;
    }

    public override void OnPrepareForLine(MarkupParseResult line, RichTextLabel text)
    {
        if (continueButton == null)
        {
            GD.PushWarning($"The {nameof(continueButton)} is null, is it not connected in the inspector?", this);
            return;
        }

        // enable the button
        _displayComplete = false;
        continueButton.Disabled = false;
        continueButton.Pressed += OnClick;
    }

    public void OnClick()
    {
        if (!IsInstanceValid(continueButton))
        {
            GD.PushWarning($"Continue button was clicked, but {nameof(continueButton)} is null!", this);
            return;
        }

        if (!IsInstanceValid(dialogueRunner))
        {
            GD.PushWarning($"Continue button was clicked, but {nameof(dialogueRunner)} is null!", this);
            return;
        }

        if (_displayComplete)
        {
            dialogueRunner.RequestNextLine();
        }
        else
        {
            dialogueRunner.RequestHurryUpLine();
        }
    }

    public override void OnLineDisplayBegin(MarkupParseResult line, RichTextLabel text)
    {
    }


    public override YarnTask OnCharacterWillAppear(int currentCharacterIndex, MarkupParseResult line,
        CancellationToken cancellationToken)
    {
        return YarnTask.CompletedTask;
    }

    public override void OnLineDisplayComplete()
    {
        _displayComplete = true;
    }

    public override void OnLineWillDismiss()
    {
        if (!IsInstanceValid(continueButton))
        {
            return;
        }

        // disable interaction
        continueButton.Pressed -= OnClick;
        continueButton.Disabled = true;
    }
}