#nullable disable
#if TOOLS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Godot;
using Godot.Collections;
using Yarn;
using Yarn.Compiler;
using Yarn.Utility;

namespace YarnSpinnerGodot;

/// <summary>
/// A <see cref="EditorImportPlugin"/> for YarnSpinner JSON project files (.yarnproject files)
/// </summary>
public partial class YarnProjectImporter : EditorImportPlugin
{
    public override string[] _GetRecognizedExtensions() =>
        new[]
        {
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
        YarnProject godotProject = null;
        var fullSavePath = $"{savePath}.{_GetSaveExtension()}";
        try
        {
            godotProject = ResourceLoader.Load<YarnProject>(assetPath);
        }
        catch (Exception e)
        {
            GD.PushError(
                $"Error loading existing {nameof(YarnProject)}: {e.Message}\n{e.StackTrace}. Creating new resource.");
        }

        godotProject ??= new YarnProject();
        godotProject.JSONProjectPath = assetPath;
        godotProject.ImportPath = fullSavePath;
        godotProject.ResourceName = Path.GetFileName(assetPath);
        var saveErr = ResourceSaver.Save(godotProject, godotProject.ImportPath);
        if (saveErr != Error.Ok)
        {
            GD.PrintErr($"Error saving .yarnproject file import: {saveErr}");
        }

        YarnProjectEditorUtility.UpdateYarnProject(godotProject);

        return (int)Error.Ok;
    }

}
#endif