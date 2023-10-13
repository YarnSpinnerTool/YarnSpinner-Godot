#if TOOLS
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using Google.Protobuf;
using Yarn;
using Yarn.Compiler;
using File = System.IO.File;
using Path = System.IO.Path;

namespace YarnSpinnerGodot.Editor
{
    [Tool]
    public static class YarnProjectEditorUtility
    {
        public const string YARN_PROJECT_PATHS_SETTING_KEY = "YarnSpinnerGodot/YarnProjectPaths";

        /// <summary>
        /// The contents of a .csv.import file to avoid importing it as a Godot localization csv file
        /// </summary>
        public const string KEEP_IMPORT_TEXT = "[remap]\n\nimporter=\"keep\"";

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        /// <summary>
        /// Find an associated yarn project in the same or ancestor directory
        /// </summary>
        /// <param name="scriptPath"></param>
        /// <returns></returns>
        public static YarnProject GetDestinationProject(string scriptPath)
        {
            var projectRoot = ProjectSettings.GlobalizePath("res://");
            var scriptDir = ProjectSettings.GlobalizePath(scriptPath);
            scriptDir = Path.Combine(scriptDir, "..");
            scriptDir = Path.GetFullPath(scriptDir).Replace("\\", "/");
            string destinationProjectPath = null;

            var allProjects = (Godot.Collections.Array) ProjectSettings.GetSetting(YARN_PROJECT_PATHS_SETTING_KEY);
            foreach (var project in allProjects)
            {
                var projectPath = ProjectSettings.GlobalizePath(project.ToString())
                    .Replace("\\", "/");
                projectPath = projectPath.Substring(0, projectPath.LastIndexOf("/", StringComparison.Ordinal));
                if (scriptDir.Contains(projectPath) &&
                    (destinationProjectPath == null ||
                     destinationProjectPath.Length < projectPath.Length))
                {
                    // use the deepest matching directory
                    destinationProjectPath = project.ToString();
                }
            }

            if (destinationProjectPath == null)
            {
                return null;
            }

            destinationProjectPath = ProjectSettings.LocalizePath(destinationProjectPath);
            return ResourceLoader.Load<YarnProject>(destinationProjectPath);
        }


        private const int PROJECT_UPDATE_TIMEOUT = 200; // ms 

        private static ConcurrentDictionary<string, DateTime> _projectPathToLastUpdateTime =
            new ConcurrentDictionary<string, DateTime>();

        private static Dictionary<string, Task> _projectPathToUpdateTask = new Dictionary<string, Task>();
        private static object _lastUpdateLock = new object();

        /// <summary>
        /// Re-compile scripts in a yarn project, add all associated data to the project,
        /// and save it back to disk in the same .tres file.
        /// </summary>
        /// <param name="project"></param>
        public static void UpdateYarnProject(YarnProject project)
        {
            if (project == null) return;
            if (string.IsNullOrEmpty(project.ResourcePath)) return;
            lock (_lastUpdateLock)
            {
                _projectPathToLastUpdateTime[project.ResourcePath] = DateTime.Now;
                if (_projectPathToUpdateTask.ContainsKey(project.ResourcePath))
                {
                    return;
                }

                _projectPathToUpdateTask[project.ResourcePath] = UpdateYarnProjectTask(project);
            }
        }

        private static async Task UpdateYarnProjectTask(YarnProject project)
        {
            TimeSpan getTimeDiff()
            {
                lock (_lastUpdateLock)
                {
                    return DateTime.Now - _projectPathToLastUpdateTime[project.ResourcePath];
                }
            }

            while (getTimeDiff() < TimeSpan.FromMilliseconds(PROJECT_UPDATE_TIMEOUT))
            {
                // wait to update the yarn project until we haven't received another request in PROJECT_UPDATE_TIMEOUT ms
                await Task.Delay(PROJECT_UPDATE_TIMEOUT);
            }

            try
            {
                CompileAllScripts(project);
                UpdateMetadataCSV(project);
                SaveYarnProject(project);
                lock (_lastUpdateLock)
                {
                    _projectPathToUpdateTask.Remove(project.ResourcePath);
                }
            }
            catch (Exception e)
            {
                lock (_lastUpdateLock)
                {
                    _projectPathToUpdateTask.Remove(project.ResourcePath);
                }

                GD.PushError(
                    $"Error updating {nameof(YarnProject)} '{project.ResourcePath}': {e.Message}{e.StackTrace}");
            }
        }

