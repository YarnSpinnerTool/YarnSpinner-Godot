#nullable enable

using System.Text.RegularExpressions;

namespace YarnSpinnerGodot;

using System;
using System.Collections.Generic;
using System.Threading;
using Godot;

/// <summary>
/// An implementation of <see cref="IAsyncTypewriter"/> that delivers
/// characters one at a time, and invokes any <see
/// cref="IActionMarkupHandler"/>s along the way as needed.
/// </summary>
public class BasicTypewriter : IAsyncTypewriter
{
    /// <summary>
    /// The <see cref="TMP_Text"/> to display the text in.
    /// </summary>
    public RichTextLabel? Text { get; set; }

    /// <summary>
    /// A collection of <see cref="IActionMarkupHandler"/> objects that
    /// should be invoked as needed during the typewriter's delivery in <see
    /// cref="RunTypewriter"/>, depending upon the contents of a line.
    /// </summary>
    public IEnumerable<IActionMarkupHandler> ActionMarkupHandlers { get; set; } = Array.Empty<IActionMarkupHandler>();

    /// <summary>
    /// The number of characters per second to deliver.
    /// </summary>
    /// <remarks>If this value is zero, all characters are delivered at
    /// once, subject to any delays added by the markup handlers in <see
    /// cref="ActionMarkupHandlers"/>.</remarks>
    public float CharactersPerSecond { get; set; } = 0f;

    /// <summary>
    /// Whether we will replace <> characters with [] to display them as BBCode.
    /// </summary>
    public bool ConvertHTMLToBBCode;

    /// <inheritdoc/>
    public async YarnTask RunTypewriter(Yarn.Markup.MarkupParseResult line, CancellationToken cancellationToken)
    {
        if (Text == null)
        {
            GD.PushWarning($"Can't show text as typewriter, because {nameof(Text)} was not provided");
        }
        else
        {
            Text.VisibleCharacters = 0;
            Text.Text = line.Text;
            ConvertHTMLToBBCodeIfConfigured();

            // Let every markup handler know that display is about to begin
            foreach (var markupHandler in ActionMarkupHandlers)
            {
                markupHandler.OnLineDisplayBegin(line, Text);
            }

            double secondsPerCharacter = 0;
            if (CharactersPerSecond > 0)
            {
                secondsPerCharacter = 1.0 / CharactersPerSecond;
            }

            // Get the count of visible characters from RichTextLabel to exclude BBCode characters
            var visibleCharacterCount = Text.GetParsedText().Length;

            // Start with a full time budget so that we immediately show the first character
            double accumulatedDelay = secondsPerCharacter;

            // Go through each character of the line and letting the
            // processors know about it
            for (int i = 0; i < visibleCharacterCount; i++)
            {
                // If we don't already have enough accumulated time budget
                // for a character, wait until we do (or until we're
                // cancelled)
                while (!cancellationToken.IsCancellationRequested
                       && (accumulatedDelay < secondsPerCharacter))
                {
                    var timeBeforeYield = Time.GetTicksMsec() / 1000f;
                    await YarnTask.Yield();
                    var timeAfterYield = Time.GetTicksMsec() / 1000f;
                    accumulatedDelay += timeAfterYield - timeBeforeYield;
                }

                // Tell every markup handler that it is time to process the
                // current character
                foreach (var processor in ActionMarkupHandlers)
                {
                    await processor
                        .OnCharacterWillAppear(i, line, cancellationToken)
                        .SuppressCancellationThrow();
                }

                Text.VisibleCharacters += 1;

                accumulatedDelay -= secondsPerCharacter;
            }

            // We've finished showing every character (or we were
            // cancelled); ensure that everything is now visible.
            Text.VisibleRatio = 1.0f;
        }

        // Let each markup handler know the line has finished displaying
        foreach (var markupHandler in ActionMarkupHandlers)
        {
            markupHandler.OnLineDisplayComplete();
        }
    }

    /// <summary>
    /// If <see cref="ConvertHTMLToBBCode"/> is true, replace any HTML tags in the line text and
    /// character name text with BBCode tags.
    /// </summary>
    private void ConvertHTMLToBBCodeIfConfigured()
    {
        if (ConvertHTMLToBBCode)
        {
            Text!.Text = Regex.Replace(Text.Text, LinePresenter.HtmlTagPattern, "[$1]");
        }
    }
}