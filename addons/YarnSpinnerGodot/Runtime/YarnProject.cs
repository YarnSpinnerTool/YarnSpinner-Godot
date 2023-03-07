using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using Newtonsoft.Json;
using Yarn.Compiler;
using Array = System.Array;

#if TOOLS
using YarnSpinnerGodot.Editor;
#endif
namespace Yarn.GodotIntegration
{
    [Tool]
    public partial class YarnProject : Resource //, IYarnErrorSource
    {
        /// <summary>
        /// Indicates whether the last time this file was imported, the
        /// file contained lines that did not have a line tag (and
        /// therefore were assigned an automatically-generated, 'implicit'
        /// string tag.) 
        /// </summary>
        [Export] public bool LastImportHadImplicitStringIDs;

        /// <summary>
        /// Indicates whether the last time this file was imported, the
        /// file contained any string tags.
        /// </summary>
        [Export] public bool LastImportHadAnyStrings;
        /// <summary>
        /// Indicates whether the last time this file was imported, the
        /// file was able to be parsed without errors. 
        /// </summary>
        /// <remarks>
        /// This value only represents whether syntactic errors exist or
        /// not. Other errors may exist that prevent this script from being
        /// compiled into a full program.
        /// </remarks>
        [Export] public bool IsSuccessfullyParsed = false;
        public byte[] CompiledYarnProgram => Convert.FromBase64String(CompiledYarnProgramBase64);

        [Export] public string CompiledYarnProgramBase64;
        // TODO: filter scripts by parse errors
        public List<Resource> ScriptsWithParseErrors => new List<Resource>();

        //IList<string> IYarnErrorSource.CompileErrors => ParseErrorMessages;
        public bool Destroyed => false; // not sure when this is used yet

        [Export] public Array<string> SourceScripts = new Array<string>();
        //[Export] 
        [Export] public Localization baseLocalization;

        [Export]
        public Localization[] localizations = Array.Empty<Localization>();

        [Export]
        public LineMetadata lineMetadata = null;

        public LocalizationType localizationType;

        [Export]
        public FunctionInfo[] ListOfFunctions;

        [Export] public SerializedDeclaration[] SerializedDeclarations = Array.Empty<SerializedDeclaration>();

        [Export] [Language]
        public string defaultLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;

        public List<LanguageToSourceAsset> languagesToSourceAssets
        {
            get {
                var result = new List<LanguageToSourceAsset>();
                if (localizations != null)
                {
                    foreach (var localization in localizations)
                    {
                        var entry = new LanguageToSourceAsset();
                        entry.languageID = localization.LocaleCode;
                        entry.stringsFile = localization.stringsFile;
                        result.Add(entry);
                    }
                }
                return result;
            }
        }

