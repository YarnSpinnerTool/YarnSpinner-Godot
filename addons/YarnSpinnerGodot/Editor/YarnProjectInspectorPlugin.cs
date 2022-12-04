#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Yarn.GodotIntegration;
using Yarn.GodotIntegration.Editor;
using YarnSpinnerGodot.addons.YarnSpinnerGodot.Editor;
using Object = Godot.Object;

namespace YarnSpinnerGodot.addons.YarnSpinnerGodot
{
    [Tool]
    public class YarnProjectInspectorPlugin : EditorInspectorPlugin
    {
        private Button _recompileButton;
        private Button _addTagsButton;
        private YarnCompileErrorsPropertyEditor _compileErrorsPropertyEditor;
        private ScrollContainer _parseErrorControl;
        private YarnProject _project;
        private readonly PackedScene _fileNameLabelScene = ResourceLoader.Load<PackedScene>("res://addons/YarnSpinnerGodot/Editor/UI/FilenameLabel.tscn");
        private readonly PackedScene _errorTextLabelScene = ResourceLoader.Load<PackedScene>("res://addons/YarnSpinnerGodot/Editor/UI/ErrorTextLabel.tscn");
        private readonly PackedScene _contextLabelScene = ResourceLoader.Load<PackedScene>("res://addons/YarnSpinnerGodot/Editor/UI/ContextLabel.tscn");
        private YarnProjectUtility _projectUtility = new YarnProjectUtility();
        private VBoxContainer _container;

        public override bool CanHandle(Object obj)
        {
            return obj is YarnProject;
        }

        public override bool ParseProperty(Object @object, int type, string path, int hint, string hintText, int usage)
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
         if (path == nameof(YarnProject.ProjectErrors))
            {
                _compileErrorsPropertyEditor = new YarnCompileErrorsPropertyEditor();
                AddPropertyEditor(path, _compileErrorsPropertyEditor);
                _parseErrorControl = new ScrollContainer();
                _parseErrorControl.RectMinSize = new Vector2(0, 200);
                _parseErrorControl.SizeFlagsVertical |= (int)Control.SizeFlags.Expand;
                _parseErrorControl.SizeFlagsHorizontal |= (int)Control.SizeFlags.Expand;

                _container = new VBoxContainer();
                _container.SizeFlagsVertical |= (int)Control.SizeFlags.Expand;
                _container.SizeFlagsHorizontal |= (int)Control.SizeFlags.Expand;
                _parseErrorControl.AddChild(_container);
                //parseErrorControl.BbcodeEnabled = true;
                _compileErrorsPropertyEditor.OnErrorsUpdated += RenderCompilationErrors;
                RenderCompilationErrors(_project);
                AddCustomControl(_parseErrorControl);
                return true;
            }

            return false;
        }

        public override void ParseBegin(Object @object)
        {
            _project = (YarnProject)@object;
            _projectUtility.AddProjectToList(_project);
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

        private void OnRecompileClicked(YarnProject project)
        {
            _projectUtility = new YarnProjectUtility();
            _projectUtility.UpdateYarnProject(project);
            _compileErrorsPropertyEditor.Refresh();
        }

        public void RenderCompilationErrors(Object yarnProject)
        {
            _project = (YarnProject)yarnProject;
            var errors = _project.CompileErrors;
            SetErrors(errors);
        }

        private void SetErrors(List<YarnProjectError> errors)
        {
            for (var i = _container.GetChildCount() - 1; i >= 0; i--)
            {
                var child = _container.GetChild(i);

                child.QueueFree();
            }

            var errorGroups = errors.GroupBy(e => e.FileName);
            foreach (var errorGroup in errorGroups)
            {
                var errorsInGroup = errorGroup.ToList();
                var fileNameLabel = _fileNameLabelScene.Instance<Label>();
                var resFileName = ProjectSettings.LocalizePath(errorsInGroup[0].FileName);
                fileNameLabel.Text = $"{resFileName}:";
                _container.AddChild(fileNameLabel);
                var separator = new HSeparator();
                separator.RectMinSize = new Vector2(0, 4);
                separator.SizeFlagsHorizontal |= (int)Control.SizeFlags.Expand;
                _container.AddChild(separator);
                foreach (var err in errorsInGroup)
                {
                    var errorTextLabel = _errorTextLabelScene.Instance<Label>();
                    errorTextLabel.Text = $"    {err.Message}";
                    _container.AddChild(errorTextLabel);

                    var contextLabel = _contextLabelScene.Instance<Label>();
                    contextLabel.Text = $"    {err.Context}";
                    _container.AddChild(contextLabel);
                }
            }
        }

        private void OnAddTagsClicked(YarnProject project)
        {
            _projectUtility.AddLineTagsToFilesInYarnProject(project);
            _compileErrorsPropertyEditor.Refresh();
        }
    }
}
#endif