using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Godot;
using Godot.Collections;
using Yarn;
using Array = System.Array;
#if TOOLS
using YarnSpinnerGodot.Editor;
#endif

namespace YarnSpinnerGodot
{
    [Tool]
    public partial class YarnProject : Resource
    {
        public static JsonSerializerOptions JSONOptions = new JsonSerializerOptions {IncludeFields = true};

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
        [Export] public bool IsSuccessfullyParsed;

        public byte[] CompiledYarnProgram => Convert.FromBase64String(CompiledYarnProgramBase64);

        [Export] public string CompiledYarnProgramBase64;

        public List<Resource> ScriptsWithParseErrors => new List<Resource>();

        [Export] public Array<string> SourceScripts = new Array<string>();
        [Export] public Localization baseLocalization;

        /// <summary>
        /// Mapping of non-base locale codes to a path where Yarn will read & write
        /// its localization CSV files. These will be automatically compiled into Godot
        /// .translation files in the same directory as the csv.  YarnSpinner-Godot will automatically
        /// mark the .csv files as 'Keep file (no import)' when creating them, but if that setting changes,
        /// it will cause Godot import errors as the CSVs are not in Godot localization's expected format. 
        /// If that happens, you may notice that Godot generates several translation files in error, such as 
        /// <language>.metadata.translation, <language>.comment.translation, etc.
        /// If the import setting is lost, go to the import tab on the CSV and select the 
        /// "Keep file (no import)" setting under "Import As". 
        /// </summary>
        [Export] public Godot.Collections.Dictionary<string, string> LocaleCodeToCSVPath =
            new Godot.Collections.Dictionary<string, string>();

        private LineMetadata _lineMetadata;

        public LineMetadata LineMetadata
        {
            get
            {
                if (_lineMetadata != null)
                {
                    return _lineMetadata;
                }
                if (!string.IsNullOrEmpty(_lineMetadataJSON))
                {
                    try
                    {
                        _lineMetadata = JsonSerializer.Deserialize<LineMetadata>(_lineMetadataJSON, JSONOptions);
                    }
                    catch (Exception e)
                    {
                        GD.PushError(
                            $"Error parsing {nameof(LineMetadata)} from {ResourcePath}. The JSON data may have been corrupted. Error: {e.Message}\n{e.StackTrace}");
                    }
                }
                else
                {
                    LineMetadata = new LineMetadata();
                }

                return _lineMetadata;
            }
            set
            {
                _lineMetadata = value;
                _lineMetadataJSON = JsonSerializer.Serialize(_lineMetadata, JSONOptions);
#if TOOLS
                YarnProjectEditorUtility.ClearJSONCache();
#endif
            }
        }

        [Export] private string _lineMetadataJSON;

        private FunctionInfo[] _listOfFunctions;

        public FunctionInfo[] ListOfFunctions
        {
            get
            {
                if (_listOfFunctions != null)
                {
                    return _listOfFunctions;
                }
                if (!string.IsNullOrEmpty(_listOfFunctionsJSON))
                {
                    try
                    {
                        _listOfFunctions = JsonSerializer.Deserialize<FunctionInfo[]>(_listOfFunctionsJSON);
                    }
                    catch (Exception e)
                    {
                        GD.PushError(
                            $"Error parsing {nameof(ListOfFunctions)} from {ResourcePath}. The JSON data may have been corrupted. Error: {e.Message}\n{e.StackTrace}");
                    }
                }
                else
                {
                    ListOfFunctions = Array.Empty<FunctionInfo>();
                }

                return _listOfFunctions;
            }
            set
            {
                _listOfFunctions = value;
                _listOfFunctionsJSON = JsonSerializer.Serialize(_listOfFunctions, JSONOptions);
#if TOOLS
                YarnProjectEditorUtility.ClearJSONCache();
#endif
            }
        }

        [Export] private string _listOfFunctionsJSON;

        private SerializedDeclaration[] _serializedDeclarations;

