tool
extends EditorPlugin
# YarnSpinner-Godot editor plugin


# instances
var container
var project_import_plugin
var script_import_plugin
var editor_utility 
var popup
var popupName = "YarnSpinner"

var createYarnScriptID = 1 
func _enter_tree():
	# resources
	var project_importer_script = load("res://addons/YarnSpinnerGodot/editor/YarnProjectImporter.cs")
	var script_importer_script = load("res://addons/YarnSpinnerGodot/editor/YarnImporter.cs")
	var editor_utility_script = load("res://addons/YarnSpinnerGodot/Editor/YarnEditorUtility.cs")

	container = load("res://addons/YarnSpinnerGodot/editor/YarnSpinnerEditorContainer.tscn").instance()
	container.undoRedo = get_undo_redo()
	add_control_to_bottom_panel(container, "YarnSpinner")
	project_import_plugin = project_importer_script.new()
	script_import_plugin = script_importer_script.new()
	editor_utility = editor_utility_script.new()
	add_import_plugin(script_import_plugin);
	add_import_plugin(project_import_plugin);
	popup = PopupMenu.new()
	popup.add_item("Create Yarn Script", 1, createYarnScriptID)
	popup.connect("id_pressed", self, "on_popup_id_pressed", [], 0)
	add_tool_submenu_item(popupName, popup)
	add_custom_type("DialogueRunner", "Node", preload("res://addons/YarnSpinnerGodot/Runtime/DialogueRunner.cs"), preload("res://addons/YarnSpinnerGodot/Editor/Icons/YarnSpinnerLogo.png"))

func _exit_tree():
	remove_control_from_bottom_panel(container)
	remove_import_plugin(project_import_plugin)
	remove_import_plugin(script_import_plugin)
	project_import_plugin = null
	remove_custom_type("DialogueRunner")
	if container != null:
		container.free()
	remove_tool_menu_item(popupName)

func on_popup_id_pressed(id):
	if id == createYarnScriptID:
		create_yarn_script()

func create_yarn_script():
	print("Opening 'create yarn script' menu")
	var dialog = EditorFileDialog.new()
	dialog.add_filter("*.yarn ; Yarn Script")
	dialog.mode = FileDialog.MODE_SAVE_FILE
	dialog.window_title = "Create a new Yarn Script"
	dialog.connect("file_selected", self, "create_yarn_script_destination_selected")
	get_editor_interface().get_editor_viewport().add_child(dialog)
	dialog.popup((Rect2(0,0, 700, 500)))
	
func create_yarn_script_destination_selected(destination):
	print("Creating a yarn script at " + destination)
	editor_utility.CreateYarnScript(destination)
