extends Node

@export var DialogueRunner : Node
# Called when the node enters the scene tree for the first time.
func _ready():
	print(' Project path ' +DialogueRunner.yarnProject.resource_path)
	DialogueRunner.StartDialogue('GDScriptSample')
	DialogueRunner.AddCommandHandlerCallable("string_arg", string_arg_command)
	DialogueRunner.AddCommandHandlerCallable("no_type_arg_command", no_type_arg_command)
	DialogueRunner.AddCommandHandlerCallable("no_args", no_args_command)
	DialogueRunner.AddCommandHandlerCallable("async", async_command)
	DialogueRunner.AddCommandHandlerCallable("async_six_args", async_six_args_command)
	DialogueRunner.AddCommandHandlerCallable("int_arg_command", int_arg_command)
	
func no_args_command():
	print('No args in this one!')

func no_type_arg_command(arg1):
	print("The arg is this: '" + str(arg1) +  "'")
	
func int_arg_command(arg1 : int):
	print('The int is: ' + str(arg1))
	
func async_command(on_complete: Callable):
	print('Before timeout')
	await get_tree().create_timer(1.0).timeout
	print("Still working on the async command...")
	await get_tree().create_timer(2.0).timeout
	print("After timeout")
	on_complete.call()

func async_six_args_command(arg1: String, arg2: int, arg3: String, 
	arg4: String, arg5: bool, arg6: String, on_complete: Callable):
	print('Character ' + arg1 + ' intends to pick ' +str(arg2) + ' ' + arg3 + ' in the ' + arg4)
	await get_tree().create_timer(2.0).timeout
	print('Picked. Intended for sale?: ' + str(arg5) + '. Planned preparation method: ' + arg6)
	on_complete.call()
		
func string_arg_command(my_str_arg : String):
	print("The string arg is " + my_str_arg)
