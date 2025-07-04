using System;
using Godot;
using YarnSpinnerGodot;

public partial class SQLSampleCommands : Node2D
{
    [YarnCommand("do_something")]
    public void DoSomething()
    {
        GD.Print($"My name is {Name} and I'm the one running the command called do_something!");
    }

    [YarnCommand("array_args_example")]
    public static void ArrayArgsExample(int myNumber, string[] myStrings)
    {
        GD.Print($"My number is {myNumber} and my strings are: {string.Join(", ", myStrings)}");
    }

    [YarnFunction("get_random_string")]
    public static string GetRandomString()
    {
        return Random.Shared.GetItems(new string[] { "mate", "friendo", "pal", "buddy" }, 1)[0];
    }
}