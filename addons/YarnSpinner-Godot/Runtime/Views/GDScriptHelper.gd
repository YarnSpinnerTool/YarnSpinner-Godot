class_name GDYS
## Helper class supporting C# to GDScript functionality
##
## This helper class creates GDScript versions of classes and other helper functions
## to have parity with the YarnSpinner C# API.

## Represents a dialogue option, ready to be presented to the user
##
## Constructor takes a dictionary structured the same way as provided
## by the DialogueRunner for GDScript
## Mirrors YarnSpinnerGodot.DialogueOption
class DialogueOption:
	var dialogue_option_id: int ## The ID of this dialogue option
	var text_id: String ## The ID of the dialogue option's text
	var line: LocalizedLine ## The line for this dialogue option
	var is_available: bool ## Indicates whether this value should be presented as available

	func _init(data: Dictionary = {}) -> void:
		if data.is_empty(): return
		if !data.has_all(["dialogue_option_id", "line"]):
			push_error("YarnSpinner GDScript: Can't create DialogueOption, incorrect or missing data")
			return

		dialogue_option_id = data["dialogue_option_id"]
		text_id = data["text_id"]
		line = LocalizedLine.new(data["line"])
		is_available = data["is_available"]


## Represents a line, ready to be presented to the user in the localisation they have specified.
##
## Constructor takes a dictionary structured the same way as provided
## by the DialogueRunner for GDScript
## Mirrors YarnSpinnerGodot.LocalizedLine class
class LocalizedLine:
	var text: Line ## The underlying Line class for this line
	var text_without_character_name: Line ## The original text with the character attribute removed
	var text_id: String ## DialogueLine's ID
	var raw_text: String ## DialogueLine's unparsed text
	var substitutions: Array ## DialogueLine's inline expression's substitution
	var metadata: Array ## Any metadata associated with this line.

	## The name of the character, if present
	var character_name: String:
		get:
			var name = text.try_get_attribute_with_name("character")
			if name.is_empty():
				return ""
			else:
				return name["properties"][0]["name"]

	func _init(data: Dictionary = {}) -> void:
		if data.is_empty(): return
		if !data.has_all(["text", "metadata"]):
			push_error("YarnSpinner GDScript: Can't create LocalizedLine, incorrect or missing data")
			return

		text = Line.new(data["text"])
		text_without_character_name = Line.new(data["text_without_character_name"])
		text_id = data["text_id"]
		raw_text = data["raw_text"]
		substitutions = data["substitutions"]
		metadata = data["metadata"]


## Line mirroring the MarkupParseResult values
##
## Constructor takes a dictionary structured the same way as provided
## by the DialogueRunner for GDScript's "text" value
## Similar to Yarn.Markup.MarkupParseResult
class Line:
	var text: String ## The original text, with all parsed markers removed.

	## List of attributes from the MarkupAttribute, in an array of dictionaries
	##
	## The dictionary is as follows:
	## "length": int - the number of text elements in the plain text that this attribute covers.
	## "name": String - the name of the attribute.
	## "position": int - the position in the plain text where this attribute begins.
    ## "properties": Dictionary - the properties associated with this attribute.
	var attributes: Array

	func _init(data: Dictionary = {}) -> void:
		if data.is_empty(): return
		if !data.has_all(["text", "attributes"]):
			push_error("YarnSpinner GDScript: Can't create LocalizedLine.Line, incorrect or missing data")
			return

		text = data["text"]
		attributes = data["attributes"]

	func text_for_attribute(name: String) -> String:
		var attr := try_get_attribute_with_name(name)

		if attr.is_empty():
			return ""
		elif attr["length"] <= 0 :
			return ""
		else:
			return text.substr(attr["position"], attr["length"])

	func try_get_attribute_with_name(name: String) -> Dictionary:
		for value: Dictionary in attributes:
			if value["name"] == (name): return value

		return {}


## Converts a dictionary array to an array of DialogueOption's
static func new_dialogue_option_array(data: Array) -> Array[DialogueOption]:
	if data.is_empty():
		push_error("YarnSpinner GDScript: Can't create DialogueOption Array, empty array")
		return []

	var return_data : Array[DialogueOption] = []

	for val in data:
		return_data.push_back(DialogueOption.new(val))

	return return_data

