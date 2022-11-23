using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Godot;
using Yarn;
using Yarn.Compiler;

public partial class Option : Godot.Object
{
    public int ID { get; private set; }
    public string Text { get; private set; }
    public string DestinationNode { get; private set; }
    public bool IsAvailable { get; private set; }

    internal static object fromOption(OptionSet.Option option, string text)
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
