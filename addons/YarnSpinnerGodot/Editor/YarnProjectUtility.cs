#if TOOLS
#define YARNSPINNER_DEBUG // todo remove
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Godot;
using Godot.Collections;
using Google.Protobuf;
using Newtonsoft.Json;
using Yarn;
using Yarn.Compiler;
using Yarn.GodotIntegration;
using Yarn.GodotIntegration.Editor;
using Array = Godot.Collections.Array;
using File = System.IO.File;
using Path = System.IO.Path;

namespace Yarn.GodotIntegration.Editor
{
    [Tool]
    public partial class YarnProjectUtility
    {
        public List<string> parseErrorMessages = new List<string>();

        public const string YarnProjectPathsSettingKey = "YarnSpinner-Godot/YarnProjectPaths";

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public YarnProject GetDestinationProject(string assetPath)
        {
            var destinationProjectPath = LoadAllYarnProjects()
                .FirstOrDefault(proj =>
                {
                    // Does this project depend on this script? If so,
                    // then this is our destination asset.
                    var importerDependsOnThisAsset = proj.SourceScripts.ToList()
                        .ConvertAll(s => s.ResourcePath).Contains(assetPath);
                    return importerDependsOnThisAsset;
                })?.ResourcePath;

            if (destinationProjectPath == null)
            {
                return null;
            }

            return ResourceLoader.Load<YarnProject>(destinationProjectPath);
        }

        /// <summary>
        /// Re-compile scripts in a yarn project, add all associated data to the project,
        /// and save it back to disk in the same .tres file.
        /// </summary>
        /// <param name="project"></param>
        public void UpdateYarnProject(YarnProject project)
        {
            if (string.IsNullOrEmpty(project.ResourcePath)) return;
            CompileAllScripts(project);
            SaveYarnProject(project);
        }

