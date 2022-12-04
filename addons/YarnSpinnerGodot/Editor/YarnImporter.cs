#if TOOLS
using System.Security.Cryptography;
using System.Text;
using Godot;
using Godot.Collections;
using File = System.IO.File;
using Array = Godot.Collections.Array;
using Google.Protobuf;

namespace Yarn.GodotIntegration.Editor
{

    /// <summary>
    /// A <see cref="EditorImportPlugin"/> for Yarn scripts (.yarn files)
    /// </summary>
    public class YarnImporter : EditorImportPlugin
    {

        private YarnEditorUtility _editorUtility = new YarnEditorUtility();
        private YarnProjectUtility _projectUtility = new YarnProjectUtility();
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

        public override string GetSaveExtension() => "tres";
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

            if (extension == ".yarn")
            {
                ImportYarn(assetPath);
            }
            else if (extension == ".yarnc")
            {
                ImportCompiledYarn(assetPath);
            }
            var importedMarkerResource = new Resource();
            importedMarkerResource.ResourceName = System.IO.Path.GetFileNameWithoutExtension(ProjectSettings.GlobalizePath(assetPath));
            
            var saveErr = ResourceSaver.Save($"{savePath}.{GetSaveExtension()}", importedMarkerResource);
            if (saveErr != Error.Ok)
            {
                GD.PrintErr($"Error saving yarn file import: {saveErr.ToString()}");
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
        public static string GetHashString(string inputString, int limitCharacters = -1)
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
           var project = _projectUtility.GetDestinationProject(assetPath);
           if (project == null)
           {
               GD.Print($"The yarn file {assetPath} is not currently associated with a Yarn Project. Create a Yarn Project via Tools > YarnSpinner > Create Yarn Project and add this script to it to compile it.");
           }
           else
           {
               _projectUtility.UpdateYarnProject(project);
           }
        }

        /// <summary>
        /// Import a .yarnrc file
        /// </summary>
        /// <param name="assetPath"></param>
        private void ImportCompiledYarn(string assetPath)
        {

            var bytes = File.ReadAllBytes(assetPath);
            try
            {
                // Validate that this can be parsed as a Program protobuf
                var _ = Program.Parser.ParseFrom(bytes);
            }
            catch (InvalidProtocolBufferException)
            {
                GD.PushError("Invalid compiled yarn file. Please re-compile the source code.");
                return;
            }
            // Create a container for storing the bytes
            var programContainer = new Resource(); // "<pre-compiled Yarn script>"

            // Add this container to the imported asset; it will be what
            // the user interacts with in Godot
            GD.PrintErr("TODO: Need to save compiled yarn file asset.");
        }
    }
}
#endif