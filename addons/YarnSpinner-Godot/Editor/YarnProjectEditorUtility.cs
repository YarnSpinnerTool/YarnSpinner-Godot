#if TOOLS
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using Google.Protobuf;
using Microsoft.Extensions.FileSystemGlobbing;
using Yarn;
using Yarn.Compiler;
using Yarn.Utility;
using File = System.IO.File;
using Path = System.IO.Path;
#if YARNSPINNER_DEBUG
using System.Diagnostics;
#endif

#nullable enable
namespace YarnSpinnerGodot;

[Tool]
public static class YarnProjectEditorUtility
{
    /// <summary>
    /// The contents of a .csv.import file to avoid importing it as a Godot localization csv file
    /// </summary>
    public const string KEEP_IMPORT_TEXT = "[remap]\n\nimporter=\"keep\"";

    /// <summary>
    /// Find an associated yarn project in the same or ancestor directory
    /// </summary>
    /// <param name="scriptPath"></param>
    /// <returns></returns>
    public static string? GetDestinationProjectPath(string scriptPath)
    {
        string? destinationProjectPath = null;
        var globalScriptPath = Path.GetFullPath(ProjectSettings.GlobalizePath(scriptPath));
        var allProjects = FindAllYarnProjects();
        foreach (var project in allProjects)
        {
            var projectPath = ProjectSettings.GlobalizePath(project.ToString())
                .Replace("\\", "/");
            try
            {
                var loadedProject = Yarn.Compiler.Project.LoadFromFile(projectPath);
                if (!loadedProject.SourceFiles.Contains(globalScriptPath))
                {
                    continue;
                }

                destinationProjectPath = ProjectSettings.LocalizePath(projectPath);
                break;
            }
            catch (Exception e)
            {
                GD.PushError(
                    $"Error while searching for the project associated with {scriptPath}: {e.Message}\n{e.StackTrace}");
            }
        }

        return destinationProjectPath;
    }

    private static IEnumerable FindAllYarnProjects()
    {
        var projectMatcher = new Matcher();
        projectMatcher.AddInclude($"**/*{YarnProject.YARN_PROJECT_EXTENSION}");
        return projectMatcher.GetResultsInFullPath(ProjectSettings.GlobalizePath("res://"))
            .Select(ProjectSettings.LocalizePath);
    }

    private const int PROJECT_UPDATE_TIMEOUT = 500; // ms 

    private static ConcurrentDictionary<string, DateTime> _projectPathToLastUpdateTime =
        new();

    private static object _lastUpdateLock = new();

    /// <summary>
    /// Queue up a re-compile of scripts in a yarn project, add all associated data to the project,
    /// and save it back to disk in the same .tres file. This will wait for <see cref="PROJECT_UPDATE_TIMEOUT"/>
    /// before updating the project, resetting the timeout each time it is called for a given YarnProject.
    /// Call this method when you want to avoid repeated updates of the same project.
    /// </summary>
    /// <param name="project">The yarn project to re-compile scripts for</param>
    public static void UpdateYarnProject(YarnProject project)
    {
        if (!GodotObject.IsInstanceValid(project))
        {
            return;
        }

        if (string.IsNullOrEmpty(project.ResourcePath))
        {
            return;
        }

        TimeSpan? lastCompile = null;
        if (_projectPathToLastUpdateTime.ContainsKey(project.ResourcePath))
        {
            lastCompile = (DateTime.Now - _projectPathToLastUpdateTime[project.ResourcePath]);
        }

        if (lastCompile is { TotalMilliseconds: < PROJECT_UPDATE_TIMEOUT })
        {
            // skip repeated compilations in case many yarn files under the same yarn project
            // trigger repeated compiles
            return;
        }

        try
        {
            UpdateYarnProjectImmediate(project);
        }
        finally
        {
            // update the cooldown even if the import failed
            _projectPathToLastUpdateTime[project.ResourcePath] = DateTime.Now;
        }
    }

    /// <summary>
    /// Like <see cref="UpdateYarnProject"/>, but rather than queueing up an update,
    /// update it immediately. Call this when you don't anticipate another
    /// update request to be triggered  within a second or so.
    /// </summary>
    public static void UpdateYarnProjectImmediate(YarnProject project)
    {
        try
        {
            var compilationResult = CompileAllScripts(project);
            SaveYarnProject(project);
            if (project is { generateVariablesSourceFile: true, ResourcePath: not null }
                && compilationResult != null)
            {
                var fileName = project.variablesClassName + ".cs";

                var generatedSourcePath =
                    Path.Combine(Path.GetDirectoryName(ProjectSettings.GlobalizePath(project.ResourcePath))!,
                        fileName);
                bool generated = GenerateVariableSource(generatedSourcePath, project, compilationResult);
                if (generated)
                {
                    YarnSpinnerPlugin.editorInterface.GetResourceFilesystem().ScanSources();
                }
            }
        }
        catch (Exception e)
        {
            GD.PushError(
                $"Error updating {nameof(YarnProject)} '{project.ResourcePath}': {e.Message}{e.StackTrace}");
        }
    }

    public static void WriteBaseLanguageStringsCSV(YarnProject project, string path)
    {
        UpdateLocalizationFile(project.baseLocalization.GetStringTableEntries(),
            project.JSONProject.BaseLanguage, path, false);
    }

