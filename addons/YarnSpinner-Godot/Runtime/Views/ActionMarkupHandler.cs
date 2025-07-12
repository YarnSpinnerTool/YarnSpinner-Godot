using System.Threading;
using Godot;
using Yarn.Markup;

namespace YarnSpinnerGodot;

/// <summary>
/// This is an abstract Node that conforms to the <see cref="IActionMarkupHandler"/> interface.
/// </summary>
/// <remarks>
/// <para>
/// Intended to be used in situations where you require a Node version of the interfaces.
/// This is used by <see cref="LinePresenter"/> to have a list of handlers that can be connected up via the inspector.
/// </para>
/// </remarks>
public abstract partial class ActionMarkupHandler : Godot.Node, IActionMarkupHandler
{
    /// <inheritdoc/>
    public abstract void OnPrepareForLine(MarkupParseResult line, RichTextLabel text);

    /// <inheritdoc/>
    public abstract void OnLineDisplayBegin(MarkupParseResult line, RichTextLabel text);

    /// <inheritdoc/>
    public abstract YarnTask OnCharacterWillAppear(int currentCharacterIndex, MarkupParseResult line,
        CancellationToken cancellationToken);

    /// <inheritdoc/>
    public abstract void OnLineDisplayComplete();

    /// <inheritdoc/>
    public abstract void OnLineWillDismiss();
}