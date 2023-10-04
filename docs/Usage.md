To get started, after you have followed the steps in [Installation](./Installation), create a YarnProject. You will [typically only need one YarnProject](https://docs.yarnspinner.dev/using-yarnspinner-with-unity/faq#how-many-yarn-files-should-i-have-can-my-entire-game-be-in-one-project-or-script-or-one-project-per) for your Godot project.  This can be done by clicking a folder in the Filesystem panel and click New Resource, selecting YarnProject as the type. 

You then write your dialogue .yarn files throughout your Godot project. The .yarn scripts control the flow of your dialogue. See [here](https://github.com/dogboydog/YarnDonut/blob/develop/Samples/VisualNovel/Dialogue/VNExampleDialogue.yarn) for an example Yarn script used in one of the samples.


You can follow [this documentation](https://docs.yarnspinner.dev/getting-started/writing-in-yarn) to learn about writing `.yarn` scripts. I suggest reading through the introductory sections of this link to get a sense of the syntax and capabilities of the Yarn scripting language.

The YarnProject will search for all .yarn files in the directory to which you have saved it, and any child/descendant directories.  It is not necessary to manually associate a .yarn file with your project. 

So for instance you can create this setup: 

```
res://art/Arthur.yarn // MyYarnProject will NOT include this file as it is in a directory above the YarnProject
res://dialogue/MyYarnProject.tres
res://dialogue/GameIntro.yarn  // MyYarnProject will include this file
res://dialogue/level2/merchants/Dan.yarn // MyYarnProject will also include this file
```
All the yarn files in your project are compiled into a Yarn Program whenever you edit your .yarn files, and when press "Re-compile Scripts in Project" button in the YarnProject inspector.

# Integrating with Your C# Code 

You can integrate the story in your .yarn scripts with your C# code in several ways. These all work the same way as they do in YarnSpinner-Unity.

* [Commands and Functions](https://docs.yarnspinner.dev/using-yarnspinner-with-unity/creating-commands-functions) Define a command in C# to trigger a method with no return value. You can do this by calling dialogueRunner.AddCommandHandler("CommandName", MethodName) or by adding the `[YarnCommand]` attribute to a method. If you make a command method with an `async Task` signature, YarnSpinner-Godot will `await` for the Task to complete before proceeding with your story.  Similarly you can define a yarn function, which differs from a command in that it can return a value. This is useful for example for getting number, boolean, or string values using your C# game logic.
* [Variable Storage](https://docs.yarnspinner.dev/using-yarnspinner-with-unity/components/variable-storage) The samples in this repository use the default InMemoryVariableStorage, which is sufficient if none of the values in your Yarn story need to persist after the scene is unloaded. You can also [make a custom variable storage](https://docs.yarnspinner.dev/using-yarnspinner-with-unity/components/variable-storage/custom-variable-storage) if you want to automatically save/load the story values to your game's save data, make C# variable values available as $variables, or otherwise introduce custom behavior surrounding $variables in yarn. 

# UI (Views)

This repository also comes with a few default [dialogue views](https://docs.yarnspinner.dev/using-yarnspinner-with-unity/components/dialogue-view). These will do the basics for you of displaying lines and options in Godot UI components.

The [samples](./Samples) in this repository give an example of how to set up a scene in Godot with the necessary views. You can try out the DefaultDialogueSystem.tscn sample file in the Scenes/ directory as a base of your own view. In the likely event you want to change the look and feel of this provided UI, you may be able to accomplish this just by theming and rearranging the components. For more customization, consider basing your view on the provided samples. You might want to include custom text animations, font changes, etc., in your custom view. See [Creating Custom Dialogue Views](https://docs.yarnspinner.dev/using-yarnspinner-with-unity/components/dialogue-view/custom-dialogue-views). Rather than subclassing DialogueViewBase as in Unity, however, you implement the DialogueViewBase interface and can subclass any type which derives from Godot's Node class.

Keep in mind you can also write smaller, single-purpose views, such as a dialogue view that reads each line of dialogue, checks for a `#portrait:` metadata tag on the line, and if it is present, and sets the appropriate dialogue portrait texture. 

# Localization 


You should periodically press the "Add Line Tags To Scripts" button on your YarnProject inspector to auto-generate [unique IDs](https://docs.yarnspinner.dev/getting-started/writing-in-yarn/tags-metadata#line) for your lines. This will allow YarnSpinner to correlate lines across languages.

Your dialogue will work by default in the base language you write your .yarn files in. In order to assist in the task of localizing your dialogue, you can register language codes by clicking this "Add" button on the inspector of your YarnProject: 

![image](https://github.com/dogboydog/YarnDonut/assets/9920963/588b24a6-3cfd-46a2-93c4-5ae1b683c534)

A popup will appear for your to enter the locale code for the new language. For example, `ja`. 
Then choose a path where the localization CSV for this language will live, such as `res://localization/ja.csv`. Whenever you click on the "Update Localizations" button on your YarnProject inspector, these CSVs will be created (or updated if they already exist). The base language dialogue is included in the `original` column, and by filling in the `text` column of the CSV for that language, and clicking the "Update Localizations" button again,  YarnSpinner-Godot will generate .translation files. You can then add these .translation files to your project settings, or load them on demand at runtime when switching languages. Whenever the base language text of a line changes,  if you click the "Update Localizations" button,  `(NEEDS UPDATED)` will be added to the `text` column of that line's CSV row. YarnSpinner-Godot will mark these CSVs as "Keep File (No Import)" in Godot because they are not of the format used by Godot in its localization CSV files, so Godot should not attempt to import them. 

Related: https://docs.godotengine.org/en/3.6/tutorials/i18n/internationalizing_games.html