        public void SaveYarnProject(YarnProject project)
        {
            var saveErr = ResourceSaver.Save(project, project.ResourcePath, ResourceSaver.SaverFlags.ReplaceSubresourcePaths);
            if (saveErr != Error.Ok)
            {
                GD.PushError($"Error updating YarnProject {project.ResourceName} to {project.ResourcePath}: {saveErr}");
            }
            else
            {
                GD.Print($"Wrote updated YarnProject {project.ResourceName} to {project.ResourcePath}");
            }
        }
        public void CompileAllScripts(YarnProject project)
        {
            var assetPath = project.ResourcePath;
            GD.Print($"Compiling all scripts in {assetPath}");

            project.ResourceName = Path.GetFileNameWithoutExtension(assetPath);
            if (project.SourceScripts == null)
            {
                return;
            }

            var library = new Library();
            ActionManager.ClearAllActions();
            ActionManager.AddActionsFromAssemblies();
            ActionManager.RegisterFunctions(library);
            // localDeclarationsCompileJob.Library = library;
            project.ListOfFunctions = predeterminedFunctions().ToArray();
            IEnumerable<Diagnostic> errors;
            project.ProjectErrors = "[]";

            // We now now compile!
            var scriptAbsolutePaths = project.SourceScripts.ToList().Where(s => s != null)
                .Select(scriptResource => ProjectSettings.GlobalizePath(scriptResource.ResourcePath)).ToList();
            // Store the compiled program
            byte[] compiledBytes = null;
            CompilationResult? compilationResult = new CompilationResult?();
            if (scriptAbsolutePaths.Count > 0)
            {
                var job = CompilationJob.CreateFromFiles(scriptAbsolutePaths);
                // job.VariableDeclarations = localDeclarations;

                job.Library = library;
                compilationResult = Yarn.Compiler.Compiler.Compile(job);

                errors = compilationResult.Value.Diagnostics.Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error);

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
                            FileName = e.FileName
                        });
                    project.ProjectErrors = JsonConvert.SerializeObject(projectErrors);
                    return;
                }

                if (compilationResult.Value.Program == null)
                {
                    GD.PushError("public error: Failed to compile: resulting program was null, but compiler did not report errors.");
                    return;
                }

                // Store _all_ declarations - both the ones in this
                // .yarnproject file, and the ones inside the .yarn files.

                // While we're here, filter out any declarations that begin with our
                // Yarn public prefix. These are synthesized variables that are
                // generated as a result of the compilation, and are not declared by
                // the user.
                project.SerializedDeclarations = new List<Declaration>() //localDeclarations
                    .Concat(compilationResult.Value.Declarations)
                    .Where(decl => !decl.Name.StartsWith("$Yarn.Internal."))
                    .Where(decl => !(decl.Type is FunctionType))
                    .Select(decl =>
                    {
                        var serialized = new SerializedDeclaration();
                        serialized.SetDeclaration(decl);
                        serialized.ResourceName = serialized.name;
                        return serialized;
                    }).ToArray();

                // Clear error messages from all scripts - they've all passed
                // compilation
                project.ProjectErrors = "[]";

                CreateYarnInternalLocalizationAssets(project, compilationResult.Value);
                project.localizationType = LocalizationType.YarnInternal;

                using (var memoryStream = new MemoryStream())
                using (var outputStream = new CodedOutputStream(memoryStream))
                {
                    // Serialize the compiled program to memory
                    compilationResult.Value.Program.WriteTo(outputStream);
                    outputStream.Flush();

                    compiledBytes = memoryStream.ToArray();
                }
            }
            project.CompiledYarnProgramBase64 = compiledBytes == null ? "" : Convert.ToBase64String(compiledBytes);
            ResourceSaver.Save(project,project.ResourcePath,  ResourceSaver.SaverFlags.ReplaceSubresourcePaths);
        }

        private static void LogDiagnostic(Diagnostic diagnostic)
        {
            var messagePrefix = string.IsNullOrEmpty(diagnostic.FileName) ? string.Empty : $"{diagnostic.FileName}: {diagnostic.Range.Start}:{diagnostic.Range.Start.Character} ";

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
                compilationResult = Compiler.Compiler.Compile(compilationJob);
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
        public FunctionInfo CreateFunctionInfoFromMethodGroup(System.Reflection.MethodInfo method)
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
            info.ResourceName = info.Name;
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
        public bool searchAllAssembliesForActions = true;
        private Localization developmentLocalization;
        private YarnEditorUtility _editorUtility = new YarnEditorUtility();

        private void CreateYarnInternalLocalizationAssets(YarnProject project, CompilationResult compilationResult)
        {
            // Will we need to create a default localization? This variable
            // will be set to false if any of the languages we've
            // configured in languagesToSourceAssets is the default
            // language.
            var shouldAddDefaultLocalization = true;

            foreach (var pair in project.languagesToSourceAssets)
            {
                // Don't create a localization if the language ID was not
                // provided
                if (string.IsNullOrEmpty(pair.languageID))
                {
                    GD.PrintErr($"Not creating a localization for {project.ResourceName} because the language ID wasn't provided. Add the language ID to the localization in the Yarn Project's inspector.");
                    continue;
                }

                IEnumerable<StringTableEntry> stringTable;

                // Where do we get our strings from? If it's the default
                // language, we'll pull it from the scripts. If it's from
                // any other source, we'll pull it from the CSVs.
                if (pair.languageID == project.defaultLanguage)
                {
                    // We'll use the program-supplied string table.
                    stringTable = GenerateStringsTable(project);

                    // We don't need to add a default localization.
                    shouldAddDefaultLocalization = false;
                }
                else
                {
                    try
                    {
                        if (pair.stringsFile == null)
                        {
                            // We can't create this localization because we
                            // don't have any data for it.
                            GD.PrintErr($"Not creating a localization for {pair.languageID} in the Yarn Project {project.ResourceName} because a text asset containing the strings wasn't found. Add a .csv file containing the translated lines to the Yarn Project's inspector.");
                            continue;
                        }

                        var csvText = System.IO.File.ReadAllText(ProjectSettings.GlobalizePath(pair.stringsFile));

                        stringTable = StringTableEntry.ParseFromCSV(csvText);
                    }
                    catch (ArgumentException e)
                    {
                        GD.PrintErr($"Not creating a localization for {pair.languageID} in the Yarn Project {project.ResourceName} because an error was encountered during text parsing: {e}");
                        continue;
                    }
                }

                var newLocalization = new Localization();
                newLocalization.LocaleCode = pair.languageID;

                // Add these new lines to the localisation's asset
                foreach (var entry in stringTable)
                {
                    newLocalization.AddLocalisedStringToAsset(entry.ID, entry.Text);
                }

                newLocalization.ResourceName = pair.languageID;
//             TODO: localizable resources
//             if (pair.assetsFolder != null)
//             {
//                 var assetsFolderPath = AssetDatabase.GetAssetPath(pair.assetsFolder);
//
//                 if (assetsFolderPath == null)
//                 {
//                     // This was somehow not a valid reference?
//                     GD.PrintErr($"Can't find assets for localization {pair.languageID} in {project.name} because a path for the provided assets folder couldn't be found.");
//                 }
//                 else
//                 {
//                     newLocalization.ContainsLocalizedAssets = true;
//
//                     // We need to find the assets used by this
//                     // localization now, and assign them to the
//                     // Localization object.
// #if YARNSPINNER_DEBUG
//                             // This can take some time, so we'll measure
//                             // how long it takes.
//                             var stopwatch = System.Diagnostics.Stopwatch.StartNew();
// #endif
//
//                     // Get the line IDs.
//                     IEnumerable<string> lineIDs = stringTable.Select(s => s.ID);
//
//                     // Map each line ID to its asset path.
//                     var stringIDsToAssetPaths = YarnProjectUtility.FindAssetPathsForLineIDs(lineIDs, assetsFolderPath);
//
//                     // Load the asset, so we can assign the reference.
//                     var assetPaths = stringIDsToAssetPaths
//                         .Select(a => new KeyValuePair<string, Resource>(a.Key, ResourceLoader.Load(a.Value)));
//
//                     newLocalization.AddLocalizedObjects(assetPaths);
//
// #if YARNSPINNER_DEBUG
//                             stopwatch.Stop();
//                             GD.Print($"Imported {stringIDsToAssetPaths.Count()} assets for {project.ResourceName} \"{pair.languageID}\" in {stopwatch.ElapsedMilliseconds}ms");
// #endif
//
//                 }
//             }

                if (pair.languageID == project.defaultLanguage)
                {
                    // If this is our default language, set it as such
                    project.baseLocalization = newLocalization;

                    // Since this is the default language, also populate the line metadata.
                    project.lineMetadata = new LineMetadata();
                    project.lineMetadata.AddMetadata(LineMetadataTableEntriesFromCompilationResult(compilationResult));
                }
                foreach (var existingLocalization in project.localizations)
                {
                    if (existingLocalization.LocaleCode.Equals(newLocalization.LocaleCode))
                    {
                        newLocalization.stringsFile = existingLocalization.stringsFile;
                        if (!existingLocalization.ResourcePath.Contains(project.ResourcePath)
                            && !existingLocalization.ResourcePath.Contains("::"))
                        {
                            // only try to save it to disk if it's a standalone file and a sub resource
                            var saveErr = ResourceSaver.Save(newLocalization, existingLocalization.ResourcePath);
                            if (saveErr != Error.Ok)
                            {
                                GD.PushError($"Error saving localization {newLocalization.LocaleCode} to {existingLocalization.ResourcePath}");
                            }
                        }

                    }
                }
            }

            if (shouldAddDefaultLocalization)
            {
                // We didn't add a localization for the default language.
                // Create one for it now.
                var stringTableEntries = GetStringTableEntries(project, compilationResult);

                developmentLocalization = new Localization();
                developmentLocalization.ResourceName = $"Default ({project.defaultLanguage})";
                developmentLocalization.LocaleCode = project.defaultLanguage;


                // Add these new lines to the development localisation's asset
                foreach (var entry in stringTableEntries)
                {
                    developmentLocalization.AddLocalisedStringToAsset(entry.ID, entry.Text);
                }

                project.baseLocalization = developmentLocalization;
                project.localizations = project.localizations.Concat(new List<Localization>{project.baseLocalization}).ToArray();

                // Since this is the default language, also populate the line metadata.
                project.lineMetadata = new LineMetadata();
                project.lineMetadata.AddMetadata(LineMetadataTableEntriesFromCompilationResult(compilationResult));
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
        public IEnumerable<StringTableEntry> GenerateStringsTable(YarnProject project)
        {
            CompilationResult? compilationResult = CompileStringsOnly(project);

            if (!compilationResult.HasValue)
            {
                // We only get no value if we have no scripts to work with.
                // In this case, return an empty collection - there's no
                // error, but there's no content either.
                return new List<StringTableEntry>();
            }

            var errors = compilationResult.Value.Diagnostics.Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error);

            if (errors.Count() > 0)
            {
                GD.PrintErr($"Can't generate a strings table from a Yarn Project that contains compile errors", null);
                return null;
            }

            return GetStringTableEntries(project, compilationResult.Value);
        }

        private CompilationResult? CompileStringsOnly(YarnProject project)
        {
            var scriptPaths = project.SourceScripts.Where(s => s != null).Select(s => ProjectSettings.GlobalizePath(s.ResourcePath));

            if (scriptPaths.Count() == 0)
            {
                // We have no scripts to work with.
                return null;
            }

            // We now now compile!
            var job = CompilationJob.CreateFromFiles(scriptPaths);
            job.CompilationType = CompilationJob.Type.StringsOnly;

            return Compiler.Compiler.Compile(job);
        }

        private IEnumerable<LineMetadataTableEntry> LineMetadataTableEntriesFromCompilationResult(CompilationResult result)
        {
            return result.StringTable.Select(x => new LineMetadataTableEntry
            {
                ID = x.Key,
                File = x.Value.fileName,
                Node = x.Value.nodeName,
                LineNumber = x.Value.lineNumber.ToString(),
                Metadata = RemoveLineIDFromMetadata(x.Value.metadata).ToArray(),
            }).Where(x => x.Metadata.Length > 0);
        }

        private IEnumerable<StringTableEntry> GetStringTableEntries(YarnProject project, CompilationResult result)
        {

            return result.StringTable.Select(x => new StringTableEntry
            {
                ID = x.Key,
                Language = project.defaultLanguage,
                Text = x.Value.text,
                File = x.Value.fileName,
                Node = x.Value.nodeName,
                LineNumber = x.Value.lineNumber.ToString(),
                Lock = YarnImporter.GetHashString(x.Value.text, 8),
                Comment = GenerateCommentWithLineMetadata(x.Value.metadata),
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
        private string GenerateCommentWithLineMetadata(string[] metadata)
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
        private IEnumerable<string> RemoveLineIDFromMetadata(string[] metadata)
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

        private List<FunctionInfo> predeterminedFunctions()
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
        [Serializable]
        private class AssemblyDefinition
        {
            public string name;
        }
        public void AddLineTagsToFilesInYarnProject(YarnProject project)
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
                    .Select(sourceAsset => sourceAsset.ResourcePath)
                    // remove any nulls, in case any are found
                    .Where(path => path != null);

#if YARNSPINNER_DEBUG
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
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
                var compilationJob = Yarn.Compiler.CompilationJob.CreateFromFiles(ProjectSettings.GlobalizePath(path));
                compilationJob.CompilationType = Yarn.Compiler.CompilationJob.Type.StringsOnly;
                compilationJob.Library = library;

                var result = Yarn.Compiler.Compiler.Compile(compilationJob);

                bool containsErrors = result.Diagnostics
                    .Any(d => d.Severity == Compiler.Diagnostic.DiagnosticSeverity.Error);

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
                    var assetPath = ProjectSettings.GlobalizePath(script.ResourcePath);
                    var contents = System.IO.File.ReadAllText(assetPath);

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

                        System.IO.File.WriteAllText(assetPath, taggedVersion, Encoding.UTF8);
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
        public List<YarnProject> LoadAllYarnProjects()
        {
            var projects = new List<YarnProject>();
            CleanUpMovedOrDeletedProjects();
            var allProjects = (string[])ProjectSettings.GetSetting(YarnProjectPathsSettingKey);
            foreach (var path in allProjects)
            {
                projects.Add(ResourceLoader.Load<YarnProject>(path));
            }
            return projects;
        }

        private void CleanUpMovedOrDeletedProjects()
        {
            var projects = ((string[])ProjectSettings.GetSetting(YarnProjectPathsSettingKey));
            var removeProjects = new List<string>();
            foreach (var path in projects)
            {
                if (!File.Exists(ProjectSettings.GlobalizePath(path)))
                {
                    removeProjects.Add(path);
                }
            }
            var newProjects = projects.ToList().Where(p => !removeProjects.Contains(p)).ToArray();
            ProjectSettings.SetSetting(YarnProjectPathsSettingKey, Variant.From(newProjects));
        }
        /// <summary>
        /// Add a yarn project to the list of known yarn projects, if it is not already in the list
        /// </summary>
        public void AddProjectToList(YarnProject project)
        {
            CleanUpMovedOrDeletedProjects();
            var projects = ((string[])ProjectSettings.GetSetting(YarnProjectPathsSettingKey)).ToList();
            if (project.ResourcePath != "" && !projects.Contains(project.ResourcePath))
            {
                projects.Add(project.ResourcePath);
            }
            ProjectSettings.SetSetting(YarnProjectPathsSettingKey, Variant.From(projects));
        }
    }

// A simple class lets us use a delegate as an IEqualityComparer from
// https://stackoverflow.com/a/4607559
    public static class Compare
    {
        public static IEqualityComparer<T> By<T>(Func<T, T, bool> comparison)
        {
            return new DelegateComparer<T>(comparison);
        }

        private class DelegateComparer<T> : EqualityComparer<T>
        {
            private readonly Func<T, T, bool> comparison;

            public DelegateComparer(Func<T, T, bool> identitySelector)
            {
                comparison = identitySelector;
            }

            public override bool Equals(T x, T y)
            {
                return comparison(x, y);
            }

            public override int GetHashCode(T obj)
            {
                // Force LINQ to never refer to the hash of an object by
                // returning a constant for all values. This is inefficient
                // because LINQ can't use an public comparator, but we're
                // already looking to use a delegate to do a more
                // fine-grained test anyway, so we want to ensure that it's
                // called.
                return 0;
            }
        }
    }

}
#endif