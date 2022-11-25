tool
extends EditorPlugin
# YarnSpinner-Godot editor plugin

# resources
var project_importer_script = preload("res://addons/YarnSpinnerGodot/editor/YarnProjectImporter.cs")
var script_importer_script = preload("res://addons/YarnSpinnerGodot/editor/YarnImporter.cs")
var editor_utility_script = preload("res://addons/YarnSpinnerGodot/Editor/YarnEditorUtility.cs")

# instances
var container
var project_import_plugin
var script_import_plugin
var editor_utility 

func _enter_tree():
	container = preload("res://addons/YarnSpinnerGodot/editor/YarnSpinnerEditorContainer.tscn").instance()
	container.undoRedo = get_undo_redo()
	add_control_to_bottom_panel(container, "YarnSpinner")
	project_import_plugin = project_importer_script.new()
	script_import_plugin = script_importer_script.new()
	editor_utility = editor_utility_script.new()
	add_import_plugin(script_import_plugin);
	add_import_plugin(project_import_plugin);
	add_custom_type("DialogueRunner", "Node", preload("res://addons/YarnSpinnerGodot/Runtime/DialogueRunner.cs"), preload("res://addons/YarnSpinnerGodot/Editor/Icons/YarnSpinnerLogo.png"))
	add_tool_menu_item("Yarn Spinner/Create Yarn Script", editor_utility, "CreateYarnAsset",null)
	
func _exit_tree():
	remove_control_from_bottom_panel(container)
	remove_import_plugin(project_import_plugin)
	remove_import_plugin(script_import_plugin)
	project_import_plugin = null
	remove_custom_type("DialogueRunner")
	container.free()
	remove_tool_menu_item("Yarn Spinner/Create Yarn Script")
