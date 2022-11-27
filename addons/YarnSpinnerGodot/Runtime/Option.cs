using System;
using Yarn;
using Object = Godot.Object;
public class Option : Object
{
    public int ID { get; private set; }
    public string Text { get; private set; }
    public string DestinationNode { get; private set; }
    public bool IsAvailable { get; private set; }

    public static object fromOption(OptionSet.Option option, string text)
    {
        return new Option
        {
            ID = option.ID,
            Text = text,
            DestinationNode = option.DestinationNode,
            IsAvailable = option.IsAvailable
        };
    }

    public override string ToString()
    {
        return String.Format($"[Option: {Text} {IsAvailable} To:{DestinationNode}]");
    }
}
