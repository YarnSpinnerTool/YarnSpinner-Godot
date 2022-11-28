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
        private YarnImporter _scriptImportPlugin;
        private YarnProjectInspectorPlugin _projectInspectorPlugin;
        private YarnEditorUtility _editorUtility;
        private PopupMenu _popup;
        private const string PopupName = "YarnSpinner";

        private const int CreateYarnScriptId = 1;
        public const int createYarnProjectID = 2;

        public override void _EnterTree()
        {
            // load script resources
            var scriptImporterScript = ResourceLoader.Load<CSharpScript>("res://addons/YarnSpinnerGodot/editor/YarnImporter.cs");
            var editorUtilityScript = ResourceLoader.Load<CSharpScript>("res://addons/YarnSpinnerGodot/Editor/YarnEditorUtility.cs");
            var projectInspectorScript = ResourceLoader.Load<CSharpScript>("res://addons/YarnSpinnerGodot/Editor/YarnProjectInspectorPlugin.cs");
            var localizationScript = ResourceLoader.Load<CSharpScript>("res://addons/YarnSpinnerGodot/Runtime/Localization.cs");
            var dialogueRunnerScript = ResourceLoader.Load<CSharpScript>("res://addons/YarnSpinnerGodot/Runtime/DialogueRunner.cs");

            // load icons
            var miniLocalizationIcon = ResourceLoader.Load<Texture>("res://addons/YarnSpinnerGodot/Editor/Icons/Asset Icons/mini_Localization Icon.png");
            var miniYarnSpinnerIcon = ResourceLoader.Load<Texture>("res://addons/YarnSpinnerGodot/Editor/Icons/mini_YarnSpinnerLogo.png");


            _scriptImportPlugin = (YarnImporter)scriptImporterScript.New();
            _editorUtility = (YarnEditorUtility)editorUtilityScript.New();
            _projectInspectorPlugin = (YarnProjectInspectorPlugin)projectInspectorScript.New();
            AddInspectorPlugin(_projectInspectorPlugin);
            AddImportPlugin(_scriptImportPlugin);
            _popup = new PopupMenu();

            _popup.AddItem("Create Yarn Script", CreateYarnScriptId);
            _popup.AddItem("Create Yarn Project", createYarnProjectID);
            _popup.Connect("id_pressed", this, "on_popup_id_pressed");
            AddToolSubmenuItem(PopupName, _popup);
            AddCustomType("DialogueRunner", "Node",
                dialogueRunnerScript,
                miniYarnSpinnerIcon);
            AddCustomType("Localization", "Resource",
                localizationScript,
                miniLocalizationIcon);
        }
        public override void _ExitTree()
        {
            RemoveImportPlugin(_scriptImportPlugin);
            RemoveCustomType("DialogueRunner");
            RemoveCustomType("Localization");
            RemoveInspectorPlugin(_projectInspectorPlugin);
            RemoveToolMenuItem(PopupName);
        }

        /// <summary>
        /// Called when an item in the Tools > Yarn Spinner menu is clicked
        /// </summary>
        /// <param name="id"></param>
        public void OnPopupIDPressed(int id)
        {
            if (id == CreateYarnScriptId)
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
            _editorUtility.CreateYarnScript(destination);
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
            _editorUtility.CreateYarnProject(destination);
        }
    }
}