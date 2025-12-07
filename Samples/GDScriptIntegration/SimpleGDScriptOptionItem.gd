class_name SimpleGDScriptOptionItem extends Button

@export var label: RichTextLabel 

func set_option(option: YarnSpinner.DialogueOption, on_selected: Callable) -> void:
	label.text = option.line.text.text

	pressed.connect(func() -> void:
		on_selected.call(option.dialogue_option_id)
	)
