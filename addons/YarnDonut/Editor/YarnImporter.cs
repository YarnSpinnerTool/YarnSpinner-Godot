#if TOOLS
using System.Security.Cryptography;
using System.Text;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

namespace YarnDonut.Editor
{

    /// <summary>
    /// A <see cref="EditorImportPlugin"/> for Yarn scripts (.yarn files)
    /// </summary>
    public partial class YarnImporter : EditorImportPlugin
    {
        
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
            GD.Print($"Importing Yarn script {assetPath}");
            var project = YarnProjectEditorUtility.GetDestinationProject(assetPath);
            if (project == null)
            {
                GD.Print($"The yarn file {assetPath} is not currently associated with a Yarn Project." +
                    " Create a Yarn Project via Tools > YarnDonut > Create Yarn Project and make sure this" +
                    " script is in the same directory as the YarnProject or" +
                    " in a directory underneath that directory.");
            }
            else
            {
                YarnProjectEditorUtility.UpdateYarnProject(project);
            }
        }
    }
}
#endif