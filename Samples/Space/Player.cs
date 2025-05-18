#nullable disable
using Godot;
using Godot.Collections;
using YarnSpinnerGodot;
namespace Samples.Space;

/// <summary>
/// Script for controlling the player and triggering dialogue
/// in the space sample
/// </summary>
public partial class Player : CharacterBody2D
{

    /// <summary>
    /// Collision shape used to check for NPCs to chat with 
    /// </summary>
    [Export] public CollisionShape2D IntersectShape;

    [Export] public  DialogueRunner DialogueRunner;
    // we start the intro dialogue automatically, so prevent the player from moving at first
    private bool _dialoguePlaying = true; 

    private const float X_SPEED = 480f;
    private const int NPC_LAYER = 2; // NPCs in the demo are in layer 2

    public override void _Ready()
    {
        DialogueRunner.onDialogueComplete += SetDialogueNotPlaying;
        DialogueRunner.onDialogueStart += DialogueStarted;
        DialogueRunner.onNodeComplete += NodeCompleted;
    }
    public override void _PhysicsProcess(double delta)
    {
        if (_dialoguePlaying)
        {
            return;
        }

        if (Input.IsActionPressed("right"))
        {
            Velocity = new Vector2(X_SPEED, 0f);
        
        }
        else if (Input.IsActionPressed("left"))
        {
            Velocity = new Vector2(-X_SPEED, 0f);
        }
        else
        {
            Velocity *= 0.1f; // skid to stop
        }
        MoveAndSlide();
        if (Input.IsActionPressed("interact"))
        {
            var world = GetWorld2D();
            var spaceState = world.DirectSpaceState;
            var hitCollider = spaceState.IntersectShape(new  PhysicsShapeQueryParameters2D()
            {
                CollisionMask = 1 << (NPC_LAYER - 1),
                Margin = 40,
                ShapeRid = IntersectShape.Shape.GetRid(),
                Transform = IntersectShape.GlobalTransform,
                CollideWithAreas = true,
                CollideWithBodies = true
            });
            foreach (Dictionary colliderCheck in hitCollider)
            {
                if (colliderCheck.ContainsKey("collider"))
                {
                    var colliderNode = ((Node)colliderCheck["collider"]);
                    if (colliderNode.HasNode(nameof(DialogueTarget)))
                    {
                        var target = colliderNode.GetNode<DialogueTarget>(nameof(DialogueTarget));
                        _dialoguePlaying = true;
                        DialogueRunner.StartDialogue(target.nodeName);
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Example of connecting to onDialogueStart from <see cref="DialogueRunner"/>
    /// </summary>
    private void DialogueStarted()
    {
        GD.Print("Dialogue started in space sample");
    }
        
    /// <summary>
    /// Example of connecting to onNodeComplete from <see cref="DialogueRunner"/>
    /// </summary>
    private void NodeCompleted(string nodeName)
    {
        GD.Print($"Dialogue node {nodeName} completed in space sample");
    }
    private void SetDialogueNotPlaying()
    {
        _dialoguePlaying = false;
    }
}