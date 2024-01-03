extends Node

@export var dialogue_runner : Node
# Called when the node enters the scene tree for the first time.
func _ready():
	var yarn_project = dialogue_runner.yarnProject
	print(' Project path ' + yarn_project.resource_path)
	dialogue_runner.StartDialogue('GDScriptSample')
	
	# Demonstrate registering a variety of commands that will 
	# be used in our story.
	dialogue_runner.AddCommandHandlerCallable("string_arg", string_arg_command)
	dialogue_runner.AddCommandHandlerCallable("no_type_arg_command", no_type_arg_command)
	dialogue_runner.AddCommandHandlerCallable("no_args", no_args_command)
	dialogue_runner.AddCommandHandlerCallable("async", async_command)
	dialogue_runner.AddCommandHandlerCallable("async_six_args", async_six_args_command)
	dialogue_runner.AddCommandHandlerCallable("int_arg_command", int_arg_command)
	
func no_args_command():
	print('No args in this one!')

func no_type_arg_command(arg1):
	# This command doesn't use a type hint like :int, so YarnSpinner will assume
	# that the type is String.
	print("The arg is this: '" + str(arg1) +  "'")
	
func int_arg_command(arg1 : int):
	print('The int is: ' + str(arg1))
	
func async_command(on_complete: Callable):
	# We added a Callable as the last argument to this command handler,
	# which means that YarnSpinner will wait until on_complete is called.
	# Callables are only supported as the last argument to a command.
	print('Before timeout')
	await get_tree().create_timer(1.0).timeout
	print("Still working on the async command...")
	await get_tree().create_timer(2.0).timeout
	print("After timeout")
	on_complete.call()

func async_six_args_command(arg1: String, arg2: float, arg3: String, 
	arg4: String, arg5: bool, arg6: String, on_complete: Callable):
	# This is similar to async_command, but it takes more arguments
	# before the on_complete callback.
	print('Character ' + arg1 + ' intends to pick ' +str(arg2) + ' kilograms of ' + arg3 + ' in the ' + arg4)
	await get_tree().create_timer(2.0).timeout
	print('Picked. Intended for sale?: ' + str(arg5) + '. Planned preparation method: ' + arg6)
	on_complete.call()
		
func string_arg_command(my_str_arg : String):
	print("The string arg is " + my_str_arg)
