# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [0.3.0-beta 6] 2025-05-10

- This release aligns the terminology used in the plugin with the v3 Beta of the original Unity plugin, renaming
  dialogue "views" to dialogue "presenters", to make it clear that presenters can do more than just display dialogue
  lines and options visually.
- Renamed `AsyncDialogueViewBase` to be `DialoguePresenterBase`
- Renamed `AsyncLineView` to be `LinePresenter`
- Renamed `AsyncOptionItem` to be `OptionItem`
- Renamed `AsyncOptionsView` to be `OptionsPresenter`
- Renamed `viewControl` property in all `*Presenter` classes to `presenterControl`

## [0.3.0-beta 5] 2025-05-04

* Upgrade the addon and samples to Godot 4.4.1. The addon now delivers .uid files as part of the 4.4 upgrade.
* Remove onPauseStarted and onPauseEnded signals from AsyncLineView (they were not functioning anyway in prior 0.3
  releases)

## [0.3.0-beta 4] 2025-02-19

* Add a way to set Saliency Strategy from godot without having to write C# with @misha-cilantro (#85)
* Fix #82 - on_dialogue_complete_async problems with GDScript Integration
* Fix #81 - Restore GlobalClass attribute to DialogueRunner and InMemoryVariableStorage

## [0.3.0-beta 3] 2025-01-23

* Fixed an issue where the AsyncLineView would show at the beginning of dialogue, even if the dialogue begins with an
  option.
* Fixed an issue where the System.Threading.Tasks implementation of YarnTask.WaitUntil did not return early when the
  CancellationToken was cancelled
