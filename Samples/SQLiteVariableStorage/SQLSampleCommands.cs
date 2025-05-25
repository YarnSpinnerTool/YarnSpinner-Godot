using Godot;
using YarnSpinnerGodot;

public partial class SQLSampleCommands : Node2D
{

    [YarnCommand("do_something")]
    public void DoSomething()
    {
        GD.Print($"My name is {Name} and I'm the one running the command called do_something!");
    }
}