tool
extends EditorPlugin

var c_sharp_script_resource = null
var plugin = null 
var c_sharp_script_path =  "res://addons/YarnSpinnerGodot/YarnSpinnerGodotMainPlugin.cs"
var loaded = false

func print_load_error():
	push_error("Unable to load the script " + c_sharp_script_path + 
			". Please build your c# solution to use the YarnSpinner plugin.")
func try_load_plugin():
	if c_sharp_script_resource == null:
		c_sharp_script_resource = load(c_sharp_script_path)
		if c_sharp_script_resource == null or not c_sharp_script_resource.is_tool():
			print_load_error()
		else:
			plugin = c_sharp_script_resource.new()
			if plugin != null:
				plugin.Load(self)
				loaded = true
				print("Loaded the YarnSpinner-Godot plugin.")
				
			
func apply_changes():
	try_load_plugin()

func build():
	try_load_plugin()
	if not loaded:
		print_load_error()
	return true
	
func _enter_tree():
	# Initialization of the plugin goes here.
	try_load_plugin()

