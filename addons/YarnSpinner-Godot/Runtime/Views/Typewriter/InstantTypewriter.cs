/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

#nullable enable

using System.Diagnostics;

namespace YarnSpinnerGodot;

using System;
using System.Collections.Generic;
using System.Threading;
using Godot;
using Yarn.Markup;
 

/// <summary>
/// An implementation of <see cref="IAsyncTypewriter"/> that delivers
/// all content instantly, and invokes any <see
/// cref="IActionMarkupHandler"/>s along the way as needed.
/// </summary>
public class InstantTypewriter : IAsyncTypewriter
{
    /// <summary>
    /// The RichTextLabel> to display the text in.
    /// </summary>
    public RichTextLabel? TextElement { get; set; }

    public bool ConvertHTMLToBBCode { get; set; }

    /// <summary>
    /// A collection of <see cref="IActionMarkupHandler"/> objects that
    /// should be invoked as needed during the typewriter's delivery in <see
    /// cref="RunTypewriter"/>, depending upon the contents of a line.
    /// </summary>
    public List<IActionMarkupHandler> ActionMarkupHandlers { get; set; } = new();

    /// <inheritdoc/>
    public async YarnTask RunTypewriter(Yarn.Markup.MarkupParseResult line, CancellationToken cancellationToken)
    {
        if (TextElement == null)
        {
           GD.PushWarning($"Can't show text as typewriter, because {nameof(TextElement)} was not provided");
            return;
        }

        TextElement.VisibleCharacters = 0;
        TextElement.Text = line.Text;
        this.ConvertHTMLToBBCodeIfConfigured();
        // Let every markup handler know that display is about to begin
        foreach (var markupHandler in ActionMarkupHandlers)
        {
            markupHandler.OnLineDisplayBegin(line, TextElement);
        }

        var textInfo = TextElement.GetParsedText();
        // Get the count of visible characters from the RichTextLabel to exclude markup characters
        var visibleCharacterCount = textInfo.Length;

        // Go through each character of the line and letting the
        // processors know about it
        for (int i = 0; i < visibleCharacterCount; i++)
        {
            // Tell every markup handler that it is time to process the
            // current character
            foreach (var processor in ActionMarkupHandlers)
            {
                await processor
                    .OnCharacterWillAppear(i, line, cancellationToken)
                    .SuppressCancellationThrow();
            }

            TextElement.VisibleCharacters += 1;
        }

        // We've finished showing every character (or we were
        // cancelled); ensure that everything is now visible.
        TextElement.VisibleCharacters = visibleCharacterCount;

        // Let each markup handler know the line has finished displaying
        foreach (var markupHandler in ActionMarkupHandlers)
        {
            markupHandler.OnLineDisplayComplete();
        }
    }

    public void PrepareForContent(MarkupParseResult line)
    {
        if (TextElement == null)
        {
            return;
        }

        TextElement.VisibleCharacters = 0;
        TextElement.Text = line.Text;

        foreach (var processor in ActionMarkupHandlers)
        {
            processor.OnPrepareForLine(line, TextElement);
        }
    }

    public void ContentWillDismiss()
    {
        // we tell all action processors that the line is finished and is about to go away
        foreach (var processor in ActionMarkupHandlers)
        {
            processor.OnLineWillDismiss();
        }
    }

    public void ContentDidDismiss()
    {
        if (TextElement == null)
        {
            return;
        }
        TextElement.VisibleCharacters = 0;
    }
}