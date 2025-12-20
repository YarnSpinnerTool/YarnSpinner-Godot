# Example of writing an options presenter in GDScript
extends Node

@export var option_item_prefab : PackedScene
@export var options_container: Container
var option_selected_handler: Callable 
var option_selected : bool = false 

func _ready() -> void: 
	options_container.visible = false 

func run_options_async(options: Array, on_option_selected: Callable) -> void:
	print("Options: %s"  % JSON.stringify(options))

	# options is a Dictionary converted from the LocalizedLine C# Class
	# converts to GDScript DialogueOptions array
	var dialogue_options := YarnSpinner.dialogue_options_from_array(options)
	await _run_options_internal(dialogue_options, on_option_selected)

func _run_options_internal(dialogue_options: Array[YarnSpinner.DialogueOption], on_option_selected: Callable) -> void:
	# You can do await statements here if you want.
	await get_tree().process_frame
	option_selected = false
	while options_container.get_child_count() > 0:
		options_container.remove_child(options_container.get_child(0))

	for option in dialogue_options:
		var option_item: Button = option_item_prefab.instantiate() 
		
		option_item.disabled = !option.is_available # disable unavaliable options
		option_item.text = option.line.text.text
		option_item.pressed.connect(func (): 
			on_option_selected.call(option.dialogue_option_id)
			option_selected = true
		)
		
		options_container.add_child(option_item)
	
	options_container.visible = true 
	while not option_selected:
		await get_tree().process_frame
	options_container.visible = false