    public static void UpdateLocalizationCSVs(YarnProject project)
    {
        if (project.JSONProject.Localisation.Count > 0)
        {
            var modifiedFiles = new List<string>();
            if (project.baseLocalization == null)
            {
                CompileAllScripts(project);
            }

            foreach (var loc in project.JSONProject.Localisation)
            {
                if (string.IsNullOrEmpty(loc.Value.Strings))
                {
                    GD.PrintErr(
                        $"Can't update localization for {loc.Key} because it doesn't have a Strings file.");
                    continue;
                }

                if (project.baseLocalization == null)
                {
                    GD.PrintErr(
                        $"Can't update localization for {loc.Key} because it doesn't have a {nameof(project.baseLocalization)}.");
                    continue;
                }

                var fileWasChanged = UpdateLocalizationFile(project.baseLocalization.GetStringTableEntries(),
                    loc.Key, loc.Value.Strings);

                if (fileWasChanged)
                {
                    modifiedFiles.Add(loc.Value.Strings);
                }
            }

            if (modifiedFiles.Count > 0)
            {
                GD.Print($"Updated the following files: {string.Join(", ", modifiedFiles)}");
            }
            else
            {
                GD.Print($"No Localization CSV files needed updating.");
            }
        }
    }

    /// <summary>
    /// Verifies the CSV file referred to by csvResourcePath and updates it if
    /// necessary.
    /// </summary>
    /// <param name="baseLocalizationStrings">A collection of <see
    /// cref="StringTableEntry"/></param>
    /// <param name="language">The language that <paramref name="csvResourcePath"/> provides strings for.</param>
    /// <param name="csvResourcePath">res:// path to the destination CSV to update</param>
    /// <returns>Whether the contents of <paramref name="csvResourcePath"/> was modified.</returns>
    private static bool UpdateLocalizationFile(IEnumerable<StringTableEntry> baseLocalizationStrings,
        string language, string csvResourcePath, bool generateTranslation = true)
    {
        var absoluteCSVPath = ProjectSettings.GlobalizePath(csvResourcePath);

        // Tracks if the translated localisation needed modifications
        // (either new lines added, old lines removed, or changed lines
        // flagged)
        var modificationsNeeded = false;

        IEnumerable<StringTableEntry> translatedStrings = new List<StringTableEntry>();
        if (File.Exists(absoluteCSVPath))
        {
            var existingCSVText = File.ReadAllText(absoluteCSVPath);
            translatedStrings = StringTableEntry.ParseFromCSV(existingCSVText);
        }
        else
        {
            GD.Print(
                $"CSV file {csvResourcePath} did not exist for locale {language}. A new file will be created at that location.");
            modificationsNeeded = true;
        }

        // Convert both enumerables to dictionaries, for easier lookup
        var baseDictionary = baseLocalizationStrings.ToDictionary(entry => entry.ID);
        var translatedDictionary = translatedStrings.ToDictionary(entry => entry.ID);

        // The list of line IDs present in each localisation
        var baseIDs = baseLocalizationStrings.Select(entry => entry.ID);
        foreach (var str in translatedStrings)
        {
            if (baseDictionary.ContainsKey(str.ID))
            {
                str.Original = baseDictionary[str.ID].Text;
            }
        }

        var translatedIDs = translatedStrings.Select(entry => entry.ID);

        // The list of line IDs that are ONLY present in each
        // localisation
        var onlyInBaseIDs = baseIDs.Except(translatedIDs);
        var onlyInTranslatedIDs = translatedIDs.Except(baseIDs);

        // Remove every entry whose ID is only present in the
        // translated set. This entry has been removed from the base
        // localization.
        foreach (var id in onlyInTranslatedIDs.ToList())
        {
            translatedDictionary.Remove(id);
            modificationsNeeded = true;
        }

        // Conversely, for every entry that is only present in the base
        // localisation, we need to create a new entry for it.
        foreach (var id in onlyInBaseIDs)
        {
            StringTableEntry baseEntry = baseDictionary[id];
            baseEntry.File = ProjectSettings.LocalizePath(baseEntry.File);
            var newEntry = new StringTableEntry(baseEntry)
            {
                // Empty this text, so that it's apparent that a
                // translated version needs to be provided.
                Text = string.Empty,
                Original = baseEntry.Text,
                Language = language,
            };
            translatedDictionary.Add(id, newEntry);
            modificationsNeeded = true;
        }

        // Finally, we need to check for any entries in the translated
        // localisation that:
        // 1. have the same line ID as one in the base, but
        // 2. have a different Lock (the hash of the text), which
        //    indicates that the base text has changed.

        // First, get the list of IDs that are in both base and
        // translated, and then filter this list to any where the lock
        // values differ
        var outOfDateLockIDs = baseDictionary.Keys
            .Intersect(translatedDictionary.Keys)
            .Where(id => baseDictionary[id].Lock != translatedDictionary[id].Lock);

        // Now loop over all of these, and update our translated
        // dictionary to include a note that it needs attention
        foreach (var id in outOfDateLockIDs)
        {
            // Get the translated entry as it currently exists
            var entry = translatedDictionary[id];

            // Include a note that this entry is out of date
            entry.Text = $"(NEEDS UPDATE) {entry.Text}";

            // update the base language text
            entry.Original = baseDictionary[id].Text;
            // Update the lock to match the new one
            entry.Lock = baseDictionary[id].Lock;

            // Put this modified entry back in the table
            translatedDictionary[id] = entry;

            modificationsNeeded = true;
        }

        // We're all done!

        if (modificationsNeeded == false)
        {
            if (generateTranslation)
            {
                GenerateGodotTranslation(language, csvResourcePath);
            }

            // No changes needed to be done to the translated string
            // table entries. Stop here.
            return false;
        }

        // We need to produce a replacement CSV file for the translated
        // entries.

        var outputStringEntries = translatedDictionary.Values
            .OrderBy(entry => entry.File)
            .ThenBy(entry => int.Parse(entry.LineNumber));

        var outputCSV = StringTableEntry.CreateCSV(outputStringEntries);

        // Write out the replacement text to this existing file,
        // replacing its existing contents
        File.WriteAllText(absoluteCSVPath, outputCSV, System.Text.Encoding.UTF8);
        var csvImport = $"{absoluteCSVPath}.import";
        if (!File.Exists(csvImport))
        {
            File.WriteAllText(csvImport, KEEP_IMPORT_TEXT);
        }

        if (generateTranslation)
        {
            GenerateGodotTranslation(language, csvResourcePath);
        }

        // Signal that the file was changed
        return true;
    }

