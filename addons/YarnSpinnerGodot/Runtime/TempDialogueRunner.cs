using System;
using Godot;
using Godot.Collections;
using Yarn;
using Yarn.Markup;
using Array = Godot.Collections.Array;
using Node = Godot.Node;
public partial class TempDialogueRunner : Node
{
	[Signal]
	public delegate void HandleLineEventHandler(string text, Array<string> tags, Array<MarkupAttribute> attributes);

    [Signal]
    public delegate void HandleOptionsEventHandler();

    [Export(PropertyHint.ResourceType, "CompiledYarnProject")]
	private CompiledYarnProject CompiledYarnProject;

	[Export(PropertyHint.Enum, "en,nl")]
	private string LanguageCode = "en";

	[Export]
	private string StartNode = "Start";

    private IVariableStorage _storage = new MemoryVariableStore();

	private Dialogue _dialogue;

    public string CurrentNode()
    {
		return _dialogue.CurrentNode;
    }

	public bool GetVariableAsBool(string name)
	{
		bool result = false;
		_storage.TryGetValue(name, out result);
		return result;
	}

    public string GetVariableAsString(string name)
    {
        string result = "";
        _storage.TryGetValue(name, out result);
        return result;
    }

    public float GetVariableAsFloat(string name)
    {
        float result = 0.0f;
        _storage.TryGetValue(name, out result);
        return result;
    }

    public override void _Ready()
	{
		CreateDialogue();
        if (CompiledYarnProject != null)
		{
			LoadYarnProject();
		}
	}

	public void LoadYarnProject()
	{
		var bytes = Convert.FromBase64String(CompiledYarnProject.YarnC);
		Program program = Program.Parser.ParseFrom(bytes);
		_dialogue.SetProgram(program);
		_dialogue.SetNode(StartNode);
		_dialogue.Continue();
	}

	public void Continue()
	{
		_dialogue.Continue();
	}

	private void LogDebugMessage(string message)
	{
		GD.Print(message);
	}

	private void LogErrorMessage(string message)
	{
		GD.PrintErr(message);
	}

	private void LineHandler(Line line)
	{
		if (CompiledYarnProject == null)
			throw new Exception("no compiled yarn project found");
		if (CompiledYarnProject.StringTable == null)
            throw new Exception("no string table found on yarn project!");
		if (!CompiledYarnProject.StringTable.ContainsKey(line.ID))
            throw new Exception("line not found in string table!");
        var stringInfo = CompiledYarnProject.StringTable[line.ID];
		var text = String.Format(stringInfo.text, line.Substitutions);
		MarkupParseResult markupParseResult = _dialogue.ParseMarkup(text);
        Array<MarkupAttribute> attributes = new Array<MarkupAttribute>();
		foreach (Yarn.Markup.MarkupAttribute attribute in markupParseResult.Attributes)
		{
			attributes.Add(MarkupAttribute.fromMarkupAttribute(attribute));
		}
		EmitSignal("HandleLine", markupParseResult.Text, stringInfo.metadata, attributes);
	} 

	private void OptionsHandler(OptionSet options)
	{
        if (CompiledYarnProject == null)
            throw new Exception("no compiled yarn project found");
        if (CompiledYarnProject.StringTable == null)
            throw new Exception("no string table found on yarn project!");
		var result = new Array();
		foreach (OptionSet.Option option in options.Options)
		{
            var stringInfo = CompiledYarnProject.StringTable[option.Line.ID];
            var text = String.Format(stringInfo.text, option.Line.Substitutions);
            result.Add(Variant.From(Option.fromOption(option, text)));
		}
        EmitSignal("HandleOptions", result);
    }

    private void CreateDialogue()
	{
		_dialogue = new Dialogue(_storage);
		_dialogue.LanguageCode = "en";
		_dialogue.LogDebugMessage = LogDebugMessage;
		_dialogue.LogErrorMessage = LogErrorMessage;
		_dialogue.LineHandler = LineHandler;
		_dialogue.OptionsHandler = OptionsHandler;
		_dialogue.CommandHandler = command =>
		{
			GD.Print(command);
		};

		_dialogue.NodeCompleteHandler = nodeName =>
		{
			GD.Print($"Node complete: {nodeName}");
		};

		_dialogue.DialogueCompleteHandler = () =>
		{
            GD.Print("Dialogue complete");
        };
	}
}
