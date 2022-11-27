tool
extends EditorPlugin
# YarnSpinner-Godot editor plugin


# instances
var container
var script_import_plugin
var project_inspector_plugin
var editor_utility 
var popup
var popupName = "YarnSpinner"

var createYarnScriptID = 1 
var createYarnProjectID = 2 

func _enter_tree():
	# resources
	var script_importer_script = load("res://addons/YarnSpinnerGodot/editor/YarnImporter.cs")
	var editor_utility_script = load("res://addons/YarnSpinnerGodot/Editor/YarnEditorUtility.cs")
	var project_inspector_script = load("res://addons/YarnSpinnerGodot/Editor/YarnProjectInspectorPlugin.cs")
	container = load("res://addons/YarnSpinnerGodot/editor/YarnSpinnerEditorContainer.tscn").instance()
	container.undoRedo = get_undo_redo()
	add_control_to_bottom_panel(container, "YarnSpinner")
	script_import_plugin = script_importer_script.new()
	editor_utility = editor_utility_script.new()
	project_inspector_plugin = project_inspector_script.new()
	add_inspector_plugin(project_inspector_plugin)
	add_import_plugin(script_import_plugin);
	popup = PopupMenu.new()
	popup.add_item("Create Yarn Script", createYarnScriptID)
	popup.add_item("Create Yarn Project", createYarnProjectID)
	popup.connect("id_pressed", self, "on_popup_id_pressed", [], 0)
	add_tool_submenu_item(popupName, popup)
	add_custom_type("DialogueRunner", "Node", preload("res://addons/YarnSpinnerGodot/Runtime/DialogueRunner.cs"), preload("res://addons/YarnSpinnerGodot/Editor/Icons/YarnSpinnerLogo.png"))

func _exit_tree():
	remove_control_from_bottom_panel(container)
	remove_import_plugin(script_import_plugin)
	remove_custom_type("DialogueRunner")
	remove_inspector_plugin(project_inspector_plugin)
	if container != null:
		container.free()
	remove_tool_menu_item(popupName)

func on_popup_id_pressed(id):
	if id == createYarnScriptID:
		create_yarn_script()
	if id == createYarnProjectID:
		create_yarn_project()

func create_yarn_script():
	print("Opening 'create yarn script' menu")
	var dialog = EditorFileDialog.new()
	dialog.add_filter("*.yarn ; Yarn Script")
	dialog.mode = FileDialog.MODE_SAVE_FILE
	dialog.window_title = "Create a new Yarn Script"
	dialog.connect("file_selected", self, "create_yarn_script_destination_selected")
	get_editor_interface().get_editor_viewport().add_child(dialog)
	dialog.popup((Rect2(50,50, 700, 500)))
	
func create_yarn_script_destination_selected(destination):
	print("Creating a yarn script at " + destination)
	editor_utility.CreateYarnScript(destination)

func create_yarn_project():
	print("Opening 'create yarn project' menu")
	var dialog = EditorFileDialog.new()
	dialog.add_filter("*.tres; Yarn Project")
	dialog.mode = FileDialog.MODE_SAVE_FILE
	dialog.window_title = "Create a new Yarn Project"
	dialog.connect("file_selected", self, "create_yarn_project_destination_selected")
	get_editor_interface().get_editor_viewport().add_child(dialog)
	dialog.popup((Rect2(50,50, 700, 500)))
	
func create_yarn_project_destination_selected(destination):
	print("Creating a yarn project at " + destination)
	editor_utility.CreateYarnProject(destination)

	
