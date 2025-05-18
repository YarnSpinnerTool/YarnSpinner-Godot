#if TOOLS
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.Serialization.Json;
using System.Text.Json;
using Godot;
using Yarn.Compiler;
using YarnSpinnerGodot;
using Array = Godot.Collections.Array;

namespace YarnSpinnerGodot;

/// <summary>
/// Main plugin script for the YarnSpinner-Godot plugin
/// </summary>
[Tool]
public partial class YarnSpinnerPlugin : EditorPlugin
{
#if GODOT4_2_OR_GREATER
    public static EditorInterface editorInterface => EditorInterface.Singleton;
#else
    public static EditorInterface editorInterface;
#endif

    private const string ToolsMenuName = "YarnSpinner";
    public const string VersionString = "0.3.0";

    private List<EditorInspectorPlugin> _inspectorPlugins =
        new();

    private List<EditorImportPlugin>
        _importPlugins = new();

    private struct ToolsMenuItem
    {
        public Action Handler;
        public string MenuName;
    };

    private Dictionary<int, ToolsMenuItem>
        _idToToolsMenuItem;

    /// <summary>
    /// This dictionary becomes null when rebuilding the C# code of the samples/
    /// your project. To work around this, if we detect the dictionary is null,
    /// we will re-initialize it.
    /// </summary>
    private Dictionary<int, ToolsMenuItem> IDToToolsMenuItem
    {
        get
        {
            _idToToolsMenuItem ??= new()
            {
                [0] =
                    new ToolsMenuItem()
                    {
                        MenuName = "Create Yarn Script",
                        Handler = CreateYarnScript,
                    },
                [1] =
                    new ToolsMenuItem()
                    {
                        MenuName = "Create Yarn Project",
                        Handler = CreateYarnProject,
                    },
                [2] =
                    new ToolsMenuItem()
                    {
                        MenuName = "Create Markup Palette",
                        Handler = CreateMarkupPalette,
                    }
                // TODO: actions source generation 
                //     [8] =
                //     new ToolsMenuItem()
                //     {
                //         MenuName = "Update Yarn Commands",
                //         Handler = ActionSourceCodeGenerator.GenerateYarnActionSourceCode,
                //     }
                // 
            };

            return _idToToolsMenuItem;
        }
    }

    private PopupMenu _popup;
    public const string YARN_PROJECT_EXTENSION = ".yarnproject";

    public override void _EnterTree()
    {
#if !GODOT4_2_OR_GREATER
        editorInterface = GetEditorInterface();
#endif
        // load script resources
        var yarnProjectScript =
            ResourceLoader.Load<CSharpScript>(
                "res://addons/YarnSpinner-Godot/Runtime/YarnProject.cs");
        var dialogueRunnerScript =
            ResourceLoader.Load<CSharpScript>(
                "res://addons/YarnSpinner-Godot/Runtime/DialogueRunner.cs");

        // load icons
        var miniYarnSpinnerIcon =
            ResourceLoader.Load<Texture2D>(
                "res://addons/YarnSpinner-Godot/Editor/Icons/mini_YarnSpinnerLogo.png");
        var miniYarnProjectIcon =
            ResourceLoader.Load<Texture2D>(
                "res://addons/YarnSpinner-Godot/Editor/Icons/Asset Icons/mini_YarnProject Icon.png");

        var scriptImportPlugin = new YarnImporter();
        _importPlugins.Add(scriptImportPlugin);
        var projectImportPlugin = new YarnProjectImporter();
        _importPlugins.Add(projectImportPlugin);
        var projectInspectorPlugin = new YarnProjectInspectorPlugin();
        _inspectorPlugins.Add(projectInspectorPlugin);


        foreach (var plugin in _inspectorPlugins)
        {
            AddInspectorPlugin(plugin);
        }

        foreach (var plugin in _importPlugins)
        {
            AddImportPlugin(plugin);
        }

        _popup = new PopupMenu();
        foreach (var entry in IDToToolsMenuItem)
        {
            _popup.AddItem(entry.Value.MenuName, entry.Key);
        }

        _popup.IdPressed += OnPopupIDPressed;
        AddToolSubmenuItem(ToolsMenuName, _popup);

        AddCustomType(nameof(DialogueRunner), "Node", dialogueRunnerScript,
            miniYarnSpinnerIcon);
        AddCustomType(nameof(YarnProject), "Resource", yarnProjectScript,
            miniYarnProjectIcon);
    }

    public override void _ExitTree()
    {
        foreach (var plugin in _importPlugins.Where(IsInstanceValid))
        {
            RemoveImportPlugin(plugin);
        }

        RemoveCustomType(nameof(DialogueRunner));
        RemoveCustomType(nameof(YarnProject));
        foreach (var plugin in _inspectorPlugins.Where(IsInstanceValid))
        {
            RemoveInspectorPlugin(plugin);
        }
    }

    /// <summary>
    /// Called when an item in the Tools > Yarn Spinner menu is clicked
    /// </summary>
    /// <param name="id"></param>
    public void OnPopupIDPressed(long id)
    {
        if (IDToToolsMenuItem.TryGetValue((int) id, out var menuItem))
        {
            menuItem.Handler();
        }
    }

    private void CreateYarnScript()
    {
        GD.Print("Opening 'create yarn script' menu");
        ShowCreateFilePopup("*.yarn ; Yarn Script",
            "Create a new Yarn Script",
            nameof(CreateYarnScriptDestinationSelected));
    }

    private static void CreateYarnScriptDestinationSelected(string destination)
    {
        GD.Print("Creating a yarn script at " + destination);
        YarnEditorUtility.CreateYarnScript(destination);
    }

    private void CreateMarkupPalette()
    {
        GD.Print("Opening 'create markup palette' menu");
        ShowCreateFilePopup("*.tres; Markup Palette",
            "Create a new Markup Palette",
            nameof(CreateMarkupPaletteDestinationSelected));
    }

    private static void CreateMarkupPaletteDestinationSelected(string destination)
    {
        GD.Print($"Creating a markup palette at {destination}");
        YarnEditorUtility.CreateMarkupPalette(destination);
    }

    private void CreateYarnProject()
    {
        GD.Print("Opening 'create yarn project' menu");
        ShowCreateFilePopup("*.yarnproject; Yarn Project",
            "Create a new Yarn Project",
            nameof(CreateYarnProjectDestinationSelected));
    }

    private void ShowCreateFilePopup(string filter, string windowTitle,
        string fileSelectedHandlerName)
    {
        if (!IsInstanceValid(editorInterface))
        {
            GD.PushError(
                $"Lost the reference to the Godot {nameof(EditorInterface)}. You might need to restart the editor or disable and enable this plugin.");
            return;
        }

        var dialog = new EditorFileDialog();
        dialog.AddFilter(filter);
        dialog.FileMode = EditorFileDialog.FileModeEnum.SaveFile;
        dialog.Title = windowTitle;
        dialog.Connect(FileDialog.SignalName.FileSelected,
            new Callable(this, fileSelectedHandlerName));
        editorInterface.GetBaseControl().AddChild(dialog);
        dialog.PopupCentered(new Vector2I(700, 500));
    }

    private static void CreateYarnProjectDestinationSelected(string destination)
    {
        GD.Print("Creating a yarn project at " + destination);
        YarnEditorUtility.CreateYarnProject(destination);
        editorInterface.GetResourceFilesystem().ScanSources();
    }
}
#endif