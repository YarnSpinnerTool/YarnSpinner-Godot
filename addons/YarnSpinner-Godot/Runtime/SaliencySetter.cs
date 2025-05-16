#nullable enable
using Godot;
using System;
using Yarn.Saliency;

namespace YarnSpinnerGodot;

/// <summary>
/// Lets you set the saliency strategy of the chosen DialogueRunner.
/// Saliency Strategy determines how group nodes are chosen.
///
/// Note: Does not support custom saliency strategies. For that use case you would want your own script
/// that implements and sets your custom strategy. 
/// </summary>
[GlobalClass]
public partial class SaliencySetter : Node
{
    public enum SaliencyType
    {
        First,
        Best,
        BestLeastRecentlyViewed,
        RandomBestLeastRecentlyViewed
    }

    [Export] public DialogueRunner? dialogueRunner;

    [Export] public SaliencyType SaliencyStrategy = SaliencyType.First;

    public override void _EnterTree()
    {
        GD.Print("SaliencySetter ready");

        if (!IsInstanceValid(dialogueRunner))
        {
            GD.PushError(
                $"{nameof(dialogueRunner)} is not set on {nameof(SaliencySetter)}! You must set it to use this script");
            return;
        }

        
        dialogueRunner!.Ready += () =>
        {
            IContentSaliencyStrategy chosenStrategy = SaliencyStrategy switch
            {
                SaliencyType.First => new FirstSaliencyStrategy(),
                SaliencyType.Best => new BestSaliencyStrategy(),
                SaliencyType.BestLeastRecentlyViewed => new BestLeastRecentlyViewedSaliencyStrategy(
                    dialogueRunner.VariableStorage),
                SaliencyType.RandomBestLeastRecentlyViewed => new RandomBestLeastRecentlyViewedSaliencyStrategy(
                    dialogueRunner.VariableStorage),
                _ => throw new Exception("Unknown Saliency Strategy")
            };
            dialogueRunner.Dialogue.ContentSaliencyStrategy = chosenStrategy;
        };
    }
}