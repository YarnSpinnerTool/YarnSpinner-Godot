# Yarn Spinner for Godot

> **Note:**
> Yarn Spinner for Godot is a work-in-progress project. We don't currently offer any official support for it. We encourage you to file [issues](https://github.com/YarnSpinnerTool/YarnSpinner-Godot/issues/new) if you have them, and to join the official [Yarn Spinner Discord](https://discord.gg/yarnspinner) to discuss the project!

Yarn Spinner for Godot is a beta port of [YarnSpinner-Unity](https://github.com/YarnSpinnerTool/YarnSpinner-Unity) integration to the Godot Engine v4 (requires C# support).

Here is a video of the various samples available as of version 0.2.0:

https://github.com/YarnSpinnerTool/YarnSpinner-Godot/assets/9920963/ec50efb5-0d1e-4353-9c08-71e27ff66038

To try these samples yourself, open this repository as a Godot project. Feel free to use the code in the samples as the basis for your own game features  based on YarnSpinner-Godot. 

## Documentation

There is a [guide here](https://docs.yarnspinner.dev/beginners-guide/making-a-game/yarn-spinner-for-godot) on the basic installation and usage of the plugin. 

See the [docs directory](./docs/Home.md) for more documentation. 

## Features 

* Create a `.yarnproject` with the Project > Tools > YarnSpinner > Create Yarn Project menu item. 
* Manage your Yarn Project with a custom inspector which provides buttons similar to the YarnSpinner-Unity inspector
* Yarn scripts will re-import on change, triggering a compilation of all yarn scripts in the associated project
* Storing a compiled yarn program, a list of errors, line metadata, and string tables.
* Dialogue runners, commands, and functions
* Example line view and option view scripts provided.
* Generate CSV files for localizing your dialogue. The CSV files are not in the Godot format, but they have more context fields than Godot CSVs, and YarnSpinner handles parsing and generating the CSVs, and converting them into Godot `.translation` files.
* MarkupPalette custom resources which allow you to simply color parts of your dialogue


### Thanks

Thanks to the YarnSpinner team for answering questions as this plugin was developed, to Taedan for providing an initial example C# import plugin, and to KXI and fmoo for giving feedback.
