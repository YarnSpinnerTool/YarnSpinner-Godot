using System;
using Godot;

namespace YarnDonut
{
    [Serializable]
    [Tool]
    public class FunctionInfo
    {
        public string Name;
        public string ReturnType;
        public string[] Parameters;
    }
}