        public static void UpdateMetadataCSV(YarnProject project)
        {
            if (project.LineMetadata != null && project.LineMetadata.stringsFile != null &&
                project.LineMetadata.stringsFile.Trim() != "")
            {
                var csvPath = project.LineMetadata.stringsFile;

                var csvText = LineMetadataTableEntry.CreateCSV(project.LineMetadata.GetAllMetadata());
                GD.Print($"Updating metadata csv file to: {csvPath}");
                csvPath = ProjectSettings.GlobalizePath(csvPath);
                var parent = Path.GetDirectoryName(csvPath);
                if (!Directory.Exists(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                File.WriteAllText(csvPath, csvText);
                var csvImport = $"{csvPath}.import";
                if (!File.Exists(csvImport))
                {
                    File.WriteAllText(csvImport, KEEP_IMPORT_TEXT);
                }
            }
        }

        public static void UpdateLocalizationCSVs(YarnProject project)
        {
            if (project.LocaleCodeToCSVPath.Count > 0)
            {
                var modifiedFiles = new List<string>();

                foreach (var loc in project.LocaleCodeToCSVPath)
                {
                    if (string.IsNullOrEmpty(loc.Value))
                    {
                        GD.PrintErr($"Can't update localization for {loc.Key} because it doesn't have a strings file.");
                        continue;
                    }

                    var fileWasChanged = UpdateLocalizationFile(project.baseLocalization.GetStringTableEntries(),
                        loc.Key, loc.Value);

                    if (fileWasChanged)
                    {
                        modifiedFiles.Add(loc.Value);
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
            string language, string csvResourcePath)
        {
            var absoluteCSVPath = ProjectSettings.GlobalizePath(csvResourcePath);

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

            // Tracks if the translated localisation needed modifications
            // (either new lines added, old lines removed, or changed lines
            // flagged)
            var modificationsNeeded = false;

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
                GenerateGodotTranslation(language, csvResourcePath);
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

            GenerateGodotTranslation(language, csvResourcePath);
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
            clearCacheMethod?.Invoke(null, new object?[] {null});
        }

        public static void SaveYarnProject(YarnProject project)
        {
            // force the JSON serialization to update before saving 
            project.baseLocalization.stringTable = project.baseLocalization.stringTable;
            project.LineMetadata = project.LineMetadata;
            project.ListOfFunctions = project.ListOfFunctions;
            project.SerializedDeclarations = project.SerializedDeclarations;

            // Prevent plugin failing to load when code is rebuilt
            ClearJSONCache();
            var saveErr = ResourceSaver.Save(project, project.ResourcePath);
            if (saveErr != Error.Ok)
            {
                GD.PushError($"Error updating YarnProject {project.ResourceName} to {project.ResourcePath}: {saveErr}");
            }
            else
            {
                GD.Print($"Wrote updated YarnProject {project.ResourceName} to {project.ResourcePath}");
            }
        }

        public static void CompileAllScripts(YarnProject project)
        {
            List<FunctionInfo> newFunctionList = new List<FunctionInfo>();
            var assetPath = project.ResourcePath;
            GD.Print($"Compiling all scripts in {assetPath}");

            project.ResourceName = Path.GetFileNameWithoutExtension(assetPath);
            project.SearchForSourceScripts();

            if (!project.SourceScripts.Any())
            {
                GD.Print($"No .yarn files in directories below {project.ResourcePath}");
                return;
            }

            var library = new Library();
            ActionManager.ClearAllActions();
            ActionManager.AddActionsFromAssemblies();
            ActionManager.RegisterFunctions(library);
            var existingFunctions = project.ListOfFunctions ?? Array.Empty<FunctionInfo>();
            var pretedermined = predeterminedFunctions().ToArray();
            foreach (var func in pretedermined)
            {
                FunctionInfo existingFunc = null;
                foreach (var existing in existingFunctions)
                {
                    if (existing.Name == func.Name)
                    {
                        existingFunc = existing;
                        existingFunc.Parameters = func.Parameters;
                        existingFunc.ReturnType = func.ReturnType;
                        break;
                    }
                }

                newFunctionList.Add(existingFunc ?? func);
            }

            IEnumerable<Diagnostic> errors;
            project.ProjectErrors = Array.Empty<YarnProjectError>();

            // We now now compile!
            var scriptAbsolutePaths = project.SourceScripts.ToList().Where(s => s != null)
                .Select(scriptResource => ProjectSettings.GlobalizePath(scriptResource)).ToList();
            // Store the compiled program
            byte[] compiledBytes = null;
            CompilationResult? compilationResult = new CompilationResult?();
            if (scriptAbsolutePaths.Count > 0)
            {
                var job = CompilationJob.CreateFromFiles(scriptAbsolutePaths);
                // job.VariableDeclarations = localDeclarations;
                job.CompilationType = CompilationJob.Type.FullCompilation;
                job.Library = library;
                compilationResult = Yarn.Compiler.Compiler.Compile(job);

                errors = compilationResult.Value.Diagnostics.Where(d =>
                    d.Severity == Diagnostic.DiagnosticSeverity.Error);

                if (errors.Count() > 0)
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
                    return;
                }

                if (compilationResult.Value.Program == null)
                {
                    GD.PushError(
                        "public error: Failed to compile: resulting program was null, but compiler did not report errors.");
                    return;
                }

                // Store _all_ declarations - both the ones in this
                // .yarnproject file, and the ones inside the .yarn files.

                // While we're here, filter out any declarations that begin with our
                // Yarn public prefix. These are synthesized variables that are
                // generated as a result of the compilation, and are not declared by
                // the user.

                var newDeclarations = new List<Declaration>() //localDeclarations
                    .Concat(compilationResult.Value.Declarations)
                    .Where(decl => !decl.Name.StartsWith("$Yarn.Internal."))
                    .Where(decl => !(decl.Type is FunctionType))
                    .Select(decl =>
                    {
                        SerializedDeclaration existingDeclaration = null;
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

                CreateYarnInternalLocalizationAssets(project, compilationResult.Value);

                using (var memoryStream = new MemoryStream())
                using (var outputStream = new CodedOutputStream(memoryStream))
                {
                    // Serialize the compiled program to memory
                    compilationResult.Value.Program.WriteTo(outputStream);
                    outputStream.Flush();

                    compiledBytes = memoryStream.ToArray();
                }
            }

            project.ListOfFunctions = newFunctionList.ToArray();
            project.CompiledYarnProgramBase64 = compiledBytes == null ? "" : Convert.ToBase64String(compiledBytes);
            ResourceSaver.Save(project, project.ResourcePath, ResourceSaver.SaverFlags.ReplaceSubresourcePaths);
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
            var results = new List<(FileInfo file, Program program, IDictionary<string, StringInfo> stringTable)>();

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

        public static FunctionInfo CreateFunctionInfoFromMethodGroup(MethodInfo method)
        {
            var returnType = $"-> {method.ReturnType.Name}";

            var parameters = method.GetParameters();
            var p = new string[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var q = parameters[i].ParameterType;
                p[i] = parameters[i].Name;
            }

            var info = new FunctionInfo();
            info.Name = method.Name;
            info.ReturnType = returnType;
            info.Parameters = p;
            return info;
        }

        /// <summary>
        /// If <see langword="true"/>, <see cref="ActionManager"/> will search
        /// all assemblies that have been defined using an <see
        /// cref="AssemblyDefinitionAsset"/> for commands and actions, when this
        /// project is loaded into a <see cref="DialogueRunner"/>. Otherwise,
        /// <see cref="assembliesToSearch"/> will be used.
        /// </summary>
        /// <seealso cref="assembliesToSearch"/>
        public static bool searchAllAssembliesForActions = true;

        private static Localization developmentLocalization;

        private static void CreateYarnInternalLocalizationAssets(YarnProject project,
            CompilationResult compilationResult)
        {
            // Will we need to create a default localization? This variable
            // will be set to false if any of the languages we've
            // configured in languagesToSourceAssets is the default
            // language.
            var shouldAddDefaultLocalization = true;
            if (project.LocaleCodeToCSVPath == null)
            {
                project.LocaleCodeToCSVPath = new Godot.Collections.Dictionary<string, string>();
            }

            if (shouldAddDefaultLocalization)
            {
                // We didn't add a localization for the default language.
                // Create one for it now.
                var stringTableEntries = GetStringTableEntries(project, compilationResult);

                developmentLocalization = project.baseLocalization ?? new Localization();
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

            if (!compilationResult.HasValue)
            {
                // We only get no value if we have no scripts to work with.
                // In this case, return an empty collection - there's no
                // error, but there's no content either.
                return new List<StringTableEntry>();
            }

            var errors =
                compilationResult.Value.Diagnostics.Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error);

            if (errors.Count() > 0)
            {
                GD.PrintErr("Can't generate a strings table from a Yarn Project that contains compile errors", null);
                return null;
            }

            return GetStringTableEntries(project, compilationResult.Value);
        }

        private static CompilationResult? CompileStringsOnly(YarnProject project)
        {
            var scriptPaths = project.SourceScripts.Where(s => s != null).Select(s => ProjectSettings.GlobalizePath(s));

            if (scriptPaths.Count() == 0)
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
            return result.StringTable.Select(x =>
            {
                var meta = new LineMetadataTableEntry();
                meta.ID = x.Key;
                meta.File = ProjectSettings.LocalizePath(x.Value.fileName);
                meta.Node = x.Value.nodeName;
                meta.LineNumber = x.Value.lineNumber.ToString();
                meta.Metadata = RemoveLineIDFromMetadata(x.Value.metadata).ToArray();
                return meta;
            }).Where(x => x.Metadata.Length > 0);
        }

        private static IEnumerable<StringTableEntry> GetStringTableEntries(YarnProject project,
            CompilationResult result)
        {
            return result.StringTable.Select(x =>
                {
                    var entry = new StringTableEntry();

                    entry.ID = x.Key;
                    entry.Language = project.defaultLanguage;
                    entry.Text = x.Value.text;
                    entry.File = ProjectSettings.LocalizePath(x.Value.fileName);
                    entry.Node = x.Value.nodeName;
                    entry.LineNumber = x.Value.lineNumber.ToString();
                    entry.Lock = YarnImporter.GetHashString(x.Value.text, 8);
                    entry.Comment = GenerateCommentWithLineMetadata(x.Value.metadata);
                    return entry;
                }
            );
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

            if (cleanedMetadata.Count() == 0)
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
        // TODO: search assemblies for annotated methods
        // private List<string> AssemblySearchList()
        // {
        //     // Get the list of assembly names we want to search for actions in.
        //     IEnumerable<AssemblyDefinitionAsset> assembliesToSearch = this.assembliesToSearch;
        //
        //     if (searchAllAssembliesForActions)
        //     {
        //         // We're searching all assemblies for actions. Find all assembly
        //         // definitions in the project, including in packages, and load
        //         // them.
        //         assembliesToSearch = AssetDatabase
        //             .FindAssets($"t:{nameof(AssemblyDefinitionAsset)}")
        //             .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
        //             .Distinct()
        //             .Select(path => AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path));
        //     }
        //
        //     // We won't include any assemblies whose names begin with any of
        //     // these prefixes
        //     var excludedPrefixes = new[]
        //     {
        //         "Unity",
        //     };
        //
        //     // Go through each assembly definition asset, figure out its
        //     // assembly name, and add it to the project's list of assembly names
        //     // to search.
        //     var validAssemblies = new List<string>();
        //     foreach (var reference in assembliesToSearch)
        //     {
        //         if (reference == null)
        //         {
        //             continue;
        //         }
        //         var data = new AssemblyDefinition();
        //         EditorJsonUtility.FromJsonOverwrite(reference.text, data);
        //
        //         if (excludedPrefixes.Any(prefix => data.name.StartsWith(prefix)))
        //         {
        //             continue;
        //         }
        //
        //         validAssemblies.Add(data.name);
        //     }
        //     return validAssemblies;
        // }

        private static List<FunctionInfo> predeterminedFunctions()
        {
            var functions = ActionManager.FunctionsInfo();

            List<FunctionInfo> f = new List<FunctionInfo>();
            foreach (var func in functions)
            {
                f.Add(CreateFunctionInfoFromMethodGroup(func));
            }

            return f;
        }

        // A data class used for deserialising the JSON AssemblyDefinitionAssets
        // into.
        private class AssemblyDefinition
        {
            public string name;
        }

        public static void AddLineTagsToFilesInYarnProject(YarnProject project)
        {
            // First, gather all existing line tags across ALL yarn
            // projects, so that we don't accidentally overwrite an
            // existing one. Do this by finding all yarn scripts in all
            // yarn projects, and get the string tags inside them.

            var allYarnFiles =
                // get all yarn projects across the entire project
                LoadAllYarnProjects()
                    // Get all of their source scripts, as a single sequence
                    .SelectMany(i => i.SourceScripts)
                    // Get the path for each asset
                    // remove any nulls, in case any are found
                    .Where(path => path != null);

#if YARNSPINNER_DEBUG
            var stopwatch = Stopwatch.StartNew();
#endif

            var library = new Library();
            ActionManager.ClearAllActions();
            ActionManager.RegisterFunctions(library);

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
                    return new string[]
                    {
                    };
                }

                return result.StringTable.Where(i => i.Value.isImplicitTag == false).Select(i => i.Key);
            }).ToList(); // immediately execute this query so we can determine timing information

#if YARNSPINNER_DEBUG
            stopwatch.Stop();
            GD.Print($"Checked {allYarnFiles.Count()} yarn files for line tags in {stopwatch.ElapsedMilliseconds}ms");
#endif

            var modifiedFiles = new List<string>();

            try
            {
                foreach (var script in project.SourceScripts)
                {
                    var assetPath = ProjectSettings.GlobalizePath(script);
                    var contents = File.ReadAllText(assetPath);

                    // Produce a version of this file that contains line
                    // tags added where they're needed.
                    var taggedVersion = Yarn.Compiler.Utility.AddTagsToLines(contents, allExistingTags);

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
            CleanUpMovedOrDeletedProjects();
            var allProjects = (Godot.Collections.Array) ProjectSettings.GetSetting(YARN_PROJECT_PATHS_SETTING_KEY);
            foreach (var path in allProjects)
            {
                projects.Add(ResourceLoader.Load<YarnProject>(path.ToString()));
            }

            return projects;
        }

        private static void CleanUpMovedOrDeletedProjects()
        {
            var projects = (Godot.Collections.Array) ProjectSettings.GetSetting(YARN_PROJECT_PATHS_SETTING_KEY);
            var removeProjects = new List<string>();
            foreach (var path in projects)
            {
                if (!File.Exists(ProjectSettings.GlobalizePath((string) path)))
                {
                    removeProjects.Add((string) path);
                }
            }

            var newProjects = new Godot.Collections.Array();
            foreach (var project in projects)
            {
                if (!removeProjects.Contains(project.AsString()))
                {
                    newProjects.Add(project);
                }
            }

            ProjectSettings.SetSetting(YARN_PROJECT_PATHS_SETTING_KEY, newProjects);
        }

        /// <summary>
        /// Add a yarn project to the list of known yarn projects, if it is not already in the list
        /// </summary>
        public static void AddProjectToList(YarnProject project)
        {
            CleanUpMovedOrDeletedProjects();
            var projects = (Godot.Collections.Array) ProjectSettings.GetSetting(YARN_PROJECT_PATHS_SETTING_KEY);
            if (project.ResourcePath != "" && !projects.Contains(project.ResourcePath))
            {
                projects.Add(project.ResourcePath);
            }

            ProjectSettings.SetSetting(YARN_PROJECT_PATHS_SETTING_KEY, projects);
        }
    }
}
#endif