# YarnDonut 🧶🍩
Beta port of [YarnSpinner-Unity](https://github.com/YarnSpinnerTool/YarnSpinner-Unity) integration to the Godot Mono engine v3.5

## Documentation

See the [wiki](https://github.com/dogboydog/YarnDonut/wiki) for documentation. 

## Roadmap 

Working:
* Create a Yarn Project, Yarn Script, or Yarn Localization through the Tools > YarnSpinner menu
* Manage your Yarn Project with a custom inspector which provides buttons similar to the YarnSpinner-Unity inspector
* Yarn scripts will re-import on change, triggering a compilation of all yarn scripts in the associated project
* Storing a compiled yarn program, a list of errors, line metadata, and string tables.
* Dialogue runners, commands, and functions
* Example line view and option view 
* Generate CSV files for localizing your dialogue. The CSV files are not in the Godot format, but they have more context fields than Godot CSVS, and YarnDonut handles parsing and generating the CSVs, and converting them into Godot `.translation` files.

TODO:
* Bug fixes / resilience (bug reports welcome)
* Clean up code comments

### Thanks

Thanks to the YarnSpinner team for answering questions as this plugin was developed, to Taedan for providing an initial example C# import plugin, and to KXI and fmoo for giving feedback.
