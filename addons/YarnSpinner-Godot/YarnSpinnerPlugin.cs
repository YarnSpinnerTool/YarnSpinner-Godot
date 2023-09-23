#if TOOLS
using Godot;
using Godot.Collections;
using YarnSpinnerGodot.Editor;
namespace YarnSpinnerGodot
{
    /// <summary>
    /// Main plugin script for the YarnSpinner-Godot plugin
    /// </summary>
    [Tool]
    public partial class YarnSpinnerPlugin : EditorPlugin
    {
        private YarnImporter _scriptImportPlugin;
        private YarnProjectInspectorPlugin _projectInspectorPlugin;
  
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
            var scriptImporterScript = ResourceLoader.Load<CSharpScript>("res://addons/YarnSpinner-Godot/Editor/YarnImporter.cs");
            var projectInspectorScript = ResourceLoader.Load<CSharpScript>("res://addons/YarnSpinner-Godot/Editor/YarnProjectInspectorPlugin.cs");
            var localizationScript = ResourceLoader.Load<CSharpScript>("res://addons/YarnSpinner-Godot/Runtime/Localization.cs");
            var yarnProjectScript = ResourceLoader.Load<CSharpScript>("res://addons/YarnSpinner-Godot/Runtime/YarnProject.cs");
            var dialogueRunnerScript = ResourceLoader.Load<CSharpScript>("res://addons/YarnSpinner-Godot/Runtime/DialogueRunner.cs");

            // load icons
            var miniLocalizationIcon = ResourceLoader.Load<Texture2D>("res://addons/YarnSpinner-Godot/Editor/Icons/Asset Icons/mini_Localization Icon.png");
            var miniYarnSpinnerIcon = ResourceLoader.Load<Texture2D>("res://addons/YarnSpinner-Godot/Editor/Icons/mini_YarnSpinnerLogo.png");
            var miniYarnProjectIcon = ResourceLoader.Load<Texture2D>("res://addons/YarnSpinner-Godot/Editor/Icons/Asset Icons/mini_YarnProject Icon.png");

            _scriptImportPlugin = (YarnImporter)scriptImporterScript.New();
            _projectInspectorPlugin = (YarnProjectInspectorPlugin)projectInspectorScript.New();
            _projectInspectorPlugin.editorInterface = GetEditorInterface();
            AddInspectorPlugin(_projectInspectorPlugin);
            AddImportPlugin(_scriptImportPlugin);

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
            dialog.FileMode = EditorFileDialog.FileModeEnum.SaveFile;
            dialog.Title = windowTitle;
            dialog.Connect("file_selected", new Callable(this, fileSelectedHandler));
            GetEditorInterface().GetBaseControl().AddChild(dialog);
            dialog.Popup(new Rect2I(50, 50, 700, 500));
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