    private static void GenerateGodotTranslation(string language, string csvFilePath)
    {
        var absoluteCSVPath = ProjectSettings.GlobalizePath(csvFilePath);
        var translation = new Translation();
        translation.Locale = language;

        var csvText = File.ReadAllText(absoluteCSVPath);
        var stringEntries = StringTableEntry.ParseFromCSV(csvText);
        foreach (var entry in stringEntries)
        {
            translation.AddMessage(entry.ID, entry.Text);
        }

        var extensionRegex = new Regex(@".csv$");
        var translationPath = extensionRegex.Replace(absoluteCSVPath, ".translation");
        var translationResPath = ProjectSettings.LocalizePath(translationPath);
        ResourceSaver.Save(translation, translationResPath);
        GD.Print($"Wrote translation file for {language} to {translationResPath}.");
    }

    /// <summary>
    ///  Workaround for https://github.com/godotengine/godot/issues/78513
    /// </summary>
    public static void ClearJSONCache()
    {
        var assembly = typeof(JsonSerializerOptions).Assembly;
        var updateHandlerType = assembly.GetType("System.Text.Json.JsonSerializerOptionsUpdateHandler");
        var clearCacheMethod =
            updateHandlerType?.GetMethod("ClearCache", BindingFlags.Static | BindingFlags.Public);
        clearCacheMethod?.Invoke(null, new object?[] { null });
    }

    public static void SaveYarnProject(YarnProject project)
    {
        // force the JSON serialization to update before saving 
        if (GodotObject.IsInstanceValid(project.baseLocalization))
        {
            project.baseLocalization.stringTable = project.baseLocalization.stringTable;
        }

        project.LineMetadata = project.LineMetadata;
        project.ListOfFunctions = project.ListOfFunctions;
        project.SerializedDeclarations = project.SerializedDeclarations;
        if (string.IsNullOrEmpty(project.JSONProjectPath))
        {
            project.JSONProjectPath = project.DefaultJSONProjectPath;
        }

        var saveErr = ResourceSaver.Save(project, project.ImportPath);
        if (saveErr != Error.Ok)
        {
            GD.PushError($"Error updating YarnProject {project.ResourceName} to {project.ResourcePath}: {saveErr}");
        }
        else
        {
            GD.Print($"Wrote updated YarnProject {project.ResourceName} to {project.ResourcePath}");
        }
    }

    private class FunctionDeclarationReceiver : IActionRegistration, IDisposable
    {
        public List<Declaration> FunctionDeclarations = new();

        public void AddCommandHandler(string commandName, System.Delegate handler)
        {
        }

        public void AddCommandHandler(string commandName, MethodInfo methodInfo)
        {
        }

        public void AddFunction(string name, System.Delegate implementation)
        {
        }

        public void RegisterFunctionDeclaration(string name, System.Type returnType, System.Type[] parameterTypes)
        {
            if (Types.TypeMappings.TryGetValue(returnType, out var returnYarnType) == false)
            {
                GD.PushError($"Can't register function {name}: can't convert return type {returnType} to a Yarn type");
                return;
            }

            var typeBuilder = new FunctionTypeBuilder().WithReturnType(returnYarnType);


            for (int i = 0; i < parameterTypes.Length; i++)
            {
                System.Type? parameter = parameterTypes[i];

                bool isParamsArray = false;

                if (i == parameterTypes.Length - 1 && parameter.IsArray)
                {
                    // If this is the last parameter and it is an array,
                    // treat it as though it were a params array and use the
                    // type of the array
                    parameter = parameter.GetElementType();
                    isParamsArray = true;
                }

                if (Types.TypeMappings.TryGetValue(parameter!, out var parameterYarnType) == false)
                {
                    GD.PushError(
                        $"Can't register function {name}: can't convert parameter {i} type {parameterYarnType} to a Yarn type");
                    return;
                }

                if (isParamsArray)
                {
                    typeBuilder = typeBuilder.WithVariadicParameterType(parameterYarnType);
                }
                else
                {
                    typeBuilder = typeBuilder.WithParameter(parameterYarnType);
                }
            }

            var decl = new DeclarationBuilder()
                .WithName(name)
                .WithType(typeBuilder.FunctionType)
                .Declaration;

            this.FunctionDeclarations.Add(decl);
        }

