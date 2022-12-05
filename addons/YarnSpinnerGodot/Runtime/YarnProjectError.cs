using System;
using Godot;
using Newtonsoft.Json;
namespace Yarn.GodotIntegration
{
    /// <summary>
    /// A class used to serialize errors in the yarn project so that they can be displayed
    /// in more detail in the inspector
    /// </summary>
    [Serializable] [Tool]
    public partial class YarnProjectError : Resource
    {
        [Export]public string FileName;
        [Export(PropertyHint.MultilineText)] public string Message;
        [Export(PropertyHint.MultilineText)] public string Context;
    }
}