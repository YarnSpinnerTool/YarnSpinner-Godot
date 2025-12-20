#nullable disable
using Godot;
using System;
using Yarn;
using Yarn.Markup;

namespace YarnSpinnerGodot;

public partial class DialogueRunner 
{
    /// <summary>
    /// Convert a LocalizedLine to a Godot Dictionary that is more accessible from GDScript.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static Godot.Collections.Dictionary MarkupParseResultToDict(MarkupParseResult text)
    {
        var returnValue = new Godot.Collections.Dictionary();
        returnValue["text"] = text.Text;

        var attributesList = new Godot.Collections.Array();
        foreach (var attribute in text.Attributes)
        {
            var attributeDict = new Godot.Collections.Dictionary();
            attributeDict["name"] = attribute.Name;
            attributeDict["length"] = attribute.Length;
            attributeDict["position"] = attribute.Position;
            var propertiesList = new Godot.Collections.Dictionary();
            foreach (var property in attribute.Properties)
            {
                var castValue = property.Value.Type switch
                {
                    MarkupValueType.Integer => Variant.From(property.Value.IntegerValue),
                    MarkupValueType.Float => Variant.From(property.Value.FloatValue),
                    MarkupValueType.String => Variant.From(property.Value.StringValue),
                    MarkupValueType.Bool => Variant.From(property.Value.BoolValue),
                    _ => new Variant(),
                };

                propertiesList[property.Key] = castValue;
            }

            attributeDict["properties"] = propertiesList;
            attributesList.Add(attributeDict);
        }

        returnValue["attributes"] = attributesList;
        return returnValue;
    }

    /// <summary>
    /// Convert a LocalizedLine to a Godot Dictionary that is more accessible from GDScript. 
    /// 	# example: 
    ///  {"metadata":["my_metadata"],
    ///  "text":
    ///      {
    ///        "attributes":[
    ///            { "length":8,"name":"fx","position":20,"properties":[{"type":"wave"}]},
    ///         	{ "length":6,"name":"character","position":0,"properties":[{"name":"Gary"}]}
    ///      ],
    ///      "text":"Gary: So, can I use GDScript with YarnSpinner?",
    ///      "text_without_character_name":"So, can I use GDScript with YarnSpinner?"
    ///   }
    ///  }
    /// </summary>
    /// <param name="dialogueLine"></param>
    /// <returns></returns>
    public static Godot.Collections.Dictionary LocalizedLineToDict(LocalizedLine dialogueLine)
    {
        var dialogueLineDict = new Godot.Collections.Dictionary();

        dialogueLineDict["raw_text"] = dialogueLine.RawText;
        dialogueLineDict["text_id"] = dialogueLine.TextID;

        var metadataArray = new Godot.Collections.Array();
        metadataArray.AddRange(dialogueLine.Metadata ?? Array.Empty<string>());
        dialogueLineDict["metadata"] = metadataArray;

        var textDict = MarkupParseResultToDict(dialogueLine.Text);
        textDict["text_without_character_name"] = dialogueLine.TextWithoutCharacterName.Text; // for backwards compatibility

        dialogueLineDict["text"] = textDict;

        var subList = new Godot.Collections.Array();
        subList.AddRange(dialogueLine.Substitutions ?? Array.Empty<String>());
        dialogueLineDict["substitutions"] = subList;

        return dialogueLineDict;
    }

    /// <inheritdoc/>
    /// Implement a GDScript method run_options(options: Array, on_option_selected: Callable (single int parameter)) -> void 
    public static Godot.Collections.Array DialogueOptionsToDictArray(DialogueOption[] dialogueOptions)
    {
        var dictOptions = new Godot.Collections.Array();
        foreach (var option in dialogueOptions)
        {
            var optionDict = new Godot.Collections.Dictionary();
            optionDict["dialogue_option_id"] = option.DialogueOptionID;
            optionDict["text_id"] = option.TextID;
            optionDict["line"] = LocalizedLineToDict(option.Line);
            optionDict["is_available"] = option.IsAvailable;
            dictOptions.Add(optionDict);
        }

        return dictOptions;
    }
}
