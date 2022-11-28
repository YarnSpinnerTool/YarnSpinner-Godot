using Godot;
using Yarn.GodotIntegration.Editor;
namespace YarnSpinnerGodot.addons.YarnSpinnerGodot
{
    /// <summary>
    /// Main plugin script for the YarnSpinner-Godot plugin
    /// </summary>
    [Tool]
    public class YarnSpinnerGodotMainPlugin : EditorPlugin
    {
        private YarnImporter script_import_plugin;
        private YarnProjectInspectorPlugin project_inspector_plugin;
        private YarnEditorUtility editor_utility;
        private PopupMenu popup;
        private const string popupName = "YarnSpinner";

        private const int createYarnScriptID = 1;
        private const int createYarnProjectID = 2;

        public override void _EnterTree()
        {
            // load script resources
            var script_importer_script = ResourceLoader.Load<CSharpScript>("res://addons/YarnSpinnerGodot/editor/YarnImporter.cs");
            var editor_utility_script = ResourceLoader.Load<CSharpScript>("res://addons/YarnSpinnerGodot/Editor/YarnEditorUtility.cs");
            var project_inspector_script = ResourceLoader.Load<CSharpScript>("res://addons/YarnSpinnerGodot/Editor/YarnProjectInspectorPlugin.cs");
            var localizationScript = ResourceLoader.Load<CSharpScript>("res://addons/YarnSpinnerGodot/Runtime/Localization.cs");
            var dialogueRunnerScript = ResourceLoader.Load<CSharpScript>("res://addons/YarnSpinnerGodot/Runtime/DialogueRunner.cs");

            // load icons
            var miniLocalizationIcon = ResourceLoader.Load<Texture>("res://addons/YarnSpinnerGodot/Editor/Icons/Asset Icons/mini_Localization Icon.png");
            var miniYarnSpinnerIcon = ResourceLoader.Load<Texture>("res://addons/YarnSpinnerGodot/Editor/Icons/mini_YarnSpinnerLogo.png");


            script_import_plugin = (YarnImporter)script_importer_script.New();
            editor_utility = (YarnEditorUtility)editor_utility_script.New();
            project_inspector_plugin = (YarnProjectInspectorPlugin)project_inspector_script.New();
            AddInspectorPlugin(project_inspector_plugin);
            AddImportPlugin(script_import_plugin);
            popup = new PopupMenu();

            popup.AddItem("Create Yarn Script", createYarnScriptID);
            popup.AddItem("Create Yarn Project", createYarnProjectID);
            popup.Connect("id_pressed", this, "on_popup_id_pressed");
            AddToolSubmenuItem(popupName, popup);
            AddCustomType("DialogueRunner", "Node",
                dialogueRunnerScript,
                miniYarnSpinnerIcon);
            AddCustomType("Localization", "Resource",
                localizationScript,
                miniLocalizationIcon);
        }
        public override void _ExitTree()
        {
            RemoveImportPlugin(script_import_plugin);
            RemoveCustomType("DialogueRunner");
            RemoveCustomType("Localization");
            RemoveInspectorPlugin(project_inspector_plugin);
            RemoveToolMenuItem(popupName);
        }

        /// <summary>
        /// Called when an item in the Tools > Yarn Spinner menu is clicked
        /// </summary>
        /// <param name="id"></param>
        public void OnPopupIDPressed(int id)
        {
            if (id == createYarnScriptID)
            {
                CreateYarnScript();
            }
            if (id == createYarnProjectID)
            {
                CreateYarnProject();
            }
        }
        private void CreateYarnScript()
        {
            GD.Print("Opening 'create yarn script' menu");
            var dialog = new EditorFileDialog();
            dialog.AddFilter("*.yarn ; Yarn Script");
            dialog.Mode = EditorFileDialog.ModeEnum.SaveFile;
            dialog.WindowTitle = "Create a new Yarn Script";
            dialog.Connect("file_selected", this, nameof(CreateYarnScriptDestinationSelected));
            GetEditorInterface().GetEditorViewport().AddChild(dialog);
            dialog.Popup_(new Rect2(50, 50, 700, 500));
        }
        private void CreateYarnScriptDestinationSelected(string destination)
        {
            GD.Print("Creating a yarn script at " + destination);
            editor_utility.CreateYarnScript(destination);
        }
        private void CreateYarnProject()
        {
            GD.Print("Opening 'create yarn project' menu");
            var dialog = new EditorFileDialog();
            dialog.AddFilter("*.tres; Yarn Project");
            dialog.Mode = EditorFileDialog.ModeEnum.SaveFile;
            dialog.WindowTitle = "Create a new Yarn Project";
            dialog.Connect("file_selected", this, nameof(create_yarn_project_destination_selected));
            GetEditorInterface().GetEditorViewport().AddChild(dialog);
            dialog.Popup_(new Rect2(50, 50, 700, 500));
        }

        private void create_yarn_project_destination_selected(string destination)
        {
            GD.Print("Creating a yarn project at " + destination);
            editor_utility.CreateYarnProject(destination);
        }
    }
}