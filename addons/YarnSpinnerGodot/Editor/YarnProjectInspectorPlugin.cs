#if TOOLS
using System;
using System.Collections.Generic;
using Godot;
using Yarn.GodotIntegration;
using Yarn.GodotIntegration.Editor;
using Object = Godot.Object;

namespace YarnSpinnerGodot.addons.YarnSpinnerGodot
{
    [Tool]
    public class YarnProjectInspectorPlugin : EditorInspectorPlugin
    {
        private Button recompileButton; 
        
        public override bool CanHandle(Object obj)
        {
            return obj is YarnProject;
        }

        public override bool ParseProperty(Object @object, int type, string path, int hint, string hintText, int usage)
        {
            var project = (YarnProject)@object;
            // We handle properties of type integer.
            var hideProperties = new List<string>
            {
                nameof(YarnProject.LastImportHadAnyStrings),
                nameof(YarnProject.LastImportHadImplicitStringIDs)
            };
            if (hideProperties.Contains(path))
            {
                // hide these properties from inspector
                return true;
            }
            if (path == nameof(YarnProject.CompileErrors))
            {
                // Create an instance of the custom property editor and register
                // it to a specific property path.
                // AddPropertyEditor(path, new RandomIntEditor());
                // // Inform the editor to remove the default property editor for
                // // this property type.
                // return true;
                var parseErrorControl = new Label();
                parseErrorControl.Text = "Compilation Errors\n";
                foreach (var msg in project.CompileErrors)
                {
                    parseErrorControl.Text += msg + "\n";
                }
                AddCustomControl(parseErrorControl);
                return true;
            }

            return false;
        }
        
        public override void ParseBegin(Object @object)
        {
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
            var importer = new YarnProjectUtility();
            importer.UpdateYarnProject(project);
        }
    }
}
#endif