/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

#nullable enable

using System.Text.RegularExpressions;

namespace YarnSpinnerGodot;

using System;
using System.Collections.Generic;
using System.Threading;
using Godot;

/// <summary>
/// An implementation of <see cref="IAsyncTypewriter"/> that delivers
/// words one at a time, and invokes any <see
/// cref="IActionMarkupHandler"/>s along the way as needed.
/// </summary>
public partial class WordTypewriter : IAsyncTypewriter
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

    /// <summary>
    /// The number of words per second to deliver.
    /// </summary>
    /// <remarks>If this value is zero, all words are delivered at
    /// once, subject to any delays added by the markup handlers in <see
    /// cref="ActionMarkupHandlers"/>.</remarks>
    public float WordsPerSecond { get; set; } = 0f;

    /// <inheritdoc/>
    public async YarnTask RunTypewriter(Yarn.Markup.MarkupParseResult line, CancellationToken cancellationToken)
    {
        // ok so this will have to do the following:
        // work out where the pauses are meant to be
        // do this by finding all the breaks in the line
        // then at each point in the line we move char by char
        // when we hit a break point (which we know in advance)

        if (TextElement == null)
        {
            GD.PushWarning($"Can't show text as typewriter, because {nameof(TextElement)} was not provided");
        }
        else
        {
            TextElement.VisibleCharacters = 0;
            TextElement.Text = line.Text;
            this.ConvertHTMLToBBCodeIfConfigured();
            // Let every markup handler know that display is about to begin
            foreach (var markupHandler in ActionMarkupHandlers)
            {
                markupHandler.OnLineDisplayBegin(line, TextElement);
            }

            double secondsPerWord = 0;
            if (WordsPerSecond > 0)
            {
                secondsPerWord = 1.0 / WordsPerSecond;
            }

            var wordBoundaries = new SortedSet<int>();
            var parsedText = TextElement.GetParsedText();
            var lastChar = ' ';
            for (int i = 0; i < parsedText.Length; i++)
            {
               
                if (!char.IsWhiteSpace(lastChar) && char.IsWhiteSpace(parsedText[i]))
                {
                    wordBoundaries.Add(i);
                }

                lastChar = parsedText[i];
            }

            // Get the count of visible characters from the RichTextLabel to exclude markup characters
            var visibleCharacterCount = parsedText.Length;

            // Start with a full time budget so that we immediately show the first character
            double accumulatedDelay = secondsPerWord;

            int current = wordBoundaries.Min;

            // Go through each character of the line and letting the
            // processors know about it
            for (int i = 0; i < visibleCharacterCount; i++)
            {
                // if we are at the character that requires waiting we want to wait until we hit the allotted time
                if (i == current)
                {
                    // If we don't already have enough accumulated time budget for a word, wait until we do (or until we're cancelled)
                    while (!cancellationToken.IsCancellationRequested && (accumulatedDelay < secondsPerWord))
                    {
                        var timeBeforeYield = Time.GetTicksMsec() / 1000f;
                        await YarnTask.Yield();
                        var timeAfterYield = Time.GetTicksMsec() / 1000f;
                        accumulatedDelay += timeAfterYield - timeBeforeYield;
                    }

                    accumulatedDelay -= secondsPerWord;

                    wordBoundaries.Remove(current);
                    if (wordBoundaries.Count > 0)
                    {
                        current = wordBoundaries.Min;
                    }
                    else
                    {
                        current = int.MaxValue;
                    }
                }

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
        }

        // Let each markup handler know the line has finished displaying
        foreach (var markupHandler in ActionMarkupHandlers)
        {
            markupHandler.OnLineDisplayComplete();
        }
    }

    public void PrepareForContent(Yarn.Markup.MarkupParseResult line)
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