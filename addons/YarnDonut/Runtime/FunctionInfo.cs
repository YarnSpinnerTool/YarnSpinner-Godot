using System;
using Godot;
namespace YarnDonut
{
    [Serializable]
    [Tool]
    public partial class FunctionInfo : Resource
    {
        [Export] public string Name;
        [Export] public string ReturnType;
        [Export] public string[] Parameters;
        
    }
}