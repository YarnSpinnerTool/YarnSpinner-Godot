using System;
using Godot;
using System.Linq;
namespace Yarn.GodotIntegration
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