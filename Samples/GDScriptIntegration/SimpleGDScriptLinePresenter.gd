# Example of a simple custom dialogue view written in GDScript with 
# the help of a corresponding GDScriptViewAdapter node.s
extends Node

@export var continue_button : Button
@export var character_name_label : RichTextLabel
@export var line_text_label : RichTextLabel
@export var presenter_control: Node 

func _ready() -> void: 
	presenter_control.visible = false 

func on_dialogue_start_async() -> void:
	print("Dialogue started ")
	presenter_control.visible = true

func run_line_async(line: Dictionary) -> void:
	print('Line: ' + JSON.stringify(line))

	# line is a Dictionary converted from the LocalizedLine C# Class
	# converts to GDScript LocalizedLine
	var localized_line : GDYS.LocalizedLine = GDYS.LocalizedLine.new(line)
	
	var target_line: GDYS.Line
	if localized_line.character_name.is_empty():
		character_name_label.visible = false
		target_line = localized_line.text
	else:
		character_name_label.visible = true
		character_name_label.text = localized_line.character_name
		target_line = localized_line.text_without_character_name
	
	var output_line: String = target_line.text

	var fx_attribute = target_line.try_get_attribute_with_name("fx")
	if not fx_attribute.is_empty():
		for property in fx_attribute["properties"]:
			if property["type"] == "wave":
				output_line = output_line.insert(fx_attribute["position"] + fx_attribute["length"], "[/wave]")
				output_line = output_line.insert(fx_attribute["position"], "[wave]")

	line_text_label.text = output_line

	await continue_button.pressed

func dialogue_complete_async() -> void:
	print("Dialogue complete ")
	presenter_control.visible = false
