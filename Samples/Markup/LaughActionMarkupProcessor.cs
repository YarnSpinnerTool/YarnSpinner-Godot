using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Yarn.Markup;

#nullable disable
namespace YarnSpinnerGodot;

/// <summary>
/// Example of a custom ActionMarkupProcessor
/// </summary>
public partial class LaughActionMarkupProcessor : ActionMarkupHandler
{
    private List<int> _laughs = new();
    [Export] public AnimationPlayer LaughAnimationPlayer;

    public override void OnPrepareForLine(MarkupParseResult line, RichTextLabel text)
    {
    }

    public override void OnLineDisplayBegin(MarkupParseResult line, RichTextLabel text)
    {
        _laughs = [];
        foreach (var attribute in line.Attributes.Where(attribute => attribute.Name == "laugh"))
        {
            _laughs.Add(attribute.Position);
        }
    }

    public override async YarnTask OnCharacterWillAppear(int currentCharacterIndex, MarkupParseResult line,
        CancellationToken cancellationToken)
    {
        if (_laughs.Contains(currentCharacterIndex))
        {
            await PlayLaughAnimation(cancellationToken);
        }
    }

    private async Task PlayLaughAnimation(CancellationToken cancellationToken)
    {
        LaughAnimationPlayer.Play("laugh");
        while (LaughAnimationPlayer.IsPlaying() && !cancellationToken.IsCancellationRequested)
        {
            await DefaultActions.Wait(.01d);
            if (!IsInstanceValid(this))
            {
                return;
            }
        }

        LaughAnimationPlayer.Play("RESET");
    }

    public override void OnLineDisplayComplete()
    {
        LaughAnimationPlayer.Play("RESET");
        _laughs.Clear();
    }

    public override void OnLineWillDismiss()
    {
    }
}