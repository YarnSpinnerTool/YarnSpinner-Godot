#nullable disable
using Godot;
using System;
namespace Samples.Space;

public partial class DialogueTarget : Node
{
    /// <summary>
    /// Node name if the player talks to this NPC
    /// </summary>
    [Export] public string nodeName;
}