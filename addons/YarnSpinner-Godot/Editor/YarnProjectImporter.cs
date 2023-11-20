#if TOOLS
using Godot;
using Godot.Collections;

namespace YarnSpinnerGodot.Editor
{

    /// <summary>
    /// A <see cref="EditorImportPlugin"/> for YarnSpinner JSON project files (.yarnproject files)
    /// </summary>
    public partial class YarnProjectImporter : EditorImportPlugin
    {
        
        public override string[] _GetRecognizedExtensions() =>
           new[]{
                "yarnproject"
            };

        public override string _GetImporterName()
        {
            return "yarnproject";
        }

        public override string _GetVisibleName()
        {
            return "Yarn Project";
        }

        public override string _GetSaveExtension() => "tres";
        public override string _GetResourceType()
        {
            return "Resource";
        }
        public override int _GetPresetCount()
        {
            return 0;
        }

        public override float _GetPriority()
        {
            return 1.0f;
        }
        public override int _GetImportOrder()
        {
            return 0;
        }

        public override Array<Dictionary> _GetImportOptions(string path, int presetIndex)
        {
            return new Array<Dictionary>();
        }

        public override Error _Import(
            string assetPath,
            string savePath,
            Dictionary options,
            Array<string> platformVariants,
            Array<string> genFiles)
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            GD.Print($"Updating the Godot {nameof(YarnProject)} resource that is linked to {assetPath}");
            var importedMarkerResource = new Resource();
            importedMarkerResource.ResourceName = System.IO.Path.GetFileNameWithoutExtension(ProjectSettings.GlobalizePath(assetPath));
            var godotProject = YarnProjectEditorUtility.UpdateCompilerProject(assetPath);
            YarnProjectEditorUtility.UpdateLocalizationCSVs(godotProject);
            var saveErr = ResourceSaver.Save( importedMarkerResource, $"{savePath}.{_GetSaveExtension()}");
            if (saveErr != Error.Ok)
            {
                GD.PrintErr($"Error saving .yarnproject file import: {saveErr.ToString()}");
            }
            return (int)Error.Ok;
        }

    }
}
#endif