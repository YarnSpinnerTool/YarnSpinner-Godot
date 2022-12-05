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
    public partial class YarnProjectInspectorPlugin : EditorInspectorPlugin
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

        public override bool _CanHandle(Variant obj)
        {
            return obj is YarnProject;
        }

        public override bool _ParseProperty(Object @object,
            long type,
            string name,
            long hintType,
            string hintString,
            long usageFlags,
            bool wide)
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
            if (hideProperties.Contains(name))
            {
                // hide these properties from inspector
                return true;
            }
         if (name == nameof(YarnProject.ProjectErrors))
            {
                _compileErrorsPropertyEditor = new YarnCompileErrorsPropertyEditor();
                AddPropertyEditor(name, _compileErrorsPropertyEditor);
                _parseErrorControl = new ScrollContainer();
                _parseErrorControl.CustomMinimumSize = new Vector2(0, 200);
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

        public override void _ParseBegin(Object @object)
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
            _recompileButton.Connect("pressed",Callable.From(() =>
            {
                OnRecompileClicked(_project);
            }));
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
            _addTagsButton.Connect("pressed", Callable.From(()=>
            {
                OnAddTagsClicked(_project);
            }));
            AddCustomControl(_addTagsButton);
        }

        private void OnRecompileClicked(YarnProject project)
        {
            _projectUtility = new YarnProjectUtility();
            _projectUtility.UpdateYarnProject(project);
            _compileErrorsPropertyEditor.Refresh();
            NotifyPropertyListChanged();
        }

        public void RenderCompilationErrors(Object yarnProject)
        {
            _project = (YarnProject)yarnProject;
            var errors = _project.CompileErrors;
            SetErrors(errors);
            NotifyPropertyListChanged();
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
                var fileNameLabel = _fileNameLabelScene.Instantiate<Label>();
                var resFileName = ProjectSettings.LocalizePath(errorsInGroup[0].FileName);
                fileNameLabel.Text = $"{resFileName}:";
                _container.AddChild(fileNameLabel);
                var separator = new HSeparator();
                separator.CustomMinimumSize = new Vector2(0, 4);
                separator.SizeFlagsHorizontal |= (int)Control.SizeFlags.Expand;
                _container.AddChild(separator);
                foreach (var err in errorsInGroup)
                {
                    var errorTextLabel = _errorTextLabelScene.Instantiate<Label>();
                    errorTextLabel.Text = $"    {err.Message}";
                    _container.AddChild(errorTextLabel);

                    var contextLabel = _contextLabelScene.Instantiate<Label>();
                    contextLabel.Text = $"    {err.Context}";
                    _container.AddChild(contextLabel);
                }
            }
        }

        private void OnAddTagsClicked(YarnProject project)
        {
            _projectUtility.AddLineTagsToFilesInYarnProject(project);
            _compileErrorsPropertyEditor.Refresh();
            NotifyPropertyListChanged();
        }
    }
}
#endif