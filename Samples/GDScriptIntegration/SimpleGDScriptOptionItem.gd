extends Button
class_name SimpleGDScriptOptionItem
@export var label: RichTextLabel 

var option_dict: Dictionary
var on_option_selected: Callable 
func set_option(option: Dictionary, on_selected: Callable) -> void:
	option_dict = option
	label.text = option["line"]["text"]["text"]
	on_option_selected = on_selected 
	pressed.connect(on_pressed)
	
func on_pressed()-> void:
	on_option_selected.call(option_dict["dialogue_option_id"])
