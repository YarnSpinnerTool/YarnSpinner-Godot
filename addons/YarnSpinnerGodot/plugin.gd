tool
extends EditorPlugin

var container
var project_importer = preload("res://addons/YarnSpinnerGodot/editor/YarnProjectImporter.cs")
var import_plugin

func _enter_tree():
	container = preload("res://addons/YarnSpinnerGodot/editor/YarnSpinnerEditorContainer.tscn").instance()
	container.undoRedo = get_undo_redo()
	add_control_to_bottom_panel(container, "YarnSpinner")
	import_plugin = project_importer.new()
	add_import_plugin(import_plugin);
	add_custom_type("DialogueRunner", "Node", preload("DialogueRunner.cs"), preload("res://addons/YarnSpinnerGodot/yarnproject/YarnProjectIcon.png"))

func _exit_tree():
	remove_control_from_bottom_panel(container)
	remove_import_plugin(import_plugin)
	import_plugin = null
	remove_custom_type("DialogueRunner")
	container.free()
