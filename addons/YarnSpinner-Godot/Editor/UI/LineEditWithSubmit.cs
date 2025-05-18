#nullable disable
#if TOOLS
using Godot;

namespace YarnSpinnerGodot;

/// <summary>
/// LineEdit component with an associated button.
/// When the LineEdit's text is whitespace or null,
/// the associated submit button will be disabled.
/// </summary>
public partial class LineEditWithSubmit : LineEdit
{
    /// <summary>
    /// The button which will be enabled/disabled
    /// depending on the contents of this LineEdit.
    /// </summary>
    public Button SubmitButton;

    public override void _Ready()
    {
        Connect(LineEdit.SignalName.TextChanged,
            Callable.From((string newText) => OnTextChanged(newText)));
    }

    private void OnTextChanged(string newText)
    {
        if (!IsInstanceValid(SubmitButton))
        {
            return;
        }

        SubmitButton.Disabled =
            string.IsNullOrWhiteSpace(newText);
    }
}
#endif