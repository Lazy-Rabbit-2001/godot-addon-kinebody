extends KineBody3D


func _physics_process(_delta: float) -> void:
	set_walking_velocity(Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down") * 5)
	
	if Input.is_action_just_pressed("ui_accept") and is_on_floor():
		jump(3)
	
	print(get_up_direction_rotation_basis())
	
	move_kinebody()
