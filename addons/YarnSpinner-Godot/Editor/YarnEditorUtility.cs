#nullable disable
#if TOOLS
using Godot;
using File = System.IO.File;
using Path = System.IO.Path;

namespace YarnSpinnerGodot;

/// <summary>
/// Contains utility methods for working with Yarn Spinner content in
/// the Godot editor.
/// </summary>
public static class YarnEditorUtility
{

    
    const string TemplateFilePath = "res://addons/YarnSpinner-Godot/Editor/YarnScriptTemplate.txt";

    /// <summary>
    /// Menu Item "Tools > YarnSpinner > Create Yarn Script"
    ///
    /// </summary>    
    public static void CreateYarnScript(string scriptPath)
    {
        GD.Print($"Creating new yarn script at {scriptPath}");
        CreateYarnScriptAssetFromTemplate(scriptPath);
    }

    /// <summary>
    /// Menu Item "Tools > YarnSpinner > Create Yarn Script"
    /// </summary>
    /// <param name="projectPath">res:// path of the YarnProject resource to create</param>
    public static void CreateYarnProject(string projectPath)
    {
        var jsonProject = new Yarn.Compiler.Project();
        var absPath = ProjectSettings.GlobalizePath(projectPath);
        jsonProject.SaveToFile(absPath);
    }
    /// <summary>
    /// Menu Item "Tools > YarnSpinner > Create Markup Palette"
    /// </summary>
    /// <param name="palettePath">res:// path to the markup palette to create</param>
    public static void CreateMarkupPalette(string palettePath)
    {
        var newPalette = new MarkupPalette();
        var absPath = ProjectSettings.GlobalizePath(palettePath);
        newPalette.ResourceName = Path.GetFileNameWithoutExtension(absPath);
        newPalette.ResourcePath = palettePath;
        var saveErr = ResourceSaver.Save(newPalette, palettePath);
        if (saveErr != Error.Ok)
        {
            GD.Print($"Failed to save markup palette to {palettePath}");
        }
    }
    
    /// <summary>
    /// Menu Item "Yarn Spinner/Create Yarn Script"
    /// 
    /// </summary>
    /// <param name="localizationPath"></param>
    public static void CreateYarnLocalization(string localizationPath)
    {
        var newLocalization =  new Localization();
        var absPath = ProjectSettings.GlobalizePath(localizationPath);
        newLocalization.ResourceName = Path.GetFileNameWithoutExtension(absPath);
        newLocalization.ResourcePath = localizationPath;
        ResourceSaver.Save( newLocalization , localizationPath);
        GD.Print($"Saved new yarn localization to {localizationPath}");
    }

    private static void CreateYarnScriptAssetFromTemplate(string pathName)
    {
        // Read the contents of the template file
        string templateContent;
        try
        {
            templateContent = File.ReadAllText(ProjectSettings.GlobalizePath(TemplateFilePath));
        }
        catch
        {
            GD.PrintErr("Failed to find the Yarn script template file. Creating an empty file instead.");
            // the minimal valid Yarn script - no headers, no body
            templateContent = "---\n===\n";
        }

        // Figure out the 'file name' that the user entered
        // The script name is the name of the file, sans extension.
        string scriptName = Path.GetFileNameWithoutExtension(pathName);

        // Replace any spaces with underscores - these aren't allowed
        // in node names
        scriptName = scriptName.Replace(" ", "_");

        // Replace the placeholder with the script name
        templateContent = templateContent.Replace("#SCRIPTNAME#", scriptName);

        string lineEndings = "\n";

        // Replace every line ending in the template (this way we don't
        // need to keep track of which line ending the asset was last
        // saved in)
        templateContent = System.Text.RegularExpressions.Regex.Replace(templateContent, @"\r\n?|\n", lineEndings);

        // Write it all out to disk as UTF-8
        var fullPath = Path.GetFullPath(ProjectSettings.GlobalizePath(pathName));
        File.WriteAllText(fullPath, templateContent, System.Text.Encoding.UTF8);
        GD.Print($"Wrote new file {pathName}");
        YarnSpinnerPlugin.editorInterface.GetResourceFilesystem().ScanSources();
    }
    
}
#endif