        #if TOOLS
        /// <summary>
        /// Search all directories for .yarn files and save the list to the project
        /// </summary>
        /// <returns></returns>
        public void SearchForSourceScripts()
        {
            try
            {
                if (ResourcePath == "" || ResourcePath == null)
                {
                    GD.Print($"{nameof(YarnProject)}s must be saved to a file in your project to be used with this plugin.");
                    return;
                }

                var projectDir = ProjectSettings.GlobalizePath(ResourcePath);
                projectDir = System.IO.Directory.GetParent(projectDir).FullName;
                var allProjects = (Godot.Collections.Array)ProjectSettings.GetSetting(YarnProjectEditorUtility.YARN_PROJECT_PATHS_SETTING_KEY);
                var nestedYarnProjects = new List<string>();
                foreach (string project in allProjects)
                {
                    var absoluteOtherProjectDir = ProjectSettings.GlobalizePath(project);
                    absoluteOtherProjectDir = System.IO.Directory.GetParent(absoluteOtherProjectDir).FullName;
                    if (!project.Equals(ResourcePath) && absoluteOtherProjectDir.Contains(projectDir))
                    {
                        nestedYarnProjects.Add(absoluteOtherProjectDir);
                    }
                }
                SourceScripts = new Array<string>(FindSourceScriptsRecursive(nestedYarnProjects,
                    projectDir, new List<string>()).ToArray());
            }
            catch (Exception e)
            {
                GD.PushError($"Error searching for .yarn scripts in Yarn Project '{ResourcePath}': {e.Message}{e.StackTrace}");
            }
        }
        /// <summary>
        /// Find a list of all .yarn files that this YarnProject is responsible for compiling.
        /// </summary>
        /// <param name="nestedYarnProjectDirs">list of other yarn projects that are below this one. used to exclude .yarn files covered by deeper nested yarn projects from this project.</param>
        /// <param name="dirPath">the directory to search for files and child directories</param>
        /// <param name="scripts"></param>
        /// <returns></returns>
        private List<string> FindSourceScriptsRecursive(List<string> nestedYarnProjectDirs, string dirPath, List<string> scripts)
        {
            var files = System.IO.Directory.GetFiles(dirPath);
            foreach (var file in files)
            {
                if (file.ToUpperInvariant().EndsWith(".YARN"))
                {
                    var scriptResPath = ProjectSettings.LocalizePath(file.Replace("\\", "/"));
                    scripts.Add(scriptResPath);
                }
            }
            var subdirectories = System.IO.Directory.GetDirectories(dirPath);
            foreach (var subdirectory in subdirectories)
            {
                var coveredByNestedProject = false;
                foreach (var nested in nestedYarnProjectDirs)
                {
                    if (subdirectory.Contains(nested))
                    {
                        coveredByNestedProject = true;
                        break;
                    }
                }
                if (!coveredByNestedProject)
                {
                    // ignore directories that are covered by other, nested yarn projects
                    scripts = FindSourceScriptsRecursive(nestedYarnProjectDirs, subdirectory, scripts);
                }
            }
            return scripts;
        }
        #endif

        [Export]
        public YarnProjectError[] ProjectErrors = Array.Empty<YarnProjectError>();

        /// <summary>
        /// Gets a value indicating whether the source script has line
        /// tags.
        /// </summary>
        /// <param name="script">The source script to add. This script must
        /// have been imported by a <see cref="YarnImporter"/>.</param>
        /// <returns>
        /// <see langword="true"/> if the the script is fully tagged, <see
        /// langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when <paramref name="script"/> is <see
        /// langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Thrown when <paramref name="script"/> is not imported by a <see
        /// cref="YarnImporter"/>.
        /// </exception>
        private bool GetScriptHasLineTags(Resource script)
        {
            if (script == null)
            {
                // This might be a 'None' or 'Missing' asset, so return
                // false here.
                return false;
            }

            GD.Print("TODO: accurate check on which  scripts have line tags");
            return false;
        }

        /// <summary>
        /// The cached result of deserializing <see
        /// cref="CompiledYarnProgram"/>.
        /// </summary>
        private Program cachedProgram = null;

        /// <summary>
        /// The names of assemblies that <see cref="ActionManager"/> should look
        /// for commands and functions in when this project is loaded into a
        /// <see cref="DialogueRunner"/>.
        /// </summary>
        public List<string> searchAssembliesForActions = new List<string>();

        public Localization GetLocalization(string localeCode)
        {

            // If localeCode is null, we use the base localization.
            if (localeCode == null)
            {
                return baseLocalization;
            }

            foreach (var loc in localizations)
            {
                if (loc.LocaleCode == localeCode)
                {
                    return loc;
                }
            }

            // We didn't find a localization. Fall back to the Base
            // localization.
            return baseLocalization;
        }

        /// <summary>
        /// Gets the Yarn Program stored in this project.
        /// </summary>
        [System.Obsolete("Use the Program property instead, which caches its return value.")]
        public Program GetProgram()
        {
            return Program.Parser.ParseFrom(CompiledYarnProgram);
        }

        /// <summary>
        /// Gets the Yarn Program stored in this project.
        /// </summary>
        /// <remarks>
        /// The first time this is called, the program stored in <see
        /// cref="CompiledYarnProgram"/> is deserialized and cached. Future
        /// calls to this method will return the cached value.
        /// </remarks>
        public Program Program
        {
            get {
                if (cachedProgram == null)
                {
                    cachedProgram = Program.Parser.ParseFrom(CompiledYarnProgram);
                }
                return cachedProgram;
            }
        }
    }

    public enum LocalizationType
    {
        YarnInternal,
        Unity,
    }
}