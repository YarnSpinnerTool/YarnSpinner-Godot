#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using Yarn.GodotIntegration;
using Object = Godot.Object;

namespace YarnDonut.Editor
{
    [Tool]
    public partial class YarnProjectInspectorPlugin : EditorInspectorPlugin
    {
        private Button _recompileButton;
        private Button _addTagsButton;
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
                    _sourceScriptsControl.HintTooltip = "YarnSpinner will search for all .yarn files" +
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

                return false;
            }
            catch (Exception e)
            {
                GD.PushError($"Error in {nameof(YarnProjectInspectorPlugin)}: {e.Message}\n{e.StackTrace}");
                return false;
            }
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
            }
            catch (Exception e)
            {
                GD.PushError($"Error in {nameof(YarnProjectInspectorPlugin)}: {e.Message}\n{e.StackTrace}");
            }
        }

        private void OnRecompileClicked(YarnProject project)
        {
            YarnProjectEditorUtility.UpdateYarnProject(project);
            _compileErrorsPropertyEditor.Refresh();
            PropertyListChangedNotify();
        }

        public void RenderCompilationErrors(Object yarnProject)
        {
            _project = (YarnProject)yarnProject;
            var errors = _project.ProjectErrors;
            SetErrors(errors);
            PropertyListChangedNotify();
        }

        public void RenderSourceScriptsList(Object yarnProject)
        {
            _project = (YarnProject)yarnProject;
            var scripts = _project.SourceScripts;
            SetSourceScripts(scripts);
            PropertyListChangedNotify();
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
            PropertyListChangedNotify();
        }

    }
}
#endif