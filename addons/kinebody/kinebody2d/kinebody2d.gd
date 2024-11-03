@icon("./KineBody2D.svg")
class_name KineBody2D
extends CharacterBody2D

## A type of [CharacterBody2D] specific to the development of platform games.
##
##

## Definitions about the transformation method on [member motion_vector].
enum MotionVectorDirection {
	UP_DIRECTION, ## The direction of the [member motion_vector] equals to [code]up_direction.rotated(PI/2.0)[/code].
	GLOBAL_ROTATION, ## The direction of the [member motion_vector] is rotated by [member Node2D.global_rotation].
	DEFAULT, ## The [member motion_vector] is an alternative identifier of [member CharacterBody2D.velocity].
}

## The mass of the body, which will affect the impulse that will be applied to the body.
@export_range(0.1, 99999.0, 0.1, "or_greater", "hide_slider", "suffix:kg") var mass: float = 1.0:
	set(value):
		PhysicsServer2D.body_set_param(get_rid(), PhysicsServer2D.BODY_PARAM_MASS, maxf(0.001, value))
	get:
		return PhysicsServer2D.body_get_param(get_rid(), PhysicsServer2D.BODY_PARAM_MASS)
## The option that defines which transformation method will be applied to [member motion_vector].
@export var motion_vector_direction: MotionVectorDirection = MotionVectorDirection.UP_DIRECTION
## The [member CharacterBody2D.velocity] of the body, transformed by a specific method defined by [member motion_vector_direction].
@export_custom(PROPERTY_HINT_NONE, "suffix: px/s") var motion_vector: Vector2:
	set(value):
		match (motion_vector_direction):
			MotionVectorDirection.DEFAULT:
				velocity = value
			MotionVectorDirection.UP_DIRECTION:
				velocity = value.rotated(up_direction.angle() + PI / 2.0)
			MotionVectorDirection.GLOBAL_ROTATION:
				velocity = value.rotated(global_rotation)
	get:
		match (motion_vector_direction):
			MotionVectorDirection.UP_DIRECTION:
				return velocity.rotated(-up_direction.angle() - PI / 2.0)
			MotionVectorDirection.GLOBAL_ROTATION:
				return velocity.rotated(-global_rotation)
		return velocity
## The scale of the gravity acceleration. The actual gravity acceleration is calculated as [code]gravity_scale * get_gravity[/code].
@export_range(0.0, 999.0, 0.1, "or_greater", "hide_slider", "suffix:x") var gravity_scale: float = 1.0
## The maximum
@export_range(0.0, 12500.0, 0.1, "or_greater", "hide_slider", "suffix:px/s") var max_falling_speed: float = 1500.0
@export_group("Rotation", "rotation_")
@export_range(0.0, 10.0, 0.1, "or_greater", "hide_slider", "suffix:s") var rotation_synchronizing_duration: float = 0.1

var __rotation_synchronizing_tween: Tween


## Moves the kine body instance and returns [code]true[/code] when it collides with other physics bodies.[br][br]
## The [param speed_scale] will affect the final motion, 
## while the [param global_rotation_sync_up_direction] will synchronize [member Node2D.global_rotation] to [member CharacterBody2D.up_direction] by calling [method synchronize_global_rotation_to_up_direction].
func move_kinebody(speed_scale: float = 1.0, global_rotation_sync_up_direction: bool = true) -> bool:
	# In general build, using inner class
	var g := get_gravity()
	var gdir := g.normalized()
	
	# `up_direction` will not work in floating mode
	if motion_mode == MotionMode.MOTION_MODE_GROUNDED:
		if gdir != Vector2(NAN, NAN) and not gdir.is_zero_approx():
			up_direction = gdir
	
	# Applying gravity
	if not is_on_floor() and gravity_scale > 0.0:
		velocity += g * gravity_scale * __get_delta()
		var fv := velocity.project(gdir) # Falling velocity
		if max_falling_speed > 0.0 and fv != Vector2(NAN, NAN) and fv.dot(gdir) > 0.0 and fv.length_squared() > max_falling_speed ** 2.0:
			velocity -= fv - fv.normalized() * max_falling_speed

	# Synchronizing global rotation to up direction
	if global_rotation_sync_up_direction:
		synchronize_global_rotation_to_up_direction()

	# Applying speed scale
	var tmp_v := velocity # Stored temporary previous velocity, used to recovery
	velocity *= speed_scale
	var ret := move_and_slide()
	velocity = tmp_v
	return ret

## Synchronizes [member Node2D.global_rotation] to [member CharacterBody2D.up_direction],
## that is to say, the global rotation of the body will be synchronized to [code]up_direction.angle() + PI / 2.0[/code].[br][br]
## [b]Note:[/b] This is achieved by creating an object [Tween], which may take more space of memory. Make sure to call this method within certain instances.
func synchronize_global_rotation_to_up_direction() -> void:
	if motion_mode != MotionMode.MOTION_MODE_GROUNDED:
		return # Non-ground mode does not support [member up_direction].
	var target_rotation := up_direction.angle() + PI / 2.0
	if is_equal_approx(global_rotation, target_rotation):
		global_rotation = target_rotation
	elif not __rotation_synchronizing_tween:
		# Creating a new tween for rotation synchronization.
		__rotation_synchronizing_tween = create_tween().set_trans(Tween.TRANS_SINE)
		__rotation_synchronizing_tween.tween_property(self, ^"global_position", target_rotation, rotation_synchronizing_duration)
		# Waiting for the tween to finish.
		await __rotation_synchronizing_tween.finished
		# Clearing the tween reference to avoid memory leak.
		__rotation_synchronizing_tween.kill()
		__rotation_synchronizing_tween = null


# Returns the time delta used in physics or idle iteration.
func __get_delta() -> float:
	return get_physics_process_delta_time() if Engine.is_in_physics_frame() else get_process_delta_time()