        public void RemoveCommandHandler(string commandName)
        {
        }

        public void RemoveFunction(string name)
        {
        }

        public void Dispose()
        {
            FunctionDeclarations.Clear();
        }
    }

    public static CompilationResult? CompileAllScripts(YarnProject project)
    {
        lock (project)
        {
            List<FunctionInfo> newFunctionList = new();
            var assetPath = project.ResourcePath;
            GD.Print($"Compiling all scripts in {assetPath}");

            project.ResourceName = Path.GetFileNameWithoutExtension(assetPath);
            var sourceScripts = project.JSONProject.SourceFiles.ToList();
            if (!sourceScripts.Any())
            {
                GD.Print(
                    $"No .yarn files found matching the {nameof(project.JSONProject.SourceFilePatterns)} in {project.JSONProjectPath}");
                return null;
            }

            var library = Actions.GetLibrary();

            IEnumerable<Diagnostic> errors;
            project.ProjectErrors = Array.Empty<YarnProjectError>();

            // We now compile!
            var scriptAbsolutePaths = sourceScripts.ToList().Where(s => s != null)
                .Select(ProjectSettings.GlobalizePath).ToList();
            // Store the compiled program
            byte[]? compiledBytes = null;
            CompilationResult? compilationResult = new CompilationResult();
            if (scriptAbsolutePaths.Count > 0)
            {
                // Get all function declarations found in the Unity project
                var functionDeclarationReceiver = new FunctionDeclarationReceiver();

                foreach (var registrationAction in Actions.ActionRegistrationMethods)
                {
                    registrationAction(functionDeclarationReceiver, RegistrationType.Compilation);
                }

                var job = CompilationJob.CreateFromFiles(scriptAbsolutePaths);
                // job.VariableDeclarations = localDeclarations;
                job.CompilationType = CompilationJob.Type.FullCompilation;
                job.Library = library;
                job.LanguageVersion = project.JSONProject.FileVersion;
                job.Declarations = functionDeclarationReceiver.FunctionDeclarations;

                compilationResult = Yarn.Compiler.Compiler.Compile(job);

                errors = compilationResult.Diagnostics.Where(d =>
                    d.Severity == Diagnostic.DiagnosticSeverity.Error);

                if (errors.Any())
                {
                    var errorGroups = errors.GroupBy(e => e.FileName);
                    foreach (var errorGroup in errorGroups)
                    {
                        var errorMessages = errorGroup.Select(e => e.ToString());

                        foreach (var message in errorMessages)
                        {
                            GD.PushError($"Error compiling: {message}");
                        }
                    }

                    var projectErrors = errors.ToList().ConvertAll(e =>
                        new YarnProjectError
                        {
                            Context = e.Context,
                            Message = e.Message,
                            FileName = ProjectSettings.LocalizePath(e.FileName)
                        });
                    project.ProjectErrors = projectErrors.ToArray();
                    return compilationResult;
                }

                if (compilationResult.Program == null)
                {
                    GD.PushError(
                        "public error: Failed to compile: resulting program was null, but compiler did not report errors.");
                    return compilationResult;
                }

                // Store _all_ declarations - both the ones in this
                // .yarnproject file, and the ones inside the .yarn files.

                // While we're here, filter out any declarations that begin with our
                // Yarn public prefix. These are synthesized variables that are
                // generated as a result of the compilation, and are not declared by
                // the user.

                var newDeclarations = new List<Declaration>() //localDeclarations
                    .Concat(compilationResult.Declarations)
                    .Where(decl => !decl.Name.StartsWith("$Yarn.Internal."))
                    .Where(decl => decl.Type is not FunctionType)
                    .Select(decl =>
                    {
                        SerializedDeclaration? existingDeclaration = null;
                        // try to re-use a declaration if one exists to avoid changing the .tres file so much
                        foreach (var existing in project.SerializedDeclarations)
                        {
                            if (existing.name == decl.Name)
                            {
                                existingDeclaration = existing;
                                break;
                            }
                        }

                        var serialized = existingDeclaration ?? new SerializedDeclaration();
                        serialized.SetDeclaration(decl);
                        return serialized;
                    }).ToArray();
                project.SerializedDeclarations = newDeclarations;
                // Clear error messages from all scripts - they've all passed
                // compilation
                project.ProjectErrors = Array.Empty<YarnProjectError>();

                CreateYarnInternalLocalizationAssets(project, compilationResult);

                using var memoryStream = new MemoryStream();
                using var outputStream = new CodedOutputStream(memoryStream);
                // Serialize the compiled program to memory
                compilationResult.Program.WriteTo(outputStream);
                outputStream.Flush();

                compiledBytes = memoryStream.ToArray();
            }

            project.ListOfFunctions = newFunctionList.ToArray();
            project.CompiledYarnProgramBase64 = compiledBytes == null ? "" : Convert.ToBase64String(compiledBytes);
            var saveErr = ResourceSaver.Save(project, project.ImportPath,
                ResourceSaver.SaverFlags.ReplaceSubresourcePaths);
            if (saveErr != Error.Ok)
            {
                GD.PushError($"Failed to save updated {nameof(YarnProject)}: {saveErr}");
            }

            return compilationResult;
        }
    }

