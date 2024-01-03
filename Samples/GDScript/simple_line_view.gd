extends Node

@export var line_text : RichTextLabel
@export var character_name_text : RichTextLabel

# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass

func dialogue_started():
	print("Dialogue was started on our view")
	
func run_line( dialogue_line : LocalizedLine, 
			   on_dialogue_line_finished: Callable):
	print("Running line " + dialogue_line.TextID)
	line_text.text = dialogue_line.RawText
	await get_tree().create_timer(5).timeout
	on_dialogue_line_finished.call()
	
func run_options(dialogueOptions: Array, 
				 on_option_selected: Callable):
	print("Selecting option 0")
	on_option_selected.call(0)
	
func dialogue_complete():
	print("Dialogue complete on simple line view")

func user_requested_view_advancement():
	print("User requested view advancement")
