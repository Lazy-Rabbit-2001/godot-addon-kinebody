extends KineBody3D

func _physics_process(delta: float) -> void:
	set_walking_velocity(Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down") * 480.0 * delta)
	
	if Input.is_action_just_pressed("ui_accept") and is_on_floor():
		jump(120.0 * delta)
	
	move_kinebody()
