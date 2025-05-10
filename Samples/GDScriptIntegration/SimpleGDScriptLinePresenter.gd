# Example of a simple custom dialogue view written in GDScript with 
# the help of a corresponding GDScriptViewAdapter node.s
extends Node


@export var continue_button : Button
@export var character_name_label : RichTextLabel
@export var line_text_label : RichTextLabel
@export var presenter_control: Node 

var line_finished: bool 

func _ready() -> void: 
	presenter_control.visible = false 
	
func on_dialogue_start_async() -> void:
	print("Dialogue started ")
	
	
func run_line_async(line: Dictionary) -> void:
	# line is a Dictionary converted from the LocalizedLine C# Class
	# example: 
	# {"metadata":["my_metadata"],
	# "text":
		#  {
		#  "attributes":[
		#   	{ "length":8,"name":"fx","position":20,"properties":[{"type":"wave"}]},
		#   	{ "length":6,"name":"character","position":0,"properties":[{"name":"Gary"}]}
		#   ],
		#   "text":"Gary: So, can I use GDScript with YarnSpinner?",
		#   "text_without_character_name":"So, can I use GDScript with YarnSpinner?"
		#  }
	#  }
	presenter_control.visible = true
	line_finished = false
	print('Line: ' + JSON.stringify(line))
	continue_button.pressed.connect(continue_line)
	
	line_text_label.text = line["text"]["text_without_character_name"]
	var character_name : String = ""
	var character_name_offset: int = 0 
	for attribute: Dictionary in line["text"]["attributes"]:
		if attribute["name"] == "character":
			character_name = attribute["properties"][0]["name"]
			character_name_offset = line["text"]["text"].find(line["text"]["text_without_character_name"])
			break
	for attribute: Dictionary in line["text"]["attributes"]:
		# Example of using YarnSpinner markup like [fx type="wave"] in the yarn script.
		if attribute["name"] == "fx": 
			for property in attribute["properties"]:
				if property["type"] == "wave":
					var wave_begin = "[wave]"
					line_text_label.text = line_text_label.text.insert(attribute["position"] - character_name_offset, wave_begin)
					line_text_label.text = line_text_label.text.insert(attribute["position"] - character_name_offset + len(wave_begin) + attribute["length"], "[/wave]")
	character_name_label.text = character_name
	if not (character_name):
		character_name_label.visible = false 
	
	while !line_finished:
		await get_tree().process_frame
		
func continue_line() -> void:
	continue_button.pressed.disconnect(continue_line)
	line_finished = true

func dialogue_complete_async() -> void:
	print("Dialogue complete ")
	presenter_control.visible = false
