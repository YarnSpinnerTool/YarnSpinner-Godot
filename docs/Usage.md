To get started, after you have followed the steps in [the Beginner's Guide](https://docs.yarnspinner.dev/beginners-guide/making-a-game/yarn-spinner-for-godot), 

You can follow [this documentation](https://docs.yarnspinner.dev/getting-started/writing-in-yarn) to learn more about writing `.yarn` scripts.

There is a [Visual Studio Code Extension](https://github.com/YarnSpinnerTool/VSCodeExtension) for writing .yarn scripts with syntax highlighting and many other useful features.

In order to work with yarn scripts, you first need a `.yarnproject` file, which you can do with Project -> Tools -> YarnSpinner -> Create Yarn Project. If you edit the .yarnproject by double clicking it in the Filesystem panel, you will be able to change settings.

You can change which yarn scripts to include in your project by adding or removing glob patterns to the Yarn project. The default set of scripts to include is `**/*.yarn`, which would mean all `.yarn` files in the same directory as the `.yarnproject`, or any descendent directory would be included. These patterns are all relative to the directory where the `.yarnproject` is located. So, if you move your `.yarnproject` file, you may have to adjust your set patterns. 

You can create a new Yarn script with Project -> Tools -> YarnSpinner -> Create Yarn Script.

All the yarn files in your `.yarnproject` are compiled into a Yarn Program whenever you edit your .yarn files, and when press "Re-compile Scripts in Project" button in the YarnProject inspector.

# Integrating with Your C# Code 

You can integrate the story in your .yarn scripts with your C# code in several ways. These all work the same way as they do in YarnSpinner-Unity.

* [Commands and Functions](https://docs.yarnspinner.dev/using-yarnspinner-with-unity/creating-commands-functions) Define a command in C# to trigger a method with no return value. You can do this by calling dialogueRunner.AddCommandHandler("CommandName", MethodName) or by adding the `[YarnCommand]` attribute to a method. If you make a command method with an `async Task` signature, YarnSpinner-Godot will `await` for the Task to complete before proceeding with your story.  Similarly you can define a yarn function, which differs from a command in that it can return a value. This is useful for example for getting number, boolean, or string values using your C# game logic.
* [Variable Storage](https://docs.yarnspinner.dev/using-yarnspinner-with-unity/components/variable-storage) The samples in this repository use the default InMemoryVariableStorage, which is sufficient if none of the values in your Yarn story need to persist after the scene is unloaded. You can also [make a custom variable storage](https://docs.yarnspinner.dev/using-yarnspinner-with-unity/components/variable-storage/custom-variable-storage) if you want to automatically save/load the story values to your game's save data, make C# variable values available as Yarn `$variable`s, or otherwise introduce custom behavior surrounding variables in yarn. 

# UI (Views)

This repository also comes with a few default [dialogue views](https://docs.yarnspinner.dev/using-yarnspinner-with-unity/components/dialogue-view). These will do the basics for you of displaying lines and options in Godot UI components.

The [samples](./Samples) in this repository give an example of how to set up a scene in Godot with the necessary views. 

You can try out the DefaultDialogueSystem.tscn sample file in the Scenes/ directory as a base of your own view. 
In the likely event you want to change the look and feel of this provided UI, you may be able to accomplish this just by
theming and rearranging the components.
For more customization, consider creating your own view based on the provided samples. You might want to include custom text 
animations, font changes, etc., in your custom view. See [Creating Custom Dialogue Views](https://docs.yarnspinner.dev/using-yarnspinner-with-unity/components/dialogue-view/custom-dialogue-views). 
Rather than subclassing DialogueViewBase as in Unity, however, you implement the DialogueViewBase interface and can subclass any type which derives from Godot's Node class.


If you would like to use [BBCode](https://docs.godotengine.org/en/stable/tutorials/ui/bbcode_in_richtextlabel.html) in your yarn scripts to style and animate your text, you have a few options.  
You can't use BBCode directly in .yarn scripts, as YarnSpinner uses the `[]` characters for its own [Markup](https://yarnspinner.dev/docs/writing/markup/) feature.
One option is to use the Markup feature in your .yarn scripts, and write a custom view that fills in BBCode tags based on your markup.

The example LineView in the plugin provides the `ConvertHTMLToBBCode` setting,
a simple way to use HTML style tags and convert them to BBCode at runtime, but this may not suit every use case. 
It will attempt to convert paired instances of `<`and `>` to `[` and `]`. For example `<wave amp=5>Hello!</wave>` would be converted
to the BBCode `[wave amp=5]Hello![/wave]`

Here's an example of this feature in action, using some built-in BBCode effects. 


https://github.com/YarnSpinnerTool/YarnSpinner-Godot/assets/9920963/5e372fcf-07c3-4764-90d1-43d789a1c6b6


Keep in mind you can also write smaller, single-purpose views, such as a dialogue view that reads each line of dialogue,
checks for a `#portrait:` metadata tag on the line, and if it is present, and displays the appropriate dialogue portrait texture. 

# Localization 


You should periodically press the "Add Line Tags To Scripts" button on your YarnProject inspector to auto-generate [unique IDs](https://docs.yarnspinner.dev/getting-started/writing-in-yarn/tags-metadata#line) for your lines. This will allow YarnSpinner to correlate lines across languages.

Your dialogue will work by default in the base language you write your .yarn files in. You can change the base locale code by editing the text in the input next to "Base language" on your `.yarnproject`, for example changing `en` to `es`, and clicking the "Change" button.

In order to assist in the task of localizing your dialogue, you can add new languages by clicking entering a language code and clicking add. For example, `ja` or `es`. 
Then click Browse next to the language code you added, choose a path where the localization CSV for this language will live, such as `res://localization/ja.csv`. Whenever you click on the "Update Localizations" button on your YarnProject inspector, these CSVs will be created (or updated if they already exist). The base language dialogue is included in the `original` column, and by filling in the `text` column of the CSV for that language, and clicking the "Update Localizations" button again,  YarnSpinner-Godot will generate .translation files. You can then add these .translation files to your project settings, or load them on demand at runtime when switching languages. Whenever the base language text of a line changes,  if you click the "Update Localizations" button,  `(NEEDS UPDATED)` will be added to the `text` column of that line's CSV row. YarnSpinner-Godot will mark these CSVs as "Keep File (No Import)" in Godot because they are not of the format used by Godot in its localization CSV files, so Godot should not attempt to import them. 

Related: https://docs.godotengine.org/en/3.6/tutorials/i18n/internationalizing_games.html
