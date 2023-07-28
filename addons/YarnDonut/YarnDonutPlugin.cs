#if TOOLS
using Godot;
using Godot.Collections;
using YarnDonut.Editor;
namespace YarnDonut
{
    /// <summary>
    /// Main plugin script for the YarnDonut plugin
    /// </summary>
    [Tool]
    public class YarnDonutPlugin : EditorPlugin
    {
        private YarnImporter _scriptImportPlugin;
        private YarnProjectInspectorPlugin _projectInspectorPlugin;
  
        private PopupMenu _popup;
        private const string PopupName = "YarnDonut";
        private const int CreateYarnScriptId = 1;
        public const int createYarnProjectID = 2;
        public const int createYarnLocalizationID = 3;

        public override void _EnterTree()
        {
            if (!ProjectSettings.HasSetting(YarnProjectEditorUtility.YARN_PROJECT_PATHS_SETTING_KEY))
            {
                ProjectSettings.SetSetting(YarnProjectEditorUtility.YARN_PROJECT_PATHS_SETTING_KEY, new Array());
            }
            ProjectSettings.SetInitialValue(YarnProjectEditorUtility.YARN_PROJECT_PATHS_SETTING_KEY, new Array());
            // load script resources
            var scriptImporterScript = ResourceLoader.Load<CSharpScript>("res://addons/YarnDonut/Editor/YarnImporter.cs");
            var editorUtilityScript = ResourceLoader.Load<CSharpScript>("res://addons/YarnDonut/Editor/YarnEditorUtility.cs");
            var projectInspectorScript = ResourceLoader.Load<CSharpScript>("res://addons/YarnDonut/Editor/YarnProjectInspectorPlugin.cs");
            var localizationScript = ResourceLoader.Load<CSharpScript>("res://addons/YarnDonut/Runtime/Localization.cs");
            var yarnProjectScript = ResourceLoader.Load<CSharpScript>("res://addons/YarnDonut/Runtime/YarnProject.cs");
            var dialogueRunnerScript = ResourceLoader.Load<CSharpScript>("res://addons/YarnDonut/Runtime/DialogueRunner.cs");

            // load icons
            var miniLocalizationIcon = ResourceLoader.Load<Texture>("res://addons/YarnDonut/Editor/Icons/Asset Icons/mini_Localization Icon.png");
            var miniYarnSpinnerIcon = ResourceLoader.Load<Texture>("res://addons/YarnDonut/Editor/Icons/mini_YarnSpinnerLogo.png");
            var miniYarnProjectIcon = ResourceLoader.Load<Texture>("res://addons/YarnDonut/Editor/Icons/Asset Icons/mini_YarnProject Icon.png");

            _scriptImportPlugin = (YarnImporter)scriptImporterScript.New();
            _projectInspectorPlugin = (YarnProjectInspectorPlugin)projectInspectorScript.New();
            _projectInspectorPlugin.editorInterface = GetEditorInterface();
            AddInspectorPlugin(_projectInspectorPlugin);
            AddImportPlugin(_scriptImportPlugin);
            _popup = new PopupMenu();

            _popup.AddItem("Create Yarn Script", CreateYarnScriptId);
            _popup.AddItem("Create Yarn Project", createYarnProjectID);
            _popup.AddItem("Create Yarn Localization", createYarnLocalizationID);
            _popup.Connect("id_pressed", this, nameof(OnPopupIDPressed));
            AddToolSubmenuItem(PopupName, _popup);
            AddCustomType(nameof(DialogueRunner), "Node", dialogueRunnerScript, miniYarnSpinnerIcon);
            AddCustomType(nameof(Localization), "Resource", localizationScript, miniLocalizationIcon);
            AddCustomType(nameof(YarnProject), "Resource", yarnProjectScript, miniYarnProjectIcon);
        }
        public override void _ExitTree()
        {
            RemoveImportPlugin(_scriptImportPlugin);
            RemoveCustomType(nameof(DialogueRunner));
            RemoveCustomType(nameof(Localization));
            RemoveCustomType(nameof(YarnProject));
            RemoveInspectorPlugin(_projectInspectorPlugin);
            RemoveToolMenuItem(PopupName);
        }

        /// <summary>
        /// Called when an item in the Tools > Yarn Spinner menu is clicked
        /// </summary>
        /// <param name="id"></param>
        public void OnPopupIDPressed(int id)
        {
            switch (id)
            {
                case CreateYarnScriptId:
                    CreateYarnScript();
                    break;
                case createYarnProjectID:
                    CreateYarnProject();
                    break;
                case createYarnLocalizationID:
                    CreateYarnLocalization();
                    break;
            }

        }
        private void CreateYarnScript()
        {
            GD.Print("Opening 'create yarn script' menu");
            ShowCreateFilePopup("*.yarn ; Yarn Script",
                "Create a new Yarn Script", nameof(CreateYarnScriptDestinationSelected));
        }
        private void CreateYarnScriptDestinationSelected(string destination)
        {
            GD.Print("Creating a yarn script at " + destination);
            YarnEditorUtility.CreateYarnScript(destination);
        }
        private void CreateYarnProject()
        {
            GD.Print("Opening 'create yarn project' menu");
            ShowCreateFilePopup("*.tres; Yarn Project", "Create a new Yarn Project",
                nameof(CreateYarnProjectDestinationSelected));
        }

        private void CreateYarnLocalization()
        {
            GD.Print("Opening 'create yarn localization' menu");
            ShowCreateFilePopup("*.tres; Yarn Localization", "Create a new Yarn Localization",
                nameof(CreateYarnLocalizationDestinationSelected));
        }

        private void ShowCreateFilePopup(string filter, string windowTitle, string fileSelectedHandler)
        {
            var dialog = new EditorFileDialog();
            dialog.AddFilter(filter);
            dialog.Mode = EditorFileDialog.ModeEnum.SaveFile;
            dialog.WindowTitle = windowTitle;
            dialog.Connect("file_selected", this, fileSelectedHandler);
            GetEditorInterface().GetViewport().AddChild(dialog);
            dialog.Popup_(new Rect2(50, 50, 700, 500));
        }
        private void CreateYarnProjectDestinationSelected(string destination)
        {
            GD.Print("Creating a yarn project at " + destination);
            YarnEditorUtility.CreateYarnProject(destination);
        }

        private void CreateYarnLocalizationDestinationSelected(string destination)
        {
            GD.Print("Creating a yarn project at " + destination);
            YarnEditorUtility.CreateYarnLocalization(destination);
        }
    }
}
#endif