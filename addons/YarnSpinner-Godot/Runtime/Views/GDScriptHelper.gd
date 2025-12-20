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

	## creates a dictionary formatted the same way from DialogueRunner's GDScript run_options_async
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

	## The original text with the character attribute removed
	var text_without_character_name: MarkupParseResult:
		get:
			var name := text.try_get_attribute_with_name("character")
			if name.is_empty():
				return text
			else:
				var characterless := text.delete_range("character")
				characterless.attributes.append(name)
				return characterless

	## creates a dictionary formatted the same way from DialogueRunner's GDScript run_line_async
	static func from_dictionary(data: Dictionary = {}) -> LocalizedLine:
		if !data.has_all(["text", "metadata"]):
			push_error("YarnSpinner GDScript: Can't create LocalizedLine, incorrect or missing data")
			return

		var locline := LocalizedLine.new()
		locline.text = MarkupParseResult.from_dictionary(data["text"])
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

	## creates a dictionary formatted the same way from DialogueRunner's GDScript run_line_async
	static func from_dictionary(data: Dictionary = {}) -> MarkupParseResult:
		if !data.has_all(["text", "attributes"]):
			push_error("YarnSpinner GDScript: Can't create MarkupParseResult, incorrect or missing data")
			return

		var line := MarkupParseResult.new()
		line.text = data["text"]
		line.attributes = data["attributes"]

		return line

	## Deletes an attribute from this markup.
	##
	## This method deletes the range of text covered by the name of attr_name_to_delete, and updates the other attributes in this markup as follows:
	## - Attributes that start and end before the deleted attribute are unmodified.
	## - Attributes that start before the deleted attribute and end inside it are truncated to remove the part overlapping the deleted attribute.
	## - Attributes that have the same position and length as the deleted attribute are deleted, if they apply to any text.
	## - Attributes that start and end within the deleted attribute are deleted.
	## - Attributes that start within the deleted attribute, and end outside it, have their start truncated to remove the part overlapping the deleted attribute.
	## - Attributes that start after the deleted attribute have their start point adjusted to account for the deleted text.
	## - This method does not modify the current object. A new is returned.
	##
	## If attr_name_to_delete is not an attribute of this, the behaviour is undefined.
	func delete_range(attr_name_to_delete: String) -> MarkupParseResult:
		if text.is_empty() or attributes.is_empty():
			push_error("YarnSpinner GDScript: Markup parse result does not have any attributes")
			return null

		var attr_to_delete := try_get_attribute_with_name(attr_name_to_delete)
		if attr_to_delete.is_empty():
			push_error("YarnSpinner GDScript: attribute with name %s does not exist" % attr_name_to_delete)
			return null

		var new_result := MarkupParseResult.new()

		# the attribute length is 0
		if attr_to_delete["length"] == 0:
			for a: Dictionary in attributes:
				if a != attr_to_delete:
					new_result.attributes.append(a)

			new_result.text = text
			return new_result

		var deletion_start: int = attr_to_delete["position"]
		var deletion_length: int = attr_to_delete["length"]
		var deletion_end := deletion_start + deletion_length
		var edited_substring := text.erase(deletion_start, deletion_length)

		for existing_attr: Dictionary in attributes:
			var start: int = existing_attr["position"]
			var end: int = existing_attr["position"] + existing_attr["length"]

			if existing_attr == attr_to_delete:
				# don't include the attribute we're trying to delete
				continue

			var edited_attr := existing_attr.duplicate_deep()

			if start <= deletion_start:
				if end <= deletion_start:
					pass
				elif end <= deletion_end:
					edited_attr["length"] = deletion_start - start
					if existing_attr["length"] > 0 && edited_attr["length"] <= 0:
						continue
				else:
					edited_attr["length"] -= deletion_length
			elif start >= deletion_end:
				edited_attr["position"] = start - deletion_length
			elif start >= deletion_start && end <= deletion_end:
				continue
			elif start >= deletion_start && end > deletion_end:
				var overlap_len := deletion_end - start
				var new_start := deletion_start
				var new_len: int = existing_attr["length"] - overlap_len

				edited_attr["position"] = new_start
				edited_attr["length"] = new_len

			new_result.attributes.append(edited_attr)

		new_result.text = edited_substring
		return new_result

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


## Converts a dictionary array to an array of DialogueOptions produced by DialogueRunner's run_options_async
static func dialogue_options_from_array(data: Array) -> Array[DialogueOption]:
	if data.is_empty():
		push_error("YarnSpinner GDScript: Can't create DialogueOption Array, empty array")
		return []

	var return_data : Array[DialogueOption] = []

	for val in data:
		return_data.push_back(DialogueOption.from_dictionary(val))

	return return_data

