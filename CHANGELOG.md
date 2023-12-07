# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [0.2.0] 2024-?-?

### Added

* This plugin now integrates with `.yarnproject` files, which are JSON formatted files that define the settings used to work with your `.yarn` scripts. The C# YarnProject custom resource for Godot is generated and updated automatically by the plugin. You can use the `.yarnproject` file in the editor for drag-n-drop inspector functionality for YarnProjects. For example, you can drag your .yarnproject file into the Yarn Project slot on your DialogueRunner instances.
* The .tres file containing the compiled program and other generated data relating to the .yarnproject is now stored in the `.godot` directory rather than in the main directories of your project. As such, it will no longer be version controlled in most cases as `.godot` is typically ignored. However just opening your Godot project with this plugin enabled should re-generate the necessary files if you need to delete your `.godot` folder for any reason or re-clone your project.
* Some other software such as the [VS Code Extension](https://docs.yarnspinner.dev/getting-started/editing-with-vs-code/installing-the-extension) can also use these files. This integration was added partially to support cross-engine tooling that relies on .yarnproject files, as well as bringing the workflow of using the Godot plugin closer to the original Unity plugin.
* Please use the Tools > YarnSpinner >Create Yarn Project menu item to create new projects, rather than clicking New Resource > YarnProject
    * Creating via New Resource > YarnProject will create the .tres file inside your project folder instead of being hidden in a .godot folder. If you do create a YarnProject `.tres` file this way, after you interact with the YarnProject, a `.yarnproject` file should be created automatically alongside it. At that point, it's safe to delete the .tres file that you created, and carry on using the `.yarnproject` file instead.
* The inspector for YarnProjects has been redesigned to work with the `.yarnproject` integration. Changing fields like your source script patterns, localization CSV locations, or base language code will write to the `.yarnproject` file, triggering recompilation of your yarn scripts.
    * Godot automatically inserts a warning that the `.yarnproject` is read only. It doesn't seem this message can be removed, so please ignore it in this case and interact with the inspector as normal.
* You now have much more control over which .yarn scripts are included in a given project. Rather than being limited to .yarn scripts that are in the same a descendant directory as your YarnProject, you can now enter any number of glob patterns or file paths, both of which are relative to the directory where the `.yarnproject` file is saved. The inspector will show you a preview of which .yarn scripts would be included.
* The default set of source patterns is identical to the former behavior of this plugin.
* ⚠️ If you were using this plugin before this pull request, it's better to first back up your project, then note down any settings you overrode in your  `.tres` formatted YarnProject file, such as localization CSV file locations. Then, delete your .tres YarnProject files and work with the `.yarnproject` from then on.
* The built-in Line View now can now identify markup based pauses and insert pauses into the typewriter effect.
    * To use this you can use the pause markup inside your lines:
      ` Alice: wow this line now has a halt [pause=500 /] inside of it`
    *    This line will stop the typewriter for 500ms after the halt is shown. After the 500ms delay, the rest of the line will appear.
* onPauseStarted and onPauseEnded signals have been introduced on LineView.cs related to this effect
* Effects.Typewriter now is a wrapper into the PausableTypewriter effect
    * If you don't use pauses, nothing will change
* Added MarkupPalette custom Resource and support for the palette inside of LineView and OptionsListView and associated OptionView.
  *  This is useful both as a standalone way to easily annotate your dialogue, but also as an example of the markup system.
  *   A custom inspector is provided with the plugin to simplify choosing colors for your markup tags.
* An alternate dialogue UI with a rounded appearance is provided in `addons/YarnSpinner-Godot/Scenes/RoundedDialogueSystem.tscn`.
  * A sample scene has been added to demonstrate this alternate look:
* Additional menu items were added under Tools > YarnSpinner:
    * Create Yarn Script
    * Create Yarn Project
    * Create Markup Palette
* onCharacterTyped is now a Signal on LineView.cs. The signal will be emitted as each character is revealed via the typewriter effect.
* YarnProjects can now provide a list of line IDs within a node, using `GetLineIDsForNodes`.
    *   This is intended to be used to precache multiple nodes worth of assets, but might also be useful for debugging during development.
* Add `DialogueRunner.SaveStateToPersistentStorage` and `DialogueRunner.LoadStateFromPersistentStorage` to provide a simple method to save & load yarn variables to and from JSON in [the `user://` directory](https://docs.godotengine.org/en/stable/tutorials/io/data_paths.html ).
* add Export Strings and Metadata as CSV button on Yarn Projects
* Add the ability to show character name of the last line of dialogue on a separate RichTextLabel in OptionsListView . An example is in the rounded dialogue scene sample.

### Changed

* The plugin will now reimport any .yarn scripts that are modified when clicking "Add Line Tags to Scripts" automatically without needing to defocus and refocus the Godot editor.
* If TextLineProvider cannot find a translation for a line in a language other than the base language, it will now fall back to the base language's text for that line.
* Setting a project on the dialogue runner will now also load the initial variables from this project.
* Additionally, YarnSpinner and YarnSpinner.Compiler have been updated to version 2.4.0, bringing the following changes:
  https://github.com/YarnSpinnerTool/YarnSpinner/releases/tag/v2.4.0
* ⚠️  The setting for yarn project paths in your project.godot file is no longer used. You can remove the [YarnSpinnerGodot] section from your project.godot file.
* ⚠️ Simplified some [Exported] variables in DialogueRunner, LineView and OptionsListView. You might have to re-select fields such as CharacterNameText, LastLineText if you used these example scripts on a custom UI scene. The provided example dialogue scenes have been updated already with this change.
* ⚠️ Rename DispatchCommandToGameObject to DispatchCommandToNode, for consistency with Godot naming. Its likely you weren't calling this method directly, but if you were you will have to adjust the method name to the new one.
* As part of updating YarnSpinner.dll and YarnSpinner.Compiler.dll to 2.4.0, we found a way to eliminate most of, the checked-in .dll files such as System.Numerics, CompilerServices.Unsafe, and others. Now only YarnSpinner.dll and YarnSpinner.Compiler.dll, and two System.Text DLLs are checked directly into the project. The other dependencies are delivered as NuGet dependencies in `YarnSpinner-Godot.props`. YarnSpinner.Compiler relies on System.Text.Json @ 7.0.2, so to avoid a warning when targeting net6.0, we're still delivering System.Text.Json and System.Text.Encodings.Web as dll files. This should reduce DLL version conflicts such as in #20 - @CantyCanadian hopefully this completely clears up the issue you were seeing!
* If you are updating an existing installation of this plugin, delete your `addons/YarnSpinner-Godot` directory before upgrading since some files have been removed in this update.

## [0.1.5] 2023-10-06

### Added 

- Added onDialogueStart signal to DialogueRunner

### Changed

- Removed some DLL dependencies which could cause problems with ambiguous references (#20)
- Changed onDialogueComplete, onNodeStart, onNodeComplete to be Godot signals rather than C# events - should be backwards compatible (#19)
 
## [0.1.4] 2023-10-06

### Changed

- Calling `Stop` on the Dialogue Runner will now also dismiss the LineView, OptionListView, and VoiceOverView. (#17)
- Updated YarnSpinner DLL files