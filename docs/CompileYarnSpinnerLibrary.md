# Compiling the YarnSpinner library DLLs 

A compiled DLL of YarnSpinner.dll and YarnSpinner-Compiler.dll is provided with this project. 
If you want to update the version of these DLLs from source: 

* Clone https://github.com/YarnSpinnerTool/YarnSpinner 
* Ensure you have a current version of the DotNet SDK 
* Optionally, edit YarnSpinner/AssemblyInfo.cs and YarnSpinner.Compiler/AssemblyInfo.cs to customize the version number that will be marked on the build.
* Edit the `TargetFramework` tag in the `.csporj` files for YarnSpinner and YarnSpinner.Compiler to `  <TargetFramework>net6.0</TargetFramework>` for compatibility with this repo.
* `cd` in your terminal to `YarnSpinner.Compiler` and issue the command `dotnet build --configuration Release`
* Copy `YarnSpinner.dll` and `YarnSpinner.Compiler.dll` `from YarnSpinner.Compiler/bin/Release/net6.0`