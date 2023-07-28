#if TOOLS
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;
using Object = Godot.Object;

namespace YarnDonut.Editor
{
    [Tool]
    public partial class YarnProjectInspectorPlugin : EditorInspectorPlugin
    {
        public EditorInterface editorInterface;
        private Button _recompileButton;
        private Button _addTagsButton;
        private Button _updateLocalizationsButton;
        private YarnCompileErrorsPropertyEditor _compileErrorsPropertyEditor;
        private ScrollContainer _parseErrorControl;
        private ScrollContainer _sourceScriptsControl;
        private YarnProject _project;
        private readonly PackedScene _fileNameLabelScene = ResourceLoader.Load<PackedScene>("res://addons/YarnDonut/Editor/UI/FilenameLabel.tscn");
        private readonly PackedScene _errorTextLabelScene = ResourceLoader.Load<PackedScene>("res://addons/YarnDonut/Editor/UI/ErrorTextLabel.tscn");
        private readonly PackedScene _contextLabelScene = ResourceLoader.Load<PackedScene>("res://addons/YarnDonut/Editor/UI/ContextLabel.tscn");
        private VBoxContainer _errorContainer;
        private VBoxContainer _sourceScriptsContainer;
        private YarnSourceScriptsPropertyEditor _sourceScriptsPropertyEditor;

        public override bool CanHandle(Object obj)
        {
            return obj is YarnProject;
        }

