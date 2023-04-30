#if TOOLS
using Godot;
namespace YarnDonut.Editor
{
    [Tool]
    public partial class YarnSpinnerEditorContainer : PanelContainer
    {
        public UndoRedo undoRedo;

        public void SetUndoRedo(UndoRedo redo)
        {
            undoRedo = redo;
        }
        public override void _Ready()
        {
        
        }
    }
}
#endif
