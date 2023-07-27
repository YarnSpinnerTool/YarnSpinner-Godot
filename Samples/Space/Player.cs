using Godot;
using Godot.Collections;
using YarnDonut;
namespace Samples.Space
{
    /// <summary>
    /// Script for controlling the player and triggering dialogue
    /// in the space sample
    /// </summary>
    public class Player : KinematicBody2D
    {

        /// <summary>
        /// Collision shape used to check for NPCs to chat with 
        /// </summary>
        [Export] public NodePath intersectShapePath;
        private CollisionShape2D _intersectShape;

        [Export] public NodePath dialogueRunnerPath;
        private DialogueRunner _dialogueRunner;
        private bool _dialoguePlaying;

        private const float X_SPEED = 480f;
        private const int NPC_LAYER = 2; // NPCs in the demo are in layer 2
        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _intersectShape = GetNode<CollisionShape2D>(intersectShapePath);
            _dialogueRunner = GetNode<DialogueRunner>(dialogueRunnerPath);
        }

        public override void _PhysicsProcess(float delta)
        {
            if (_dialoguePlaying)
            {
                return;
            }
            if (Input.IsActionPressed("right"))
            {
                MoveAndSlide(new Vector2(X_SPEED, 0f));
            }
            else if (Input.IsActionPressed("left"))
            {
                MoveAndSlide(new Vector2(-X_SPEED, 0f));
            }

            if (Input.IsActionPressed("interact"))
            {
                var world = GetWorld2d();
                var spaceState = world.DirectSpaceState;
                var hitCollider = spaceState.IntersectShape(new Physics2DShapeQueryParameters
                {
                    CollisionLayer = 1 << (NPC_LAYER - 1),
                    Margin = 40,
                    ShapeRid = _intersectShape.Shape.GetRid(),
                    Transform = _intersectShape.GlobalTransform,
                    CollideWithAreas = true,
                    CollideWithBodies = true
                });
                foreach (Dictionary colliderCheck in hitCollider)
                {
                    if (colliderCheck.Contains("collider"))
                    {
                        var colliderNode = ((Node)colliderCheck["collider"]);
                        if (colliderNode.HasNode(nameof(DialogueTarget)))
                        {
                            var target = colliderNode.GetNode<DialogueTarget>(nameof(DialogueTarget));
                            _dialoguePlaying = true;
                            _dialogueRunner.onDialogueComplete += () =>
                            {
                                _dialoguePlaying = false;
                            };
                            _dialogueRunner.StartDialogue(target.nodeName);
                            break;
                        }
                    }
                }
            }
        }
    }
}