using System;
using Godot;
using Newtonsoft.Json;
namespace Yarn.GodotIntegration
{
    /// <summary>
    /// A class used to serialize errors in the yarn project so that they can be displayed
    /// in more detail in the inspector
    /// </summary>
    [Serializable]
    public partial class YarnProjectError
    {
        [JsonProperty] public string FileName;
        [JsonProperty] public string Message;
        [JsonProperty] public string Context;
    }
}