# Demonstrates interacting with C# YarnSpinner-Godot 
# components from GDScript. 

extends Control
@export var dialogue_runner: Node  
@export var logo: Control 
@export var yarn_project: Resource

func _ready() -> void:
	dialogue_runner.AddCommandHandlerCallable("show_logo", show_logo)
	dialogue_runner.AddCommandHandlerCallable("log", log)
	dialogue_runner.onDialogueComplete.connect(on_dialogue_complete)
	var var_name : String = "$myVariableSetFromGDScript"
	dialogue_runner.variableStorage.SetValue( var_name, "Yay!")
	var var_value = dialogue_runner.variableStorage.GetVariantValue(var_name)
	print("Value of %s: %s" % [var_name, var_value])

func log(message: String) -> void:
	# Example command that does not use `await`, to test 
	# that no errors are thrown if nothing is returned from a command in GDScript. 
	print(message)
	
func show_logo(logo_path) -> void:
	"""
	Async handler for the <<show_logo logo_path>> command.
	"""
	print("Showing logo...")
	var logo_load_error: Error = ResourceLoader.load_threaded_request(logo_path)
	if logo_load_error != OK:
		print("Failed to load logo %s: Error %s" % [logo_path, logo_load_error])
		return
	
	var logo_load_status : ResourceLoader.ThreadLoadStatus = ResourceLoader.ThreadLoadStatus.THREAD_LOAD_IN_PROGRESS
	while logo_load_status == ResourceLoader.ThreadLoadStatus.THREAD_LOAD_IN_PROGRESS:
		logo_load_status = ResourceLoader.load_threaded_get_status(logo_path)
		await get_tree().process_frame
		
	if logo_load_status != ResourceLoader.ThreadLoadStatus.THREAD_LOAD_LOADED:
		print("Failed to load %s. Error: %s" % [logo_path, logo_load_status])
		return
	logo.texture = ResourceLoader.load_threaded_get(logo_path)
	logo.visible = true 
	
	var tween := create_tween()
	tween.tween_property(logo, "modulate", Color.BLUE , 1.0)
	await tween.finished
	tween = create_tween()
	tween.tween_property(logo, "modulate", Color.WHITE , 1.0)
	await tween.finished
	await get_tree().create_timer(1.0).timeout
	print("Hiding logo...")
	logo.visible = false
	
func on_dialogue_complete() -> void: 
	print("Dialogue completed.")
