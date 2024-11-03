extends KineBody2D


func _physics_process(_delta: float) -> void:
	var t := Time.get_ticks_usec()
	move_kinebody()
	print(Time.get_ticks_usec() - t)
