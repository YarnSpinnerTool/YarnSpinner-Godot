extends Node
# Example of writing an options presenter in GDScript
@export var option_item_prefab : PackedScene
@export var options_container: Container
@export var presenter_control: Control 

var option_selected_handler: Callable 

var option_selected : bool = false 
func _ready() -> void: 
	presenter_control.visible = false 
	
# Example options array: 
#[
	#{
		#"dialogue_option_id": 0,
		#"is_available": true,
		#"line": {
			#"metadata": [],
			#"text": {
				#"attributes": [],
				#"text": "Yes",
				#"text_without_character_name": "Yes"
			#}
		#},
		#"text_id": "line:b7aaff9b"
	#},
	#{
		#"dialogue_option_id": 1,
		#"is_available": true,
		#"line": {
			#"metadata": [],
			#"text": {
				#"attributes": [],
				#"text": "No",
				#"text_without_character_name": "No"
			#}
		#},
		#"text_id": "line:9bcbf175"
	#}
#]
func run_options_async(options: Array, on_option_selected: Callable) -> void:
	print("Options: %s"  % JSON.stringify(options))
	option_selected = false
	# You can do await statements here if you want.
	await get_tree().process_frame
	option_selected_handler = on_option_selected
	while options_container.get_child_count() >0:
		options_container.remove_child(options_container.get_child(0))
	for option in options:
		if not option["is_available"]:
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