    private static void LogDiagnostic(Diagnostic diagnostic)
    {
        var messagePrefix = string.IsNullOrEmpty(diagnostic.FileName)
            ? string.Empty
            : $"{diagnostic.FileName}: {diagnostic.Range.Start}:{diagnostic.Range.Start.Character} ";

        var message = messagePrefix + diagnostic.Message;

        switch (diagnostic.Severity)
        {
            case Diagnostic.DiagnosticSeverity.Error:
                GD.PrintErr(message);
                break;
            case Diagnostic.DiagnosticSeverity.Warning:
                GD.Print(message);
                break;
            case Diagnostic.DiagnosticSeverity.Info:
                GD.Print(message);
                break;
        }
    }

    public static CompilationResult CompileProgram(FileInfo[] inputs)
    {
        // The list of all files and their associated compiled results

        var compilationJob = CompilationJob.CreateFromFiles(inputs.Select(fileInfo => fileInfo.FullName));

        CompilationResult compilationResult;

        try
        {
            compilationResult = Yarn.Compiler.Compiler.Compile(compilationJob);
        }
        catch (Exception e)
        {
            var errorBuilder = new StringBuilder();

            errorBuilder.AppendLine("Failed to compile because of the following error:");
            errorBuilder.AppendLine(e.ToString());

            GD.PrintErr(errorBuilder.ToString());
            throw new Exception();
        }

        return compilationResult;
    }
    
    private static void CreateYarnInternalLocalizationAssets(YarnProject project,
        CompilationResult compilationResult)
    {
        // Will we need to create a default localization? This variable
        // will be set to false if any of the languages we've
        // configured in languagesToSourceAssets is the default
        // language.
        var shouldAddDefaultLocalization = true;
        if (project.JSONProject.Localisation == null)
        {
            project.JSONProject.Localisation =
                new System.Collections.Generic.Dictionary<string, Project.LocalizationInfo>();
        }

        if (shouldAddDefaultLocalization)
        {
            // We didn't add a localization for the default language.
            // Create one for it now.
            var stringTableEntries = GetStringTableEntries(project, compilationResult);

            var developmentLocalization = project.baseLocalization ?? new Localization();
            developmentLocalization.Clear();
            developmentLocalization.ResourceName = $"Default ({project.defaultLanguage})";
            developmentLocalization.LocaleCode = project.defaultLanguage;

            // Add these new lines to the development localisation's asset
            foreach (var entry in stringTableEntries)
            {
                developmentLocalization.AddLocalisedStringToAsset(entry.ID, entry);
            }

            project.baseLocalization = developmentLocalization;

            // Since this is the default language, also populate the line metadata.
            project.LineMetadata ??= new LineMetadata();
            project.LineMetadata.Clear();
            project.LineMetadata.AddMetadata(LineMetadataTableEntriesFromCompilationResult(compilationResult));
        }
    }

    /// <summary>
    /// Generates a collection of <see cref="StringTableEntry"/>
    /// objects, one for each line in this Yarn Project's scripts.
    /// </summary>
    /// <returns>An IEnumerable containing a <see
    /// cref="StringTableEntry"/> for each of the lines in the Yarn
    /// Project, or <see langword="null"/> if the Yarn Project contains
    /// errors.</returns>
    public static IEnumerable<StringTableEntry> GenerateStringsTable(YarnProject project)
    {
        CompilationResult? compilationResult = CompileStringsOnly(project);

        if (compilationResult == null)
        {
            // We only get no value if we have no scripts to work with.
            // In this case, return an empty collection - there's no
            // error, but there's no content either.
            return [];
        }

        var errors =
            compilationResult.Diagnostics.Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error);

        if (errors.Any())
        {
            GD.PrintErr("Can't generate a strings table from a Yarn Project that contains compile errors", null);
            return [];
        }

