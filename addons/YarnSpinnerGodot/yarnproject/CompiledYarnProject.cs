using Godot;
using Godot.Collections;
using System;

public partial class CompiledYarnProject : Resource
{
    [Export]
    public string YarnC { get; set; }

    [Export]
    public Dictionary<string, StringInfo> StringTable { get; set; }

    public CompiledYarnProject()
    {
        YarnC = "";
        StringTable = new Dictionary<string, StringInfo>();
    }

    public CompiledYarnProject(string yarnc = "", Dictionary<string, StringInfo> stringTable = null)
    {
        YarnC = yarnc;
        StringTable = stringTable;
    }
}
