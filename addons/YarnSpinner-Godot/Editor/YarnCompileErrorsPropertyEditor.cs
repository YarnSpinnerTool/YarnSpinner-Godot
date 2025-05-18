#nullable disable
#if TOOLS
using Godot;
using Array = Godot.Collections.Array;

namespace YarnSpinnerGodot;

[Tool]
public partial class YarnCompileErrorsPropertyEditor : EditorProperty
{
    // The main control for editing the property.
    private Label _propertyControl;

    // An internal value of the property.
    private Array _currentValue;

    [Signal]
    public delegate void OnErrorsUpdateEventHandler(GodotObject yarnProject);

    public YarnCompileErrorsPropertyEditor()
    {
        _propertyControl = new Label();
        Label = "Project Errors";
        // Add the control as a direct child of EditorProperty node.
        AddChild(_propertyControl);
        // Make sure the control is able to retain the focus.
        AddFocusable(_propertyControl);
        // Setup the initial state and connect to the signal to track changes.
        RefreshControlText();
    }

    public override void _UpdateProperty()
    {
        // Read the current value from the property.
        var newVariantValue = GetEditedObject().Get(GetEditedProperty());
        var newValue = (Array) newVariantValue;
        if (newValue == _currentValue)
        {
            return;
        }

        _currentValue = newValue;
        RefreshControlText();
        EmitSignal(SignalName.OnErrorsUpdate);
    }

    private void RefreshControlText()
    {
        if (_currentValue == null)
        {
            _propertyControl.Text = "";
        }
        else if (_currentValue.Count == 0)
        {
            _propertyControl.Text = "None";
        }
        else
        {
            _propertyControl.Text =
                $"❌{_currentValue.Count} error{(_currentValue.Count > 1 ? "s" : "")}";
        }
    }

    public void Refresh()
    {
        EmitChanged(GetEditedProperty(),
            GetEditedObject().Get(GetEditedProperty()));
    }
}
#endif