#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
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
        private Button recompileButton;
        private YarnCompileErrorsPropertyEditor _compileErrorsPropertyEditor;
        private ScrollContainer parseErrorControl;
        private YarnProject project;
        private PackedScene _fileNameLabelScene = ResourceLoader.Load<PackedScene>("res://addons/YarnSpinnerGodot/Editor/UI/FilenameLabel.tscn");
        private PackedScene _errorTextLabelScene = ResourceLoader.Load<PackedScene>("res://addons/YarnSpinnerGodot/Editor/UI/ErrorTextLabel.tscn");
        private PackedScene _contextLabelScene = ResourceLoader.Load<PackedScene>("res://addons/YarnSpinnerGodot/Editor/UI/ContextLabel.tscn");

        private VBoxContainer _container;
        
        public override bool CanHandle(Object obj)
        {
            return obj is YarnProject;
        }

        public override bool ParseProperty(Object @object, int type, string path, int hint, string hintText, int usage)
        {
            project = (YarnProject)@object;
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
                parseErrorControl = new ScrollContainer();
                parseErrorControl.RectClipContent = false;
                parseErrorControl.RectMinSize = new Vector2(0, 200);
                parseErrorControl.SizeFlagsVertical |= (int)Control.SizeFlags.Expand;
                parseErrorControl.SizeFlagsHorizontal |= (int)Control.SizeFlags.Expand;
                
                _container = new VBoxContainer();
                _container.SizeFlagsVertical |= (int)Control.SizeFlags.Expand;
                _container.SizeFlagsHorizontal |= (int)Control.SizeFlags.Expand;
                parseErrorControl.AddChild(_container);
                //parseErrorControl.BbcodeEnabled = true;
                _compileErrorsPropertyEditor.OnErrorsUpdated += RenderCompilationErrors;
                RenderCompilationErrors(project);
                AddCustomControl(parseErrorControl);
                return true;
            }

            return false;
        }

        public override void ParseBegin(Object @object)
        {
            project = (YarnProject)@object;
            if (recompileButton != null)
            {
                if (IsInstanceValid(recompileButton))
                {
                    recompileButton.QueueFree();
                }
                recompileButton = null;
            }
            recompileButton = new Button();
            recompileButton.Text = "Re-compile Scripts in Project";
            var recompileArgs = new Godot.Collections.Array();
            recompileArgs.Add(@object);
            recompileButton.Connect("pressed", this, nameof(OnRecompileClicked), recompileArgs);
            AddCustomControl(recompileButton);
        }

        private void OnRecompileClicked(YarnProject project)
        {
            var projectUtility = new YarnProjectUtility();
            projectUtility.UpdateYarnProject(project);
            _compileErrorsPropertyEditor.Refresh();
        }

        public void RenderCompilationErrors(Object yarnProject)
        {
            project = (YarnProject)yarnProject;
            var errors = project.CompileErrors;
            SetErrors(errors);
        }
        
        private void SetErrors(List<YarnProjectError> errors)
        {
            for (var i = _container.GetChildCount()-1; i >= 0; i--)
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
    }
}
#endif