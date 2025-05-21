#nullable disable

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
using YarnSpinnerGodot;
#endif

namespace YarnSpinnerGodot;

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
[GlobalClass]
public partial class YarnProject : Resource
{
    /// <summary>
    /// File extension of JSON yarn project files.
    /// </summary>
    public const string YARN_PROJECT_EXTENSION = ".yarnproject";

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

    /// <summary>
    /// Path in the .godot folder used to store generated data
    /// for this project
    /// </summary>
    [Export] public string ImportPath;

    public byte[] CompiledYarnProgram => Convert.FromBase64String(CompiledYarnProgramBase64);

    [Export] public string CompiledYarnProgramBase64;

    [Export] public Localization baseLocalization;

    /// <summary>
    /// res:// path to the .yarnproject file
    /// </summary>
    [Export] public string JSONProjectPath;

    /// <summary>
    /// Whether to generate a C# file that contains properties for each variable.
    /// </summary>
    /// <seealso cref="variablesClassName"/>
    /// <seealso cref="variablesClassNamespace"/>
    /// <seealso cref="variablesClassParent"/>
    [Export] public bool generateVariablesSourceFile;

    /// <summary>
    /// The name of the generated variables storage class.
    /// </summary>
    /// <seealso cref="generateVariablesSourceFile"/>
    /// <seealso cref="variablesClassNamespace"/>
    /// <seealso cref="variablesClassParent"/>
    [Export] public string variablesClassName = "YarnVariables";

    /// <summary>
    /// The namespace of the generated variables storage class.
    /// </summary>
    /// <seealso cref="generateVariablesSourceFile"/>
    /// <seealso cref="variablesClassName"/>
    /// <seealso cref="variablesClassParent"/>
    [Export] public string variablesClassNamespace = null;

    /// <summary>
    /// The parent class of the generated variables storage class.
    /// </summary>
    /// <seealso cref="generateVariablesSourceFile"/>
    /// <seealso cref="variablesClassName"/>
    /// <seealso cref="variablesClassNamespace"/>
    [Export] public string variablesClassParent = typeof(InMemoryVariableStorage).FullName;

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

    /// <summary>
    /// Base language that the .yarn scripts are written in.
    /// Stored in the .yarnproject file
    /// </summary>
    public string defaultLanguage => JSONProject.BaseLanguage;
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
                    _lineMetadata =
                        JsonSerializer.Deserialize(_lineMetadataJSON, YarnJSONContext.Default.LineMetadata);
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
            _lineMetadataJSON = JsonSerializer.Serialize(_lineMetadata, YarnJSONContext.Default.LineMetadata);
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
                    _listOfFunctions = JsonSerializer.Deserialize(_listOfFunctionsJSON,
                        YarnJSONContext.Default.FunctionInfoArray);
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
            _listOfFunctionsJSON =
                JsonSerializer.Serialize(_listOfFunctions, YarnJSONContext.Default.FunctionInfoArray);
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
                        JsonSerializer.Deserialize(_serializedDeclarationsJSON,
                            YarnJSONContext.Default.SerializedDeclarationArray);
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
            _serializedDeclarationsJSON = JsonSerializer.Serialize(_serializedDeclarations,
                YarnJSONContext.Default.SerializedDeclarationArray);
        }
    }

    [Export] private string _serializedDeclarationsJSON;

    [Export] public YarnProjectError[] ProjectErrors = Array.Empty<YarnProjectError>();

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
            cachedProgram ??= Program.Parser.ParseFrom(CompiledYarnProgram);

            return cachedProgram;
        }
    }
}