using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Godot;
using Godot.Collections;
using Yarn;
using Yarn.Compiler;
using Array = System.Array;
#if TOOLS
using YarnSpinnerGodot.Editor;
#endif

namespace YarnSpinnerGodot
{
    /// <summary>
    /// Godot resource which tracks the compiled Yarn Program and other metadata relating to your
    /// .yarnproject.
    /// <see cref="YarnProjectInspectorPlugin"/> will allow the user to view and
    /// update the fields that are stored in this resource as well as those
    /// in the associated .yarnproject file.
    /// 
    /// The Localisation field of the JSON .yarnproject file contains a mapping of non-base locale codes to a path where Yarn will read & write
    /// its localization CSV files. This information is stored in the .yarnproject file rather
    /// than the Godot YarnProject resource.
    /// These will be automatically compiled into Godot .translation files in
    /// the same directory as the csv.  YarnSpinner-Godot will automatically
    /// mark the .csv files as 'Keep file (no import)' when creating them, but if that setting changes,
    /// it will cause Godot import errors as the CSVs are not in Godot localization's expected format. 
    /// If that happens, you may notice that Godot generates several translation files in error, such as 
    /// [language].metadata.translation, [language].comment.translation, etc.
    /// If the import setting is lost, go to the import tab on the CSV and select the 
    /// "Keep file (no import)" setting under "Import As". 
    /// </summary>
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

        [Export] public Localization baseLocalization;

        /// <summary>
        /// res:// path to the .yarnproject file
        /// </summary>
        [Export] public string JSONProjectPath;
#if TOOLS
        public string DefaultJSONProjectPath => new Regex(@"\.tres$").Replace(ResourcePath, ".yarnproject");
        private Yarn.Compiler.Project _jsonProject;
        
        /// <summary>
        /// Information available in the editor via the .yarnproject file,
        /// parsed from JSON into a <see cref="Yarn.Compiler.Project"/>
        /// </summary>
        public Yarn.Compiler.Project JSONProject
        {
            get
            {
                if (_jsonProject == null)
                {
                    if (string.IsNullOrEmpty(JSONProjectPath))
                    {
                        JSONProjectPath = DefaultJSONProjectPath;

                    }
                    if (!File.Exists(ProjectSettings.GlobalizePath(JSONProjectPath)))
                    {
                        _jsonProject = new Yarn.Compiler.Project();
                        SaveJSONProject();
                    }
                    else
                    {
                        _jsonProject =
                            Yarn.Compiler.Project.LoadFromFile(ProjectSettings.GlobalizePath(JSONProjectPath));
                    }
                }

                return _jsonProject;
            }
            set => _jsonProject = value;
        }

        public void SaveJSONProject()
        {
            _jsonProject.SaveToFile(ProjectSettings.GlobalizePath(JSONProjectPath));
        }
#endif
        
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
                            $"Error parsing {nameof(LineMetadata)} from {ResourcePath}." +
                            $" The JSON data may have been corrupted. Error: {e.Message}\n{e.StackTrace}");
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
                _serializedDeclarationsJSON = JsonSerializer.Serialize(_serializedDeclarations, JSONOptions);
#if TOOLS
                YarnProjectEditorUtility.ClearJSONCache();
#endif
            }
        }

        [Export] private string _serializedDeclarationsJSON;

        /// <summary>
        /// Base language that the .yarn scripts are written in.
        /// Stored in the .yarnproject file
        /// </summary>
        public string defaultLanguage => JSONProject.BaseLanguage;

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
        /// Returns a list of all line and option IDs within the requested nodes
        /// </summary>
        /// <remarks>
        /// This is intended to be used either to precache multiple nodes worth of lines or for debugging
        /// </remarks>
        /// <param name="nodes">the names of all nodes whos line IDs you covet</param>
        /// <returns>The ids of all lines and options in the requested <paramref name="nodes"/> </returns>
        public IEnumerable<string> GetLineIDsForNodes(IEnumerable<string> nodes)
        {
            var ids = new List<string>();

            foreach (var node in nodes)
            {
                var lines = Program.LineIDsForNode(node);
                if (lines != null)
                {
                    ids.AddRange(lines);
                }
            }

            return ids;
        }

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