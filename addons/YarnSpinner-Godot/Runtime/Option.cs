#nullable disable
using System;
using Godot;
using Yarn;

public class Option 
{
    public int ID { get; private set; }
    public string Text { get; private set; }
    public bool IsAvailable { get; private set; }

    public static object fromOption(OptionSet.Option option, string text)
    {
        return new Option
        {
            ID = option.ID,
            Text = text,
            IsAvailable = option.IsAvailable
        };
    }

    public override string ToString()
    {
        return String.Format($"[Option: {Text} {IsAvailable}");
    }
}
