using System;
using Godot;

namespace YarnSpinnerGodot
{
	public partial class OptionView : Button
	{
		[Export] bool showCharacterName = false;

		public Action<DialogueOption> OnOptionSelected;

		DialogueOption _option;

		bool hasSubmittedOptionSelection = false;

		public DialogueOption Option
		{
			get => _option;

			set {
				_option = value;

				hasSubmittedOptionSelection = false;

				// When we're given an Option, use its text and update our
				// interactibility.
				if (showCharacterName)
				{
					Text = value.Line.Text.Text;
				}
				else
				{
					Text = value.Line.TextWithoutCharacterName.Text;
				}
				Disabled = !value.IsAvailable;
			}
		}

		public override void _Ready()
		{
			Connect("pressed", new Callable(this, nameof(InvokeOptionSelected)));
		}

		public override void _ExitTree()
		{
			Disconnect("pressed", new Callable(this, nameof(InvokeOptionSelected)));
		}

		public void InvokeOptionSelected()
		{
			// We only want to invoke this once, because it's an error to
			// submit an option when the Dialogue Runner isn't expecting it. To
			// prevent this, we'll only invoke this if the flag hasn't been cleared already.
			if (hasSubmittedOptionSelection == false)
			{
				OnOptionSelected.Invoke(Option);
				hasSubmittedOptionSelection = true;
			}
		}
	}
}
