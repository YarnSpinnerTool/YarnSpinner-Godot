#if TOOLS
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
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
        const string TemplateFilePath = "res://TODO";

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
        /// Begins the interactive process of creating a new Yarn file in
        /// the Editor. Menu Item "Yarn Spinner/Create Yarn Script"
        /// </summary>    
        public void CreateYarnAsset()
        {
            GD.Print("TODO would create a script here");
            // This method call is undocumented, but public. It's defined
            // in ProjectWindowUtil, and used by other parts of the editor
            // to create other kinds of assets (scripts, textures, etc).
            // ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
            //     0,
            //     ScriptableObject.CreateInstance<DoCreateYarnScriptAsset>(),
            //     "NewYarnScript.yarn",
            //     GetYarnDocumentIconTexture(),
            //     GetTemplateYarnScriptPath());
        }

        // [MenuItem("Yarn Spinner/Create Yarn Project", false, 101)]
        public void CreateYarnProject()
        {
            // This method call is undocumented, but public. It's defined
            // in ProjectWindowUtil, and used by other parts of the editor
            // to create other kinds of assets (scripts, textures, etc).
            // ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
            //     0,
            //     ScriptableObject.CreateInstance<DoCreateYarnScriptAsset>(),
            //     "NewProject.yarnproject",
            //     GetYarnProjectIconTexture(),
            //     GetTemplateYarnScriptPath());
        }

        /// <summary>
        /// Creates a new Yarn project at the given path, using the default
        /// template.
        /// </summary>
        /// <param name="path">The path at which to create the
        /// script.</param>
        public Object CreateYarnProject(string path)
        {
            return CreateYarnScriptAssetFromTemplate(path, TemplateFilePath);
        }

        /// <summary>
        /// Creates a new Yarn script at the given path, using the default
        /// template.
        /// </summary>
        /// <param name="path">The path at which to create the
        /// script.</param>
        public Object CreateYarnAsset(string path)
        {
            return CreateYarnScriptAssetFromTemplate(path, TemplateFilePath);
        }

        private Resource CreateYarnScriptAssetFromTemplate(string pathName, string resourceFile)
        {
            // Read the contents of the template file
            string templateContent;
            try
            {
                templateContent = File.ReadAllText(resourceFile);
            }
            catch
            {
                GD.PrintErr("Failed to find the Yarn script template file. Creating an empty file instead.");
                // the minimal valid Yarn script - no headers, no body
                templateContent = "---\n===\n";
            }

            // Figure out the 'file name' that the user entered
            string scriptName;
            if (Path.GetExtension(pathName).Equals(".yarnproject", System.StringComparison.InvariantCultureIgnoreCase))
            {
                // This is a .yarnproject file; the script "name" is always
                // "Project".
                scriptName = "Project";
            }
            else
            {
                // The script name is the name of the file, sans extension.
                scriptName = Path.GetFileNameWithoutExtension(pathName);
            }

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
            var fullPath = Path.GetFullPath(pathName);
            File.WriteAllText(fullPath, templateContent, System.Text.Encoding.UTF8);

            // We don't hugely care about the details of the object anyway
            // (we just wanted to ensure that it's imported as at least an
            // asset), so we'll return it as a Resource here.
            return ResourceLoader.Load<Resource>(pathName);
        }
        
        /// <summary>
        /// Get all assets of a given type.
        /// </summary>
        /// <typeparam name="T">AssetImporter type to search for. Should be convertible from AssetImporter.</typeparam>
        /// <param name="filterQuery">Asset query (see <see cref="AssetDatabase.FindAssets(string)"/> documentation for formatting).</param>
        /// <param name="converter">Custom type caster.</param>
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