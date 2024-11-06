extends Camera3D

@onready var kine_body_3d: CharacterBody3D = $"../KineBody3D"


func _process(_delta: float) -> void:
	global_position = kine_body_3d.global_position
	global_position.z = kine_body_3d.global_position.z + 32
	global_rotation.z = kine_body_3d.global_rotation.z
