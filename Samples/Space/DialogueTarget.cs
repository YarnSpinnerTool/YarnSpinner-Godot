using Godot;
using System;
namespace Samples.Space
{
    public class DialogueTarget : Node
    {
        /// <summary>
        /// Node name if the player talks to this NPC
        /// </summary>
        [Export] public string nodeName;
    }
}