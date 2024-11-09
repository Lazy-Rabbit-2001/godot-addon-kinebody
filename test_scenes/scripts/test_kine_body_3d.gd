extends KineBody3D

func _physics_process(_delta: float) -> void:
	set_walking_velocity(Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down") * 10.0)
	
	if Input.is_action_just_pressed("ui_accept") and is_on_floor():
		jump(10.0)
	
	move_kinebody()
