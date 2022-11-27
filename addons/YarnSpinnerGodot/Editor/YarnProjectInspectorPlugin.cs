#if TOOLS
using System;
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
            recompileButton.RectMinSize = new Vector2(80, 40);
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