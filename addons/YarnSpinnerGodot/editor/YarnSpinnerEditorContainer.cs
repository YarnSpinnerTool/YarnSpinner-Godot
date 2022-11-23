using Godot;

[Tool]
public class YarnSpinnerEditorContainer : PanelContainer
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
