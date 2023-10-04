## Installation

Copy the `addons/` directory to your project. This plugin requires some .dll dependencies which are delivered in the `addons/YarnSpinner-Godot/Runtime/DLLs` directory. In order to compile your project with YarnSpinner-Godot, add the following line to your Godot Mono project's `.csproj` file somewhere inside the `<Project>` tag (but not inside an ItemGroup or PropertyGroup) 

`   <Import Project="addons\YarnSpinner-Godot\YarnSpinner-Godot.props" />`

This will add references to all of YarnSpinner-Godot's dependencies to your C# project. 

Build your Godot project's C# solution, and then enable the plugin in Project > Project Settings > Plugins, checking Enabled next to YarnSpinner-Godot.

Note: In Godot, if the C# code for a plugin is not built when you open the Godot editor, the plugin in question will be automatically disabled, and you must re-enable it manually after building. 