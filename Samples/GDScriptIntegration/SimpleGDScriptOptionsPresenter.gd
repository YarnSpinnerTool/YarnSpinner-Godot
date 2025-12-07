# Example of writing an options presenter in GDScript
extends Node

@export var option_item_prefab : PackedScene
@export var options_container: Container
@export var presenter_control: Control 
var option_selected_handler: Callable 
var option_selected : bool = false 

func _ready() -> void: 
	presenter_control.visible = false 

func run_options_async(options: Array, on_option_selected: Callable) -> void:
	print("Options: %s"  % JSON.stringify(options))
	option_selected = false

	# options is a Dictionary converted from the LocalizedLine C# Class
	# converts to GDScript DialogueOptions array
	var dialogue_options := YarnSpinner.dialogue_options_from_array(options)

	# You can do await statements here if you want.
	await get_tree().process_frame
	option_selected_handler = on_option_selected
	while options_container.get_child_count() > 0:
		options_container.remove_child(options_container.get_child(0))

	for option in dialogue_options:
		if not option.is_available:
			# don't render unvailable options
			continue 

		var option_item: SimpleGDScriptOptionItem = option_item_prefab.instantiate() 
		option_item.set_option(option, select_option)
		options_container.add_child(option_item)
	
	presenter_control.visible = true 
	while not option_selected:
		await get_tree().process_frame
	
func select_option(option_id: int) -> void:
	option_selected_handler.call(option_id)
	presenter_control.visible = false 

	while options_container.get_child_count() > 0:
		options_container.remove_child(options_container.get_child(0))

	option_selected = true