        public override bool ParseProperty(Object @object, int type, string path, int hint, string hintText, int usage)
        {
            try
            {
                _project = (YarnProject)@object;
                // hide some properties that are not editable by the user
                var hideProperties = new List<string>
                {
                    nameof(YarnProject.LastImportHadAnyStrings),
                    nameof(YarnProject.LastImportHadImplicitStringIDs),
                    nameof(YarnProject.IsSuccessfullyParsed),
                    nameof(YarnProject.CompiledYarnProgramBase64)
                };
                if (hideProperties.Contains(path))
                {
                    // hide these properties from inspector
                    return true;
                }
                if (path == nameof(YarnProject.SourceScripts))
                {

                    _sourceScriptsPropertyEditor = new YarnSourceScriptsPropertyEditor();
                    AddPropertyEditor(path, _sourceScriptsPropertyEditor);
                    _sourceScriptsControl = new ScrollContainer();
                    _sourceScriptsControl.HintTooltip = "YarnDonut will search for all .yarn files" +
                        $" in the same directory as this {nameof(YarnProject)} (or a descendent directory)." +
                        " A list of .yarn files found this way will be displayed here.";
                    int scriptAreaHeight = 40;
                    if (_project.SourceScripts != null && _project.SourceScripts.Any())
                    {
                        scriptAreaHeight = 180;
                    }

                    _sourceScriptsControl.RectMinSize = new Vector2(0, scriptAreaHeight);
                    _sourceScriptsControl.SizeFlagsVertical |= (int)Control.SizeFlags.Expand;
                    _sourceScriptsControl.SizeFlagsHorizontal |= (int)Control.SizeFlags.Expand;

                    _sourceScriptsContainer = new VBoxContainer();
                    _sourceScriptsContainer.SizeFlagsVertical |= (int)Control.SizeFlags.Expand;
                    _sourceScriptsContainer.SizeFlagsHorizontal |= (int)Control.SizeFlags.Expand;
                    _sourceScriptsControl.AddChild(_sourceScriptsContainer);
                    RenderSourceScriptsList(_project);
                    AddCustomControl(_sourceScriptsControl);
                    return true;
                }

                if (path == nameof(YarnProject.ProjectErrors))
                {
                    _compileErrorsPropertyEditor = new YarnCompileErrorsPropertyEditor();
                    AddPropertyEditor(path, _compileErrorsPropertyEditor);
                    _parseErrorControl = new ScrollContainer();
                    int errorAreaHeight = 40;
                    if (_project.ProjectErrors != null && _project.ProjectErrors.Length > 0)
                    {
                        errorAreaHeight = 200;
                    }

                    _parseErrorControl.RectMinSize = new Vector2(0, errorAreaHeight);
                    _parseErrorControl.SizeFlagsVertical |= (int)Control.SizeFlags.Expand;
                    _parseErrorControl.SizeFlagsHorizontal |= (int)Control.SizeFlags.Expand;

                    _errorContainer = new VBoxContainer();
                    _errorContainer.SizeFlagsVertical |= (int)Control.SizeFlags.Expand;
                    _errorContainer.SizeFlagsHorizontal |= (int)Control.SizeFlags.Expand;
                    _parseErrorControl.AddChild(_errorContainer);
                    //parseErrorControl.BbcodeEnabled = true;
                    _compileErrorsPropertyEditor.OnErrorsUpdated += RenderCompilationErrors;
                    RenderCompilationErrors(_project);
                    AddCustomControl(_parseErrorControl);
                    return true;
                }
                if (path == nameof(YarnProject.LocaleCodeToCSVPath))
                {
                    var localeGrid = new GridContainer();
                    localeGrid.Columns = 3;

                    var label = new Label();
                    label.Text = "Localization CSVs";

                    localeGrid.AddChild(label);
                    localeGrid.AddChild(new Label());

                    var addButton = new Button();
                    addButton.Text = "Add";
                    addButton.Connect("pressed", this, nameof(AddLocale));
                    localeGrid.AddChild(addButton);
                    localeGrid.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;
                    localeGrid.SizeFlagsVertical = (int)Control.SizeFlags.ExpandFill;

                    foreach (var locale in _project.LocaleCodeToCSVPath)
                    {
                        var localeLabel = new Label();
                        localeLabel.Text = locale.Key;
                        localeGrid.AddChild(localeLabel);
                        var picker = new HBoxContainer();
                        picker.SizeFlagsHorizontal = (int)Control.SizeFlags.ExpandFill;
                        picker.SizeFlagsVertical = (int)Control.SizeFlags.ExpandFill;
                        var pathLabel = new Label();
                        pathLabel.Text = locale.Value;
                        if (pathLabel.Text == "")
                        {
                            pathLabel.Text = "(none)";
                        }
                        pathLabel.RectMinSize = new Vector2(80, 30);
                        pathLabel.SizeFlagsHorizontal |= (int)Control.SizeFlags.ExpandFill;
                        pathLabel.ClipText = true;
                        picker.AddChild(pathLabel);
                        var pickerButton = new Button();
                        pickerButton.Text = "Browse";
                        pickerButton.Connect("pressed", this, nameof(SelectLocaleCSVPath), new Array { locale.Key });
                        picker.AddChild(pickerButton);
                        localeGrid.AddChild(picker);
                        var deleteButton = new Button();
                        deleteButton.Text = "X";
                        deleteButton.Connect("pressed", this, nameof(RemoveLocale), new Array { locale.Key });
                        localeGrid.AddChild(deleteButton);
                    }
                    AddCustomControl(localeGrid);
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                GD.PushError($"Error in {nameof(YarnProjectInspectorPlugin)}: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        private void RemoveLocale(string localeCode)
        {
            GD.Print($"Removed locale code {localeCode}");
            _project.LocaleCodeToCSVPath.Remove(localeCode);
            _project.PropertyListChangedNotify();
        }

        public void SelectLocaleCSVPath(string localeCode)
        {
            var dialog = new FileDialog();
            dialog.AddFilter("*.csv; CSV File");
            dialog.Mode = FileDialog.ModeEnum.SaveFile;
            dialog.Access = FileDialog.AccessEnum.Filesystem;
            dialog.WindowTitle = $"Select CSV Path for Locale {localeCode}";
            dialog.Connect("file_selected", this, nameof(CSVFileSelected), new Array { localeCode });
            editorInterface.GetViewport().AddChild(dialog);
            dialog.Popup_(new Rect2(50, 50, 700, 500));
        }
        public void CSVFileSelected(string savePath, string localeCode)
        {
            savePath = ProjectSettings.LocalizePath(savePath);
            GD.Print($"CSV file selected for locale {localeCode}: {savePath}");
            _project.LocaleCodeToCSVPath[localeCode] = savePath;
            _project.PropertyListChangedNotify();
        }

        private string _newLocale = null;
        private bool _addLocaleConnected;
        private void AddLocale()
        {
            _addLocaleConnected = false;
            var dialog = new AcceptDialog();
            dialog.WindowTitle = "Add New Locale Code";
            SetNewLocaleText("", dialog.GetOk());
            var textEntry = new LineEdit();
            dialog.AddChild(textEntry);
            textEntry.PlaceholderText = "locale code";
            textEntry.Connect("text_changed", this, nameof(SetNewLocaleText), new Array(dialog.GetOk()));
            editorInterface.GetViewport().AddChild(dialog);
            dialog.Popup_(new Rect2(50, 50, 400, 150));
            textEntry.GrabFocus();

        }
        private void SetNewLocaleText(string localeCode, Button okButton)
        {
            _newLocale = localeCode;
            okButton.Disabled = string.IsNullOrEmpty(_newLocale);
            if (_addLocaleConnected)
            {
                okButton.Disconnect("pressed", this, nameof(LocaleAdded));
            }
            if (okButton.Disabled)
            {
                return;
            }
            okButton.Connect("pressed", this, nameof(LocaleAdded), new Array { localeCode });
            _addLocaleConnected = true;
        }

        public void LocaleAdded(string localeCode)
        {
            _project.LocaleCodeToCSVPath[localeCode] = "";
            _project.PropertyListChangedNotify();
        }
        public override void ParseBegin(Object @object)
        {
            try
            {
                _project = (YarnProject)@object;
                YarnProjectEditorUtility.AddProjectToList(_project);
                if (_recompileButton != null)
                {
                    if (IsInstanceValid(_recompileButton))
                    {
                        _recompileButton.QueueFree();
                    }
                    _recompileButton = null;
                }
                _recompileButton = new Button();
                _recompileButton.Text = "Re-compile Scripts in Project";
                var recompileArgs = new Godot.Collections.Array();
                recompileArgs.Add(@object);
                _recompileButton.Connect("pressed", this, nameof(OnRecompileClicked), recompileArgs);
                AddCustomControl(_recompileButton);

                if (_addTagsButton != null)
                {
                    if (IsInstanceValid(_addTagsButton))
                    {
                        _addTagsButton.QueueFree();
                    }
                    _addTagsButton = null;
                }
                _addTagsButton = new Button();

                _addTagsButton.Text = "Add Line Tags to Scripts";
                var addTagsButtonArgs = new Godot.Collections.Array();
                addTagsButtonArgs.Add(_project);
                _addTagsButton.Connect("pressed", this, nameof(OnAddTagsClicked), addTagsButtonArgs);
                AddCustomControl(_addTagsButton);

                if (_updateLocalizationsButton != null)
                {
                    if (IsInstanceValid(_updateLocalizationsButton))
                    {
                        _updateLocalizationsButton.QueueFree();
                    }
                    _updateLocalizationsButton = null;
                }
                _updateLocalizationsButton = new Button();

                _updateLocalizationsButton.Text = "Update Localizations";
                _updateLocalizationsButton.HintTooltip = "Update Localization CSV and Godot .translation Files";
                _updateLocalizationsButton.Connect("pressed", this, nameof(OnUpdateLocalizationsClicked));
                AddCustomControl(_updateLocalizationsButton);
            }
            catch (Exception e)
            {
                GD.PushError($"Error in {nameof(YarnProjectInspectorPlugin)}: {e.Message}\n{e.StackTrace}");
            }
        }
        private void OnUpdateLocalizationsClicked()
        {
            YarnProjectEditorUtility.UpdateLocalizationCSVs(_project);
        }
        private void OnRecompileClicked(YarnProject project)
        {
            YarnProjectEditorUtility.UpdateYarnProject(project);
            _compileErrorsPropertyEditor.Refresh();
            project.PropertyListChangedNotify();
        }

        public void RenderCompilationErrors(Object yarnProject)
        {
            _project = (YarnProject)yarnProject;
            var errors = _project.ProjectErrors;
            SetErrors(errors);
            yarnProject.PropertyListChangedNotify();
        }

        public void RenderSourceScriptsList(Object yarnProject)
        {
            _project = (YarnProject)yarnProject;
            var scripts = _project.SourceScripts;
            SetSourceScripts(scripts);
            yarnProject.PropertyListChangedNotify();
        }

        private void SetErrors(YarnProjectError[] errors)
        {
            for (var i = _errorContainer.GetChildCount() - 1; i >= 0; i--)
            {
                var child = _errorContainer.GetChild(i);

                child.QueueFree();
            }

            var errorGroups = errors.GroupBy(e => e.FileName);
            foreach (var errorGroup in errorGroups)
            {
                var errorsInGroup = errorGroup.ToList();
                var fileNameLabel = _fileNameLabelScene.Instance<Label>();
                var resFileName = ProjectSettings.LocalizePath(errorsInGroup[0].FileName);
                fileNameLabel.Text = $"{resFileName}:";
                _errorContainer.AddChild(fileNameLabel);
                var separator = new HSeparator();
                separator.RectMinSize = new Vector2(0, 4);
                separator.SizeFlagsHorizontal |= (int)Control.SizeFlags.Expand;
                _errorContainer.AddChild(separator);
                foreach (var err in errorsInGroup)
                {
                    var errorTextLabel = _errorTextLabelScene.Instance<Label>();
                    errorTextLabel.Text = $"    {err.Message}";
                    _errorContainer.AddChild(errorTextLabel);

                    var contextLabel = _contextLabelScene.Instance<Label>();
                    contextLabel.Text = $"    {err.Context}";
                    _errorContainer.AddChild(contextLabel);
                }
            }
        }
        private void SetSourceScripts(Array<string> sourceScripts)
        {
            for (var i = _sourceScriptsContainer.GetChildCount() - 1; i >= 0; i--)
            {
                var child = _sourceScriptsContainer.GetChild(i);

                child.QueueFree();
            }

            foreach (var script in sourceScripts)
            {
                var fileNameLabel = _fileNameLabelScene.Instance<Label>();
                var resFileName = ProjectSettings.LocalizePath(script.Replace("\\", "/"));
                fileNameLabel.Text = resFileName;
                _sourceScriptsContainer.AddChild(fileNameLabel);
            }
        }
        private void OnAddTagsClicked(YarnProject project)
        {
            YarnProjectEditorUtility.AddLineTagsToFilesInYarnProject(project);
            _compileErrorsPropertyEditor.Refresh();
            project.PropertyListChangedNotify();
        }

    }
}
#endif