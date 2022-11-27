#if TOOLS
using System.Collections.Generic;
using Godot;
using File = System.IO.File;
using Object = Godot.Object;
using Path = System.IO.Path;

namespace Yarn.GodotIntegration.Editor
{
    /// <summary>
    /// Contains utility methods for working with Yarn Spinner content in
    /// the Unity Editor.
    /// Note: this is no longer a static class unlike the Unity version because that causes
    /// difficulties calling the methods from GDScript
    /// </summary>
    public class YarnEditorUtility : Object
    {

        const string DocumentIconTexturePath = "res://addons/YarnSpinnerGodot/Editor/Icons/Asset Icons/YarnScript Icon.png";
        const string ProjectIconTexturePath = "res://addons/YarnSpinnerGodot/Editor/Icons/Asset Icons/YarnProject Icon.png";
        const string TemplateFilePath = "res://addons/YarnSpinnerGodot/Editor/YarnScriptTemplate.txt";

        /// <summary>
        /// Returns a <see cref="Texture2D"/> that can be used to represent
        /// Yarn files.
        /// </summary>
        /// <returns>A texture to use in the Unity editor for Yarn
        /// files.</returns>
        public Texture GetYarnDocumentIconTexture()
        {
            return ResourceLoader.Load<Texture>(DocumentIconTexturePath);
        }

        /// <summary>
        /// Returns a <see cref="Texture2D"/> that can be used to represent
        /// Yarn project files.
        /// </summary>
        /// <returns>A texture to use in the Unity editor for Yarn project
        /// files.</returns>
        public Texture GetYarnProjectIconTexture()
        {
            return ResourceLoader.Load<Texture>(ProjectIconTexturePath);
        }

        /// <summary>
        /// Menu Item "Yarn Spinner/Create Yarn Script"
        ///
        /// Called from the plugin.gd script
        /// </summary>    
        public void CreateYarnScript(string scriptPath)
        {
            GD.Print($"Creating new yarn script at {scriptPath}");
            CreateYarnScriptAssetFromTemplate(scriptPath);
        }

        /// <summary>
        /// Menu Item "Yarn Spinner/Create Yarn Script"
        /// 
        /// Called from the plugin.gd script
        /// </summary>
        /// <param name="projectPath"></param>
        public void CreateYarnProject(string projectPath)
        {
            // If I don't load the resource script this way, the type of the serialized resource file is incorrect,
            // and none of the script properties are saved. Simply calling the new CompiledYarnFile() constructor doesn't work.
            var projectScript = (CSharpScript)ResourceLoader.Load("res://addons/YarnSpinnerGodot/Runtime/YarnProject.cs");
            var newYarnProject = (YarnProject)projectScript.New();
            var absPath = ProjectSettings.GlobalizePath(projectPath);
            newYarnProject.ResourceName = Path.GetFileNameWithoutExtension(absPath);
            newYarnProject.ResourcePath = projectPath;
            ResourceSaver.Save(projectPath, newYarnProject);
            GD.Print($"Saved new yarn project to {projectPath}");
        }

        private void CreateYarnScriptAssetFromTemplate(string pathName)
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
        }

        /// <summary>
        /// Get all assets of a given type.
        /// </summary>
        /// <typeparam name="T">AssetImporter type to search for. Should be convertible from AssetImporter.</typeparam>
        /// <param name="filterQuery">Asset query (see <see cref="AssetDatabase.FindAssets(string)"/> documentation for formatting).</param>
        /// <returns>Enumerable of all assets of a given type.</returns>
        public IEnumerable<T> GetAllAssetsOf<T>(string filterQuery) where T : class
        {
            // TODO: store list of yarn files in plugin settings?
            // not seeing an easy way to find all resources in the project 
            // of a certain type in Godot.
            GD.PrintErr("TODO: Need a way to store/find list of yarn projects");
            return new T[]
            {
                null
            };
        }
    }
}
#endif