#if TOOLS
using System.Collections.Generic;
using Godot;
using File = System.IO.File;
using Path = System.IO.Path;

namespace YarnSpinnerGodot.Editor
{
    /// <summary>
    /// Contains utility methods for working with Yarn Spinner content in
    /// the Godot editor.
    /// </summary>
    public static class YarnEditorUtility
    {

        
        const string TemplateFilePath = "res://addons/YarnSpinner-Godot/Editor/YarnScriptTemplate.txt";

        /// <summary>
        /// Menu Item "Yarn Spinner/Create Yarn Scr ipt"
        ///
        /// </summary>    
        public static void CreateYarnScript(string scriptPath)
        {
            GD.Print($"Creating new yarn script at {scriptPath}");
            CreateYarnScriptAssetFromTemplate(scriptPath);
        }

        /// <summary>
        /// Menu Item "Yarn Spinner/Create Yarn Script"
        /// 
        /// </summary>
        /// <param name="projectPath"></param>
        public static void CreateYarnProject(string projectPath)
        {
            var newYarnProject = new YarnProject();
            var absPath = ProjectSettings.GlobalizePath(projectPath);
            newYarnProject.ResourceName = Path.GetFileNameWithoutExtension(absPath);
            newYarnProject.ResourcePath = projectPath;
            var saveErr = ResourceSaver.Save( newYarnProject, projectPath);
            if (saveErr != Error.Ok)
            {
                GD.Print($"Failed to save yarn project to {projectPath}");
            }
            else
            {
                GD.Print($"Saved new yarn project to {projectPath}");
                YarnProjectEditorUtility.AddProjectToList(newYarnProject);
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
        }

        /// <summary>
        /// Get all assets of a given type.
        /// </summary>
        /// <typeparam name="T">AssetImporter type to search for. Should be convertible from AssetImporter.</typeparam>
        /// <param name="filterQuery">Asset query (see <see cref="AssetDatabase.FindAssets(string)"/> documentation for formatting).</param>
        /// <returns>Enumerable of all assets of a given type.</returns>
        public static IEnumerable<T> GetAllAssetsOf<T>(string filterQuery) where T : class
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