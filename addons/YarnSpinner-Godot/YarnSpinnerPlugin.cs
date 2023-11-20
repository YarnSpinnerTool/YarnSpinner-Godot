#if TOOLS
using System;
using System.Collections.Generic;
using Godot;
using Yarn.Compiler;
using YarnSpinnerGodot.Editor;
using Array = Godot.Collections.Array;

namespace YarnSpinnerGodot
{
    /// <summary>
    /// Main plugin script for the YarnSpinner-Godot plugin
    /// </summary>
    [Tool]
    public partial class YarnSpinnerPlugin : EditorPlugin
    {
        private static EditorInterface _editorInterface;
        private const string TOOLS_MENU_NAME = "YarnSpinner";
        private List<EditorInspectorPlugin> _inspectorPlugins = new List<EditorInspectorPlugin>();
        private List<EditorImportPlugin> _importPlugins = new List<EditorImportPlugin>();

        private struct ToolsMenuItem
        {
            public Action Handler;
            public string MenuName;
        };

        private readonly System.Collections.Generic.Dictionary<int, ToolsMenuItem> _idToToolsMenuItem = new()
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

        private PopupMenu _popup;
        public const string YARN_PROJECT_EXTENSION = ".yarnproject";

        public override void _EnterTree()
        {
            _editorInterface = GetEditorInterface();
            if (!ProjectSettings.HasSetting(YarnProjectEditorUtility.YARN_PROJECT_PATHS_SETTING_KEY))
            {
                ProjectSettings.SetSetting(YarnProjectEditorUtility.YARN_PROJECT_PATHS_SETTING_KEY, new Array());
            }

            ProjectSettings.SetInitialValue(YarnProjectEditorUtility.YARN_PROJECT_PATHS_SETTING_KEY, new Array());
            // load script resources
            var yarnProjectScript =
                ResourceLoader.Load<CSharpScript>("res://addons/YarnSpinner-Godot/Runtime/YarnProject.cs");
            var dialogueRunnerScript =
                ResourceLoader.Load<CSharpScript>("res://addons/YarnSpinner-Godot/Runtime/DialogueRunner.cs");

            // load icons
            var miniYarnSpinnerIcon =
                ResourceLoader.Load<Texture2D>("res://addons/YarnSpinner-Godot/Editor/Icons/mini_YarnSpinnerLogo.png");
            var miniYarnProjectIcon =
                ResourceLoader.Load<Texture2D>(
                    "res://addons/YarnSpinner-Godot/Editor/Icons/Asset Icons/mini_YarnProject Icon.png");

            var scriptImportPlugin = new YarnImporter();
            _importPlugins.Add(scriptImportPlugin);
            var projectImportPlugin = new YarnProjectImporter();
            _importPlugins.Add(projectImportPlugin);
            var projectInspectorPlugin = new YarnProjectInspectorPlugin();
            projectInspectorPlugin.editorInterface = GetEditorInterface();
            _inspectorPlugins.Add(projectInspectorPlugin);
            var paletteInspectorPlugin = new YarnMarkupPaletteInspectorPlugin();
            _inspectorPlugins.Add(paletteInspectorPlugin);

            foreach (var plugin in _inspectorPlugins)
            {
                AddInspectorPlugin(plugin);
            }

            foreach (var plugin in _importPlugins)
            {
                AddImportPlugin(plugin);
            }

            _popup = new PopupMenu();
            foreach (var entry in _idToToolsMenuItem)
            {
                _popup.AddItem(entry.Value.MenuName, entry.Key);
            }

            _popup.IdPressed += OnPopupIDPressed;
            AddToolSubmenuItem(TOOLS_MENU_NAME, _popup);

            AddCustomType(nameof(DialogueRunner), "Node", dialogueRunnerScript, miniYarnSpinnerIcon);
            AddCustomType(nameof(YarnProject), "Resource", yarnProjectScript, miniYarnProjectIcon);
        }

        public override void _ExitTree()
        {
            foreach (var plugin in _importPlugins)
            {
                RemoveImportPlugin(plugin);
            }

            RemoveCustomType(nameof(DialogueRunner));
            RemoveCustomType(nameof(YarnProject));
            foreach (var plugin in _inspectorPlugins)
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
            if (_idToToolsMenuItem.TryGetValue((int) id, out var menuItem))
            {
                menuItem.Handler();
            }
        }

        private static void CreateYarnScript()
        {
            GD.Print("Opening 'create yarn script' menu");
            ShowCreateFilePopup("*.yarn ; Yarn Script",
                "Create a new Yarn Script", CreateYarnScriptDestinationSelected);
        }

        private static void CreateYarnScriptDestinationSelected(string destination)
        {
            GD.Print("Creating a yarn script at " + destination);
            YarnEditorUtility.CreateYarnScript(destination);
        }

        private static void CreateMarkupPalette()
        {
            GD.Print("Opening 'create markup palette' menu");
            ShowCreateFilePopup("*.tres; Markup Palette",
                "Create a new Markup Palette", CreateMarkupPaletteDestinationSelected);
        }

        private static void CreateMarkupPaletteDestinationSelected(string destination)
        {
            GD.Print("Creating a markup palette at " + destination);
            YarnEditorUtility.CreateMarkupPalette(destination);
        }

        private static void CreateYarnProject()
        {
            GD.Print("Opening 'create yarn project' menu");
            ShowCreateFilePopup("*.tres; Yarn Project", "Create a new Yarn Project",
                CreateYarnProjectDestinationSelected);
        }

        private static void ShowCreateFilePopup(string filter, string windowTitle, Action<string> fileSelectedHandler)
        {
            if (!IsInstanceValid(_editorInterface))
            {
                GD.PushError(
                    $"Lost the reference to the Godot {nameof(EditorInterface)}. You might need to restart the editor or disable and enable this plugin.");
                return;
            }

            var dialog = new EditorFileDialog();
            dialog.AddFilter(filter);
            dialog.FileMode = EditorFileDialog.FileModeEnum.SaveFile;
            dialog.Title = windowTitle;
            dialog.FileSelected += (fileName) => { fileSelectedHandler(fileName); };
            _editorInterface.GetBaseControl().AddChild(dialog);
            dialog.PopupCentered(new Vector2I(700, 500));
        }

        private static void CreateYarnProjectDestinationSelected(string destination)
        {
            GD.Print("Creating a yarn project at " + destination);
            YarnEditorUtility.CreateYarnProject(destination);
        }
    }
}
#endif