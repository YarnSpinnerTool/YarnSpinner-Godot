class_name YarnSpinner
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

	static func from_dictionary(data: Dictionary = {}) -> DialogueOption:
		if !data.has_all(["dialogue_option_id", "line"]):
			push_error("YarnSpinner GDScript: Can't create DialogueOption, incorrect or missing data")
			return

		var option := DialogueOption.new()
		option.dialogue_option_id = data["dialogue_option_id"]
		option.text_id = data["text_id"]
		option.line = LocalizedLine.from_dictionary(data["line"])
		option.is_available = data["is_available"]

		return option


## Represents a line, ready to be presented to the user in the localisation they have specified.
##
## Constructor takes a dictionary structured the same way as provided
## by the DialogueRunner for GDScript
## Mirrors YarnSpinnerGodot.LocalizedLine class
class LocalizedLine:
	var text: MarkupParseResult ## The underlying MarkupParseResult class for this line
	var text_without_character_name: MarkupParseResult ## The original text with the character attribute removed
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
				return name["properties"]["name"]

	static func from_dictionary(data: Dictionary = {}) -> LocalizedLine:
		if !data.has_all(["text", "metadata"]):
			push_error("YarnSpinner GDScript: Can't create LocalizedLine, incorrect or missing data")
			return

		var locline := LocalizedLine.new()
		locline.text = MarkupParseResult.from_dictionary(data["text"])
		locline.text_without_character_name = MarkupParseResult.from_dictionary(data["text_without_character_name"])
		locline.text_id = data["text_id"]
		locline.raw_text = data["raw_text"]
		locline.substitutions = data["substitutions"]
		locline.metadata = data["metadata"]

		return locline


## MarkupParseResult mirroring the MarkupParseResult values
##
## Constructor takes a dictionary structured the same way as provided
## by the DialogueRunner for GDScript's "text" value
## Similar to Yarn.Markup.MarkupParseResult
class MarkupParseResult:
	var text: String ## The original text, with all parsed markers removed.

	## List of attributes from the MarkupAttribute, in an array of dictionaries
	##
	## The dictionary is as follows:
	## "length": int - the number of text elements in the plain text that this attribute covers.
	## "name": String - the name of the attribute.
	## "position": int - the position in the plain text where this attribute begins.
	## "properties": Dictionary - the properties associated with this attribute.
	var attributes: Array

	static func from_dictionary(data: Dictionary = {}) -> MarkupParseResult:
		if !data.has_all(["text", "attributes"]):
			push_error("YarnSpinner GDScript: Can't create MarkupParseResult, incorrect or missing data")
			return

		var line := MarkupParseResult.new()
		line.text = data["text"]
		line.attributes = data["attributes"]

		return line

	## Returns the substring of Text covered by named attribute Position and Length properties.
	func text_for_attribute(name: String) -> String:
		var attr := try_get_attribute_with_name(name)

		if attr.is_empty():
			return ""
		elif attr["length"] <= 0 :
			return ""
		else:
			return text.substr(attr["position"], attr["length"])

	## Gets the first attribute with the specified name, if present.
	func try_get_attribute_with_name(name: String) -> Dictionary:
		for value: Dictionary in attributes:
			if value["name"] == (name): return value

		return {}


## Converts a dictionary array to an array of DialogueOption's
static func dialogue_options_from_array(data: Array) -> Array[DialogueOption]:
	if data.is_empty():
		push_error("YarnSpinner GDScript: Can't create DialogueOption Array, empty array")
		return []

	var return_data : Array[DialogueOption] = []

	for val in data:
		return_data.push_back(DialogueOption.from_dictionary(val))

	return return_data

