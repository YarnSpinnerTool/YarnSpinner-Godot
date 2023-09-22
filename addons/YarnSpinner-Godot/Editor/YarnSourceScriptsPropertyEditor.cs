#if TOOLS
using Godot;
using Godot.Collections;
namespace YarnSpinnerGodot.Editor
{
    [Tool]
    public partial class YarnSourceScriptsPropertyEditor : EditorProperty
    {
        // The main control for editing the property.
        private Label _propertyControl = new Label();
        // An internal value of the property.
        private Array _currentValue;

        public YarnSourceScriptsPropertyEditor()
        {
            Label = "Source Scripts";

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
            var newValue = (Array)newVariantValue;
            if (newValue == _currentValue)
            {
                return;
            }
            _currentValue = newValue;
            RefreshControlText();
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
                _propertyControl.Text = $"{_currentValue.Count} .yarn script{(_currentValue.Count > 1 ? "s" : "")}";
            }
            _propertyControl.ResetSize();
        }

        public void Refresh()
        {
            EmitChanged(GetEditedProperty(), GetEditedObject().Get(GetEditedProperty()));
        }
    }
}
#endif