        public SerializedDeclaration[] SerializedDeclarations
        {
            get
            {
                if (_serializedDeclarations != null)
                {
                    return _serializedDeclarations;
                }
                if (!string.IsNullOrEmpty(_serializedDeclarationsJSON))
                {
                    try
                    {
                        _serializedDeclarations =
                            JsonSerializer.Deserialize<SerializedDeclaration[]>(_serializedDeclarationsJSON);
                    }
                    catch (Exception e)
                    {
                        GD.PushError(
                            $"Error parsing {nameof(SerializedDeclarations)} from {ResourcePath}. The JSON data may have been corrupted. Error: {e.Message}\n{e.StackTrace}");
                    }
                }
                else
                {
                    SerializedDeclarations = Array.Empty<SerializedDeclaration>();
                }
                return _serializedDeclarations;
            }
            set
            {
                _serializedDeclarations = value;
                _serializedDeclarationsJSON =  JsonSerializer.Serialize(_serializedDeclarations, JSONOptions);
#if TOOLS
                YarnProjectEditorUtility.ClearJSONCache();
#endif
            }
        }

        [Export] private string _serializedDeclarationsJSON;

        [Export] [Language] public string defaultLanguage = CultureInfo.CurrentCulture.Name;

#if TOOLS
        /// <summary>
        /// Search all directories for .yarn files and save the list to the project
        /// </summary>
        /// <returns></returns>
        public void SearchForSourceScripts()
        {
            try
            {
                if (string.IsNullOrEmpty(ResourcePath))
                {
                    GD.Print(
                        $"{nameof(YarnProject)}s must be saved to a file in your project to be used with this plugin.");
                    return;
                }

                var projectDir = ProjectSettings.GlobalizePath(ResourcePath);
                projectDir = Directory.GetParent(projectDir).FullName;
                var allProjects =
                    (Godot.Collections.Array) ProjectSettings.GetSetting(YarnProjectEditorUtility
                        .YARN_PROJECT_PATHS_SETTING_KEY);
                var nestedYarnProjects = new List<string>();
                foreach (string project in allProjects)
                {
                    var absoluteOtherProjectDir = ProjectSettings.GlobalizePath(project);
                    absoluteOtherProjectDir = Directory.GetParent(absoluteOtherProjectDir).FullName;
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
                GD.PushError(
                    $"Error searching for .yarn scripts in Yarn Project '{ResourcePath}': {e.Message}{e.StackTrace}");
            }
        }

        /// <summary>
        /// Find a list of all .yarn files that this YarnProject is responsible for compiling.
        /// </summary>
        /// <param name="nestedYarnProjectDirs">list of other yarn projects that are below this one. used to exclude .yarn files covered by deeper nested yarn projects from this project.</param>
        /// <param name="dirPath">the directory to search for files and child directories</param>
        /// <param name="scripts"></param>
        /// <returns></returns>
        private List<string> FindSourceScriptsRecursive(List<string> nestedYarnProjectDirs, string dirPath,
            List<string> scripts)
        {
            var files = Directory.GetFiles(dirPath);
            foreach (var file in files)
            {
                if (file.ToUpperInvariant().EndsWith(".YARN"))
                {
                    var scriptResPath = ProjectSettings.LocalizePath(file.Replace("\\", "/"));
                    scripts.Add(scriptResPath);
                }
            }

            var subdirectories = Directory.GetDirectories(dirPath);
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

        [Export] public YarnProjectError[] ProjectErrors = Array.Empty<YarnProjectError>();

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
        private Program cachedProgram;

        /// <summary>
        /// The names of assemblies that <see cref="ActionManager"/> should look
        /// for commands and functions in when this project is loaded into a
        /// <see cref="DialogueRunner"/>.
        /// </summary>
        public Array<string> searchAssembliesForActions = new Array<string>();

        /// <summary>
        /// Gets the Yarn Program stored in this project.
        /// </summary>
        [Obsolete("Use the Program property instead, which caches its return value.")]
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
            get
            {
                if (cachedProgram == null)
                {
                    cachedProgram = Program.Parser.ParseFrom(CompiledYarnProgram);
                }

                return cachedProgram;
            }
        }
    }
}