        return GetStringTableEntries(project, compilationResult);
    }

    private static CompilationResult? CompileStringsOnly(YarnProject project)
    {
        var scriptPaths = project.JSONProject.SourceFiles.Where(s => s != null)
            .Select(ProjectSettings.GlobalizePath).ToList();

        if (!scriptPaths.Any())
        {
            // We have no scripts to work with.
            return null;
        }

        // We now now compile!
        var job = CompilationJob.CreateFromFiles(scriptPaths);
        job.CompilationType = CompilationJob.Type.StringsOnly;

        return Yarn.Compiler.Compiler.Compile(job);
    }

    private static IEnumerable<LineMetadataTableEntry> LineMetadataTableEntriesFromCompilationResult(
        CompilationResult result)
    {
        if (result.StringTable == null)
        {
            return [];
        }

        return result.StringTable.Select(x => new LineMetadataTableEntry
        {
            ID = x.Key,
            File = ProjectSettings.LocalizePath(x.Value.fileName),
            Node = x.Value.nodeName,
            LineNumber = x.Value.lineNumber.ToString(),
            Metadata = RemoveLineIDFromMetadata(x.Value.metadata).ToArray()
        }).Where(x => x.Metadata.Length > 0);
    }

    private static IEnumerable<StringTableEntry> GetStringTableEntries(YarnProject project,
        CompilationResult result)
    {
        if (result.StringTable == null)
        {
            return [];
        }

        return result.StringTable.Select(x => new StringTableEntry
        {
            ID = x.Key,
            Language = project.defaultLanguage,
            Text = x.Value.text,
            File = ProjectSettings.LocalizePath(x.Value.fileName),
            Node = x.Value.nodeName,
            LineNumber = x.Value.lineNumber.ToString(),
            Lock = x.Value.text == null ? "" : YarnImporter.GetHashString(x.Value.text, 8),
            Comment = GenerateCommentWithLineMetadata(x.Value.metadata)
        });
    }

    /// <summary>
    /// Generates a string with the line metadata. This string is intended
    /// to be used in the "comment" column of a strings table CSV. Because
    /// of this, it will ignore the line ID if it exists (which is also
    /// part of the line metadata).
    /// </summary>
    /// <param name="metadata">The metadata from a given line.</param>
    /// <returns>A string prefixed with "Line metadata: ", followed by each
    /// piece of metadata separated by whitespace. If no metadata exists or
    /// only the line ID is part of the metadata, returns an empty string
    /// instead.</returns>
    private static string GenerateCommentWithLineMetadata(string[] metadata)
    {
        var cleanedMetadata = RemoveLineIDFromMetadata(metadata);

        if (!cleanedMetadata.Any())
        {
            return string.Empty;
        }

        return $"Line metadata: {string.Join(" ", cleanedMetadata)}";
    }

    /// <summary>
    /// Removes any line ID entry from an array of line metadata.
    /// Line metadata will always contain a line ID entry if it's set. For
    /// example, if a line contains "#line:1eaf1e55", its line metadata
    /// will always have an entry with "line:1eaf1e55".
    /// </summary>
    /// <param name="metadata">The array with line metadata.</param>
    /// <returns>An IEnumerable with any line ID entries removed.</returns>
    private static IEnumerable<string> RemoveLineIDFromMetadata(string[] metadata)
    {
        return metadata.Where(x => !x.StartsWith("line:"));
    }

    /// <summary>
    /// Update any .yarn scripts in the project to add #line: tags with
    /// unique IDs.
    /// </summary>
    /// <param name="project">The YarnProject to update the scripts for</param>
    /// <param name="editorInterface">reference to the EditorInterface</param>
    public static void AddLineTagsToFilesInYarnProject(YarnProject project)
    {
        // First, gather all existing line tags across ALL yarn
        // projects, so that we don't accidentally overwrite an
        // existing one. Do this by finding all yarn scripts in all
        // yarn projects, and get the string tags inside them.

        var allYarnFiles =
            // Get the path for each script
            // remove any nulls, in case any are found
            // get all yarn projects across the entire project
            LoadAllYarnProjects()
                // Get all of their source scripts, as a single sequence
                .SelectMany(proj => proj.JSONProject.SourceFiles).ToList()
                .Where(path => path != null);

#if YARNSPINNER_DEBUG
        var stopwatch = Stopwatch.StartNew();
#endif

        var library = new Library();

        // Compile all of these, and get whatever existing string tags
        // they had. Do each in isolation so that we can continue even
        // if a file contains a parse error.
        var allExistingTags = allYarnFiles.SelectMany(path =>
        {
            // Compile this script in strings-only mode to get
            // string entries
            var compilationJob = CompilationJob.CreateFromFiles(ProjectSettings.GlobalizePath(path));
            compilationJob.CompilationType = CompilationJob.Type.StringsOnly;
            compilationJob.Library = library;
            var result = Yarn.Compiler.Compiler.Compile(compilationJob);

            bool containsErrors = result.Diagnostics
                .Any(d => d.Severity == Diagnostic.DiagnosticSeverity.Error);

            if (containsErrors)
            {
                GD.PrintErr($"Can't check for existing line tags in {path} because it contains errors.");
                return Array.Empty<string>();
            }

            return result.StringTable == null
                ? Array.Empty<string>()
                : result.StringTable.Where(i => i.Value.isImplicitTag == false).Select(i => i.Key);
        }).ToList(); // immediately execute this query so we can determine timing information

#if YARNSPINNER_DEBUG
        stopwatch.Stop();
        GD.Print($"Checked {allYarnFiles.Count()} yarn files for line tags in {stopwatch.ElapsedMilliseconds}ms");
#endif

        var modifiedFiles = new List<string>();

        try
        {
            foreach (var script in project.JSONProject.SourceFiles)
            {
                var assetPath = ProjectSettings.GlobalizePath(script);
                var contents = File.ReadAllText(assetPath);

                // Produce a version of this file that contains line
                // tags added where they're needed.
                var tagged = Yarn.Compiler.Utility.TagLines(contents, allExistingTags);

                var taggedVersion = tagged.Item1;

                // if the file has an error it returns null
                // we want to bail out then otherwise we'd wipe the yarn file
                if (taggedVersion == null)
                {
                    continue;
                }

                // If this produced a modified version of the file,
                // write it out and re-import it.
                if (contents != taggedVersion)
                {
                    modifiedFiles.Add(Path.GetFileNameWithoutExtension(assetPath));
                    File.WriteAllText(assetPath, taggedVersion, Encoding.UTF8);
                }
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"Encountered an error when updating scripts: {e}");
            return;
        }

        // Report on the work we did.
        if (modifiedFiles.Count > 0)
        {
            GD.Print($"Updated the following files: {string.Join(", ", modifiedFiles)}");
            // trigger reimport
            YarnSpinnerPlugin.editorInterface.GetResourceFilesystem().ScanSources();
        }
        else
        {
            GD.Print("No files needed updating.");
        }
    }

    /// <summary>
    /// Load all known YarnProject resources in the project
    /// </summary>
    /// <returns>a list of all YarnProject resources</returns>
    public static List<YarnProject> LoadAllYarnProjects()
    {
        var projects = new List<YarnProject>();
        var allProjects = FindAllYarnProjects();
        foreach (var path in allProjects)
        {
            projects.Add(ResourceLoader.Load<YarnProject>(path.ToString()));
        }

        return projects;
    }


    /// <summary>
    /// A regular expression that matches characters following the start of
    /// the string or an underscore. 
    /// </summary>
    /// <remarks>
    /// Used as part of converting variable names from snake_case to
    /// CamelCase when generating C# variable source code.
    /// </remarks>
    private static readonly System.Text.RegularExpressions.Regex SnakeCaseToCamelCase =
        new System.Text.RegularExpressions.Regex(@"(^|_)(\w)");


    private static bool GenerateVariableSource(string outputPath, YarnProject project,
        CompilationResult compilationResult)
    {
        if (!GodotObject.IsInstanceValid(project))
        {
            GD.PushError("Can't generate variable source for null project!");
            return false;
        }

        string? existingContent = null;

        if (File.Exists(outputPath))
        {
            // If the file already exists on disk, read it all in now. We'll
            // compare it to what we generated and, if the contents match
            // exactly, we don't need to re-import the resulting C# script.
            existingContent = File.ReadAllText(outputPath);
        }

        if (string.IsNullOrEmpty(project.variablesClassName))
        {
            GD.PushError("Can't generate variable interface, because the specified class name is empty.");
            return false;
        }

        StringBuilder sb = new StringBuilder();
        int indentLevel = 0;
        const int indentSize = 4;

        void WriteLine(string line = "", int offset = 0)
        {
            if (line.Length > 0)
            {
                sb.Append(new string(' ', (indentLevel + offset) * indentSize));
            }

            sb.AppendLine(line);
        }

        void WriteComment(string comment = "") => WriteLine("// " + comment);

        if (string.IsNullOrEmpty(project.variablesClassNamespace) == false)
        {
            WriteLine($"namespace {project.variablesClassNamespace} {{");
            WriteLine();
            indentLevel += 1;
        }

        WriteLine("using YarnSpinnerGodot;");
        WriteLine();

        void WriteGeneratedCodeAttribute()
        {
            var toolName = "YarnSpinner";
            var toolVersion = YarnSpinnerPlugin.VersionString;
            WriteLine($"[System.CodeDom.Compiler.GeneratedCode(\"{toolName}\", \"{toolVersion}\")]");
        }

        // For each user-defined enum, create a C# enum type
        IEnumerable<EnumType> enumTypes = compilationResult.UserDefinedTypes.OfType<Yarn.EnumType>();

        foreach (var type in enumTypes)
        {
            WriteLine($"/// <summary>");
            if (string.IsNullOrEmpty(type.Description) == false)
            {
                WriteLine($"/// {type.Description}");
            }
            else
            {
                WriteLine($"/// {type.Name}");
            }

            WriteLine($"/// </summary>");

            WriteLine($"/// <remarks>");
            WriteLine($"/// Automatically generated from Yarn project at {project.ResourcePath}.");
            WriteLine($"/// </remarks>");

            WriteGeneratedCodeAttribute();

            // Enums are always stored as integers; strings are represented
            // as CRC32 hashes of the raw value
            WriteLine($"public enum {type.Name} {{");

            indentLevel += 1;

            foreach (var enumCase in type.EnumCases)
            {
                WriteLine();

                WriteLine($"/// <summary>");
                if (string.IsNullOrEmpty(enumCase.Value.Description) == false)
                {
                    WriteLine($"/// {enumCase.Value.Description}");
                }
                else
                {
                    WriteLine($"/// {enumCase.Key}");
                }

                WriteLine($"/// </summary>");

                if (type.RawType == Types.Number)
                {
                    WriteLine($"{enumCase.Key} = {enumCase.Value.Value},");
                }
                else if (type.RawType == Types.String)
                {
                    WriteLine($"/// <remarks>");
                    WriteLine($"/// Backing value: \"{enumCase.Value.Value}\"");
                    WriteLine($"/// </remarks>");
                    var stringValue = (string)enumCase.Value.Value;
                    WriteComment($"\"{stringValue}\"");
                    // Get the hash of the string, and convert it to a
                    // signed integer. (Unity doesn't correctly handle enums
                    // whose backing value is a uint (values over signed
                    // integer max are clamped to zero), so we'll cast it here.)
                    WriteLine($"{enumCase.Key} = {(int)CRC32.GetChecksum(stringValue)},");
                }
                else
                {
                    WriteComment(
                        $"Error: enum case {type.Name}.{enumCase.Key} has an invalid raw type {type.RawType.Name}");
                }
            }

            indentLevel -= 1;

            WriteLine($"}}");
            WriteLine();
        }

        if (enumTypes.Any())
        {
            // Generate an extension class that extends the above enums with
            // methods that accesses their backing value
            WriteGeneratedCodeAttribute();
            WriteLine($"internal static class {project.variablesClassName}TypeExtensions {{");
            indentLevel += 1;
            foreach (var enumType in enumTypes)
            {
                var backingType = enumType.RawType == Types.Number ? "int" : "string";
                WriteLine($"internal static {backingType} GetBackingValue(this {enumType.Name} enumValue) {{");
                indentLevel += 1;
                WriteLine($"switch (enumValue) {{");
                indentLevel += 1;

                foreach (var @case in enumType.EnumCases)
                {
                    WriteLine($"case {enumType.Name}.{@case.Key}:", 1);
                    if (enumType.RawType == Types.Number)
                    {
                        WriteLine($"return {@case.Value.Value};", 2);
                    }
                    else if (enumType.RawType == Types.String)
                    {
                        WriteLine($"return \"{@case.Value.Value}\";", 2);
                    }
                    else
                    {
                        throw new System.ArgumentException($"Invalid Yarn enum raw type {enumType.RawType}");
                    }
                }

                WriteLine("default:", 1);
                WriteLine("throw new System.ArgumentException($\"{enumValue} is not a valid enum case.\");");

                indentLevel -= 1;
                WriteLine("}");
                indentLevel -= 1;
                WriteLine("}");
            }

            indentLevel -= 1;
            WriteLine("}");
        }

        WriteGeneratedCodeAttribute();
        WriteLine(
            $"public partial class {project.variablesClassName} : {project.variablesClassParent}, YarnSpinnerGodot.IGeneratedVariableStorage {{");

        indentLevel += 1;

        var declarationsToGenerate = compilationResult.Declarations
            .Where(d => d.IsVariable == true)
            .Where(d => d.Name.StartsWith("$Yarn.Internal") == false);

        if (declarationsToGenerate.Count() == 0)
        {
            WriteComment("This yarn project does not declare any variables.");
        }

        foreach (var decl in declarationsToGenerate)
        {
            string? cSharpTypeName = null;

            if (decl.Type == Yarn.Types.String)
            {
                cSharpTypeName = "string";
            }
            else if (decl.Type == Yarn.Types.Number)
            {
                cSharpTypeName = "float";
            }
            else if (decl.Type == Yarn.Types.Boolean)
            {
                cSharpTypeName = "bool";
            }
            else if (decl.Type is EnumType enumType1)
            {
                cSharpTypeName = enumType1.Name;
            }
            else
            {
                WriteLine(
                    $"#warning Can't generate a property for variable {decl.Name}, because its type ({decl.Type}) can't be handled.");
                WriteLine();
            }


            WriteComment($"Accessor for {decl.Type} {decl.Name}");

            // Remove '$'
            string cSharpVariableName = decl.Name.TrimStart('$');

            // Convert snake_case to CamelCase
            cSharpVariableName = SnakeCaseToCamelCase.Replace(cSharpVariableName,
                (match) => { return match.Groups[2].Value.ToUpperInvariant(); });

            // Capitalise first letter
            cSharpVariableName =
                cSharpVariableName.Substring(0, 1).ToUpperInvariant() + cSharpVariableName.Substring(1);

            if (decl.Description != null)
            {
                WriteLine("/// <summary>");
                WriteLine($"/// {decl.Description}");
                WriteLine("/// </summary>");
            }

            WriteLine($"public {cSharpTypeName} {cSharpVariableName} {{");

            indentLevel += 1;

            if (decl.Type is EnumType enumType)
            {
                WriteLine($"get => this.GetEnumValueOrDefault<{cSharpTypeName}>(\"{decl.Name}\");");
            }
            else
            {
                WriteLine($"get => this.GetValueOrDefault<{cSharpTypeName}>(\"{decl.Name}\");");
            }

            if (decl.IsInlineExpansion == false)
            {
                // Only generate a setter if it's a variable that can be modified
                if (decl.Type is EnumType e)
                {
                    WriteLine($"set => this.SetValue(\"{decl.Name}\", value.GetBackingValue());");
                }
                else
                {
                    WriteLine($"set => this.SetValue<{cSharpTypeName}>(\"{decl.Name}\", value);");
                }
            }

            indentLevel -= 1;

            WriteLine($"}}");

            WriteLine();
        }

        indentLevel -= 1;

        WriteLine($"}}");

        if (string.IsNullOrEmpty(project.variablesClassNamespace) == false)
        {
            indentLevel -= 1;
            WriteLine($"}}");
        }

        if (existingContent != null && existingContent.Equals(sb.ToString(), System.StringComparison.Ordinal))
        {
            // What we generated is identical to what's already on disk.
            // Don't write it.
            return false;
        }

        GD.Print($"Writing to {outputPath}");
        File.WriteAllText(outputPath, sb.ToString());

        return true;
    }
}
#endif