* Make the fade effect optional via inspector fields `useFadeEffect`, `fadeUpDuration, and `fadeDownDuration` in
  AsyncOptionsView.cs

## [0.3.0-beta 2]   2025-01-06

* Eliminate the need to check in DLL files with the plugin in favor of using Nuget, now that the YarnSpinner DLL files
  function properly with this plugin. You can delete any .dll files under addons/YarnSpinner-Godot that you might have
  from a previous release
* Restructure the AsyncOptionItem script (replaces OptionView from v0.2). The node that the AsyncOptionItem script is
  attached to can now be a Control rather than needing to be a button. The button component that will handle option
  selection can be a different node if desired. Assign the button in the AsyncOptionItem's inspector.

## [0.3.0-beta 1]  2024-12-14

* Updated YarnSpinner DLLs to support version 3 of the Yarn Language, which supports many new features, similar to the
  feature set described for the Unity plugin here:
    * https://www.yarnspinner.dev/blog/yarn-spinner-3-beta-1
    * The samples have been updated to demonstrate these features, including enums, node groups, line groups, smart
      variables, shadow lines, generated variable storage code, `<<detour>>` and `<<once>>`.
* `[YarnCommand`] and `[YarnFunction]` have been updated to generate a class called
  `YarnSpinnerGodot.Generated.ActionRegistration`. This class will register all of your commands and functions without
  relying on reflection, which was the cause of some performance hiccups when starting a scene with a DialogueRunner in
  v0.2.* of the plugin
* To enable new features for an existing .yarnproject, edit the .yarnproject file to change the projectFileVersion to 3.
* If you keep your .yarnproject at projectFileVersion 2, you will have to re-compile the scripts in your yarn project to
  work with this version of the plugin.
* Update views to be async Task based, like the Unity plugin version 3.
* The existing `DialogueViewBase` interface is deprecated in favor of `AsyncDialogueViewBased`. Updated example views
  are provided.
* GDScript nodes that have methods that are the `snake_case` version of methods on `AsyncDialogueViewBase` can be added
  directly in `YarnProject.dialogueViews`
  without requiring GDScriptViewAdapter. Supported methods: `on_dialogue_start_async() -> void`,
  `on_dialogue_complete_async() -> void`, `run_line_async(line: Dictionary) -> void`,
  `run_options_async(options: Array, on_option_selected: Callable) -> void`. You can still use `await` statements in
  your GDScript view methods. `AddCommandHandlerCallable` on `DialogueRunner` has been re-tested to ensure that commands
  can still be registered from GDScript. Also, the GDScriptIntegration sample has been updated with these changes.
* ⚠ Breaking change: LineProviderBehaviour has changed to require different methods and fields. If you have a custom
  line provider, please see the updated TextLineProvider as an * example of how to implement the updated class. Also
  notice that you must now set the yarn project on `TextLineProvider` instances, either via script or in the inspector.
* ⚠ Breaking change: Use DialogueRunner.VariableStorage, not DialogueRunner.variableStorage, nor
  DialogueRunner.SetDialogueViews (removed)  to get/set the variable storage associated with a DialogueRunner.
  Previously there were two properties with different case that were both publicly accessible.
* ⚠ Breaking change: MarkupPalette renamed ColourMarkers to FormatMarkers, supporting new functionality like bold,
  underline, italics, as a way to demonstrate the updated formatting
* ⚠ Breaking change: the field DialogueRunner.verboseLogging has been removed.
* New functionality on YarnProject - optionally generate a C# variable storage class which has getters and setters for
  each variable declared in your yarn scripts. You can control the class that the generated file inherits from, the
  namespace it will be in, and the name of the class and file.

## [0.2.16] 2025-1-24

* Fix null reference in AddCommandHandlerCallable when callable does not return an object (#80) by @meybax
* Fix uncaught exceptions in DialogueRunner when markup in a line was malformed.

## [0.2.15] 2025-1-23

* Fixed an issue where an exception could be raised if OptionsListView has lastLineText set, and dialogue begins with an
  option.

## [0.2.14] 2024-11-02

* GDScript: Add GDScriptViewAdapter, a C# Script which allows you to write custom dialogue views in GDScript. See
  GDScriptViewAdapter.cs for more details.
* GDScript: Add new method AddCommandHandlerCallable to DialogueRunner, allowing commands to be registered from
  GDScript. GDScript command handlers that use asynchronous `await` functionality are also supported as blocking
  YarnSpinner commands, similar to using `async Task` commands in C#.
* Samples: A new sample has been added called GDScriptIntegration, which demonstrates making a simple custom view in
  GDScript, and using cross-language scripting to access methods on C# components such as the DialogueRunner and
  InMemoryVariableStorage
* Add new method to InMemoryVariableStorage: `public Variant GetVariantValue(string variableName)`. An example of a
  method that allows you to retrieve YarnSpinner story variables from GDScript.
* Thanks to @dbaz for taking my old code from the gdscript_integration branch and making sure it compiles with the
  current version of the plugin

## [0.2.13] 2024-09-15

* General code cleanup by @valkyrienyanko. Make use of some C# language features such as target-typed `new()` to
  simplify code.
* Potential breaking change: DispatchCommandToNode on DialogueRunner has been marked static. Users generally do not need
  to call this method directly, so it shouldn't affect most or possibly any projects.

## [0.2.12] 2024-09-15

* Use file-scoped namespaces in all plugin scripts to reduce indentation, by @valkyrienyanko

## [0.2.11] 2024-09-13

* Fix an issue where the OptionsListView did not fade in properly and instead suddenly appeared at the end of the fade
  time. (Fix #62)
* `Effects.Fade` now uses a Godot Tween.
* Grab focus of the first visible option in OptionsListView.cs, rather than the first option which may be hidden due to
  being unavailable.

## [0.2.10] 2024-07-11

* Update System.Text.Json to 8.0.4 based on CVE-2024-30105 https://github.com/advisories/GHSA-hh2w-p6rv-4g7w
* Fix references to `RoundedViewStylebox.tres` that were in the wrong case in resource files, which could cause an issue
  on case-sensitive platforms.

## [0.2.9] 2024-06-30

* Set LineView's MouseFilter to 'ignore' to avoid interfering with clicks.
* Fix an issue where 'use fade effect' would cause the ConvertHTMLToBBCode feature to stop working while the text was
  fading out.
* Set the LineView to Visible=False when marking its alpha as 0.

## [0.2.8] 2024-05-24

* Fix Yarn commands not supporting methods with optional arguments (by @spirifoxy)
* Update Visual Novel sample to make use of this fix

## [0.2.7] 2024-05-11

* Add an explicit dependency on System.Text.Json v8.0.1 to fix compiler warnings and incompatibility with DotNet SDK
  versions below 8. The symptom in 0.2.6 was failing to create a .yarnproject file from the editor.

## [0.2.6] 2024-05-08

* Fix #53 where the first OptionView in an OptionsListView could not be interacted with if fadeTime was zero
* Update YarnSpinner core DLL files
  to [version 2.4.2](https://github.com/YarnSpinnerTool/YarnSpinner/releases/tag/v2.4.2)
    * Updated dependency versions:
        * Google.Protobuf: Moved from 3.15.0 to 3.25.2.
        * Microsoft.Extensions.FileSystemGlobbing: Moved from 7.0.0 to 8.0.0
    * [Standard library functions (e.g. random, round_places, dice)](https://docs.yarnspinner.dev/getting-started/writing-in-yarn/functions#built-in-functions)
      have been removed from Yarn Spinner for Godot, since they have been added directly to the core Yarn Spinner
      library.
* Fix #55 where Yarn projects with no scripts added / compiled yet threw exceptions when locale related or 'export
  strings' were pressed

## [0.2.5] 2024-04-28

* fix #50 - errors calling Stop if the runner was running a command like wait
* fix #44 - show variable declarations in the yarn project inspector
* Add SQL Variable storage Sample (not delivered in the addons/ directory, open this repository as a Godot project to
  try this sample)

## [0.2.4] 2024-04-17

* fix #46 - The Create Yarn Project menu item was not working in 4.1.2
* fix #45 - Add `#nullable disable` to the plugin's C# source files for compatibility with projects with nullable
  enabled
* fix #35 - Don't have the example OptionView expand vertically

## [0.2.3] 2024-04-03

* Make LineView compatible with any subclass of BaseButton used as your continueButton. #40 by @fmoo
* Add GlobalClass attribute to InMemoryVariableStorage, TexTLineProvider, LineView, OptionsListView. #41 by @fmoo
* Improve error logging in LineView #42 by @fmoo

## [0.2.2] 2024-03-04

* Fixed an error with Tools menu items for YarnSpinner not working after rebuilding C# code.

## [0.2.1] 2024-02-13

* Fixed a crash that could result from an attempted workaround of the 'assembly reload failed' error

## [0.2.0] 2024-02-01

### Added

* This plugin now integrates with `.yarnproject` files, which are JSON formatted files that define the settings used to
  work with your `.yarn` scripts. The C# YarnProject custom resource for Godot is generated and updated automatically by
  the plugin. You can use the `.yarnproject` file in the editor for drag-n-drop inspector functionality for
  YarnProjects. For example, you can drag your .yarnproject file into the Yarn Project slot on your DialogueRunner
  instances.
* The .tres file containing the compiled program and other generated data relating to the .yarnproject is now stored in
  the `.godot` directory rather than in the main directories of your project. As such, it will no longer be version
  controlled in most cases as `.godot` is typically ignored. However just opening your Godot project with this plugin
  enabled should re-generate the necessary files if you need to delete your `.godot` folder for any reason or re-clone
  your project.
* Some other software such as
  the [VS Code Extension](https://docs.yarnspinner.dev/getting-started/editing-with-vs-code/installing-the-extension)
  can also use these files. This integration was added partially to support cross-engine tooling that relies on
  .yarnproject files, as well as bringing the workflow of using the Godot plugin closer to the original Unity plugin.
* Please use the Tools > YarnSpinner >Create Yarn Project menu item to create new projects, rather than clicking New
  Resource > YarnProject
    * Creating via New Resource > YarnProject will create the .tres file inside your project folder instead of being
      hidden in a .godot folder. If you do create a YarnProject `.tres` file this way, after you interact with the
      YarnProject, a `.yarnproject` file should be created automatically alongside it. At that point, it's safe to
      delete the .tres file that you created, and carry on using the `.yarnproject` file instead.
* The inspector for YarnProjects has been redesigned to work with the `.yarnproject` integration. Changing fields like
  your source script patterns, localization CSV locations, or base language code will write to the `.yarnproject` file,
  triggering recompilation of your yarn scripts.
    * Godot automatically inserts a warning that the `.yarnproject` is read only. It doesn't seem this message can be
      removed, so please ignore it in this case and interact with the inspector as normal.
* You now have much more control over which .yarn scripts are included in a given project. Rather than being limited to
  .yarn scripts that are in the same a descendant directory as your YarnProject, you can now enter any number of glob
  patterns or file paths, both of which are relative to the directory where the `.yarnproject` file is saved. The
  inspector will show you a preview of which .yarn scripts would be included.
* The default set of source patterns is identical to the former behavior of this plugin.
* ⚠️ If you were using this plugin before this pull request, it's better to first back up your project, then note down
  any settings you overrode in your  `.tres` formatted YarnProject file, such as localization CSV file locations. Then,
  delete your .tres YarnProject files and work with the `.yarnproject` from then on.
* The built-in Line View now can now identify markup based pauses and insert pauses into the typewriter effect.
    * To use this you can use the pause markup inside your lines:
      ` Alice: wow this line now has a halt [pause=500 /] inside of it`
    * This line will stop the typewriter for 500ms after the halt is shown. After the 500ms delay, the rest of the line
      will appear.
* onPauseStarted and onPauseEnded signals have been introduced on LineView.cs related to this effect
* Effects.Typewriter now is a wrapper into the PausableTypewriter effect
    * If you don't use pauses, nothing will change
* Added MarkupPalette custom Resource and support for the palette inside of LineView and OptionsListView and associated
  OptionView.
    * This is useful both as a standalone way to easily annotate your dialogue, but also as an example of the markup
      system.
    * A custom inspector is provided with the plugin to simplify choosing colors for your markup tags.
* An alternate dialogue UI with a rounded appearance is provided in
  `addons/YarnSpinner-Godot/Scenes/RoundedDialogueSystem.tscn`.
    * A sample scene has been added to demonstrate this alternate look:
* Additional menu items were added under Tools > YarnSpinner:
    * Create Yarn Script
    * Create Yarn Project
    * Create Markup Palette
* onCharacterTyped is now a Signal on LineView.cs. The signal will be emitted as each character is revealed via the
  typewriter effect.
* YarnProjects can now provide a list of line IDs within a node, using `GetLineIDsForNodes`.
    * This is intended to be used to precache multiple nodes worth of assets, but might also be useful for debugging
      during development.
* Add `DialogueRunner.SaveStateToPersistentStorage` and `DialogueRunner.LoadStateFromPersistentStorage` to provide a
  simple method to save & load yarn variables to and from JSON in [the
  `user://` directory](https://docs.godotengine.org/en/stable/tutorials/io/data_paths.html ).
* add Export Strings and Metadata as CSV button on Yarn Projects
* Add the ability to show character name of the last line of dialogue on a separate RichTextLabel in OptionsListView .
  An example is in the rounded dialogue scene sample.

### Changed

* The plugin will now reimport any .yarn scripts that are modified when clicking "Add Line Tags to Scripts"
  automatically without needing to defocus and refocus the Godot editor.
* If TextLineProvider cannot find a translation for a line in a language other than the base language, it will now fall
  back to the base language's text for that line.
* Setting a project on the dialogue runner will now also load the initial variables from this project.
* Additionally, YarnSpinner and YarnSpinner.Compiler have been updated to version 2.4.0, bringing the following changes:
  https://github.com/YarnSpinnerTool/YarnSpinner/releases/tag/v2.4.0
* ⚠️ The setting for yarn project paths in your project.godot file is no longer used. You can remove
  the [YarnSpinnerGodot] section from your project.godot file.
* ⚠️ Simplified some [Exported] variables in DialogueRunner, LineView and OptionsListView. You might have to re-select
  fields such as CharacterNameText, LastLineText if you used these example scripts on a custom UI scene. The provided
  example dialogue scenes have been updated already with this change.
* ⚠️ Rename DispatchCommandToGameObject to DispatchCommandToNode, for consistency with Godot naming. Its likely you
  weren't calling this method directly, but if you were you will have to adjust the method name to the new one.
* As part of updating YarnSpinner.dll and YarnSpinner.Compiler.dll to 2.4.0, we found a way to eliminate most of, the
  checked-in .dll files such as System.Numerics, CompilerServices.Unsafe, and others. Now only YarnSpinner.dll and
  YarnSpinner.Compiler.dll, and two System.Text DLLs are checked directly into the project. The other dependencies are
  delivered as NuGet dependencies in `YarnSpinner-Godot.props`. YarnSpinner.Compiler relies on System.Text.Json @ 7.0.2,
  so to avoid a warning when targeting net6.0, we're still delivering System.Text.Json and System.Text.Encodings.Web as
  dll files. This should reduce DLL version conflicts such as in #20 - @CantyCanadian hopefully this completely clears
  up the issue you were seeing!
* If you are updating an existing installation of this plugin, delete your `addons/YarnSpinner-Godot` directory before
  upgrading since some files have been removed in this update.

## [0.1.5] 2023-10-06

### Added

- Added onDialogueStart signal to DialogueRunner

### Changed

- Removed some DLL dependencies which could cause problems with ambiguous references (#20)
- Changed onDialogueComplete, onNodeStart, onNodeComplete to be Godot signals rather than C# events - should be
  backwards compatible (#19)

## [0.1.4] 2023-10-06

### Changed

- Calling `Stop` on the Dialogue Runner will now also dismiss the LineView, OptionListView, and VoiceOverView. (#17)
- Updated YarnSpinner DLL files
