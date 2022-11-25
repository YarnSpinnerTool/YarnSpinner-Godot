using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Yarn.Compiler;
using System.Security.Cryptography;
using System.Text;
using Godot;
using Godot.Collections;
using Yarn.GodotIntegation;
using YarnSpinnerGodot.addons.YarnSpinnerGodot;
using File = System.IO.File;
using Array = Godot.Collections.Array;
namespace Yarn.GodotIntegration.Editor
{

    /// <summary>
    /// A <see cref="ScriptedImporter"/> for Yarn assets. The actual asset
    /// used and referenced at runtime and in the editor will be a <see
    /// cref="YarnScript"/>, which this class wraps around creating the
    /// asset's corresponding meta file.
    /// </summary>
    public class YarnImporter : EditorImportPlugin, IYarnErrorSource
    {

        /// <summary>
        /// Contains the text of the most recent parser error message.
        /// </summary>
        public List<string> parseErrorMessages = new List<string>();

        IList<string> IYarnErrorSource.CompileErrors => parseErrorMessages;

        bool IYarnErrorSource.Destroyed => this == null;

        private YarnEditorUtility _editorUtility = new YarnEditorUtility();
        public override Array GetRecognizedExtensions() =>
            new Array(new[]
            {
                "yarn"
            });
        
        public override string GetImporterName()
        {
            return "yarnscript";
        }

        public override string GetVisibleName()
        {
            return "Yarn Script";
        }
        public YarnProject GetDestinationProject(string assetPath)
        {

            var myAssetPath = assetPath;
            var destinationProjectPath = _editorUtility.GetAllAssetsOf<YarnProject>("t:YarnProject")
                .FirstOrDefault(importer =>
                {
                    // Does this importer depend on this asset? If so,
                    // then this is our destination asset.
                    string[] dependencies = ResourceLoader.GetDependencies(assetPath);
                    var importerDependsOnThisAsset = dependencies.Contains(myAssetPath);

                    return importerDependsOnThisAsset;
                })?.ResourcePath;

            if (destinationProjectPath == null)
            {
                return null;
            }

            return ResourceLoader.Load<YarnProject>(destinationProjectPath);
        }

        public override string GetSaveExtension() => "yarn";
        public override string GetResourceType()
        {
            return "Resource";
        }
        public override int GetPresetCount()
        {
            return 0;
        }

        public override float GetPriority()
        {
            return 1.0f;
        }
        public override int GetImportOrder()
        {
            return 0;
        }

        public override Array GetImportOptions(int preset)
        {
            return new Array();
        }

        public override int Import(string assetPath, string savePath, Dictionary options,
            Array platformVariants, Array genFiles)
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            var extension = System.IO.Path.GetExtension(assetPath);

            var compiledFile = new CompiledYarnFile();
            // Clear the 'strings available' flags in case this import
            // fails
            compiledFile.LastImportHadAnyStrings = false;
            compiledFile.LastImportHadImplicitStringIDs = false;

            parseErrorMessages.Clear();

            compiledFile.isSuccessfullyParsed = false;

            if (extension == ".yarn")
            {
                ImportYarn(assetPath);
            }
            else if (extension == ".yarnc")
            {
                ImportCompiledYarn(assetPath);
            }

            return (int)Error.Ok;
        }

        /// <summary>
        /// Returns a byte array containing a SHA-256 hash of <paramref
        /// name="inputString"/>.
        /// </summary>
        /// <param name="inputString">The string to produce a hash value
        /// for.</param>
        /// <returns>The hash of <paramref name="inputString"/>.</returns>
        private static byte[] GetHash(string inputString)
        {
            using (HashAlgorithm algorithm = SHA256.Create())
            {
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
            }
        }

        /// <summary>
        /// Returns a string containing the hexadecimal representation of a
        /// SHA-256 hash of <paramref name="inputString"/>.
        /// </summary>
        /// <param name="inputString">The string to produce a hash
        /// for.</param>
        /// <param name="limitCharacters">The length of the string to
        /// return. The returned string will be at most <paramref
        /// name="limitCharacters"/> characters long. If this is set to -1,
        /// the entire string will be returned.</param>
        /// <returns>A string version of the hash.</returns>
        internal static string GetHashString(string inputString, int limitCharacters = -1)
        {
            var sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
            {
                sb.Append(b.ToString("x2"));
            }

            if (limitCharacters == -1)
            {
                // Return the entire string
                return sb.ToString();
            }
            else
            {
                // Return a substring (or the entire string, if
                // limitCharacters is longer than the string)
                return sb.ToString(0, Mathf.Min(sb.Length, limitCharacters));
            }
        }

        private void ImportYarn(string assetPath)
        {
            var sourceText = System.IO.File.ReadAllText(assetPath);
            var absoluteScriptPath = ProjectSettings.GlobalizePath(assetPath);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(absoluteScriptPath);

            var compiledYarnFileResource = new CompiledYarnFile();

            // Add this container to the imported asset; it will be what
            // the user interacts with in Unity
            // TODO: sub resources
            // ctx.AddObjectToAsset("Program", text, YarnEditorUtility.GetYarnDocumentIconTexture());
            // ctx.SetMainObject(text);

            Yarn.Program compiledProgram = null;
            IDictionary<string, Yarn.Compiler.StringInfo> stringTable = null;

            parseErrorMessages.Clear();

            // Compile the source code into a compiled Yarn program (or
            // generate a parse error)
            sourceText = File.ReadAllText(absoluteScriptPath);
            var compilationJob = CompilationJob.CreateFromString(fileName, sourceText, null);
            compilationJob.CompilationType = CompilationJob.Type.StringsOnly;

            var result = Yarn.Compiler.Compiler.Compile(compilationJob);

            IEnumerable<Diagnostic> errors = result.Diagnostics.Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error);

            if (errors.Count() > 0)
            {
                compiledYarnFileResource.isSuccessfullyParsed = false;

                parseErrorMessages.AddRange(errors.Select(e =>
                {
                    string message = $"{assetPath}: {e}";
                    GD.PushError($"Error importing {message}");
                    return message;
                }));
            }
            else
            {
                compiledYarnFileResource.isSuccessfullyParsed = true;
                compiledYarnFileResource.LastImportHadImplicitStringIDs = result.ContainsImplicitStringTags;
                compiledYarnFileResource.LastImportHadAnyStrings = result.StringTable.Count > 0;

                stringTable = result.StringTable;
                compiledProgram = result.Program;
            }
        }

        private void ImportCompiledYarn(string assetPath)
        {

            var bytes = File.ReadAllBytes(assetPath);

            try
            {
                // Validate that this can be parsed as a Program protobuf
                var _ = Program.Parser.ParseFrom(bytes);
            }
            catch (Google.Protobuf.InvalidProtocolBufferException)
            {
                GD.PushError("Invalid compiled yarn file. Please re-compile the source code.");
                return;
            }

            //compiledYarnFileResource.isSuccessfullyParsed = true;

            // Create a container for storing the bytes
            var programContainer = new Resource(); // "<pre-compiled Yarn script>"

            // Add this container to the imported asset; it will be what
            // the user interacts with in Godot
            GD.PrintErr("TODO: Need to save compiled yarn file asset.");
        }
    }
}