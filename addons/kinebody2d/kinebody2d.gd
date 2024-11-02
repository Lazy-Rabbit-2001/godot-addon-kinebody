class_name KineBody2D
extends CharacterBody2D

##
##
##

# Default value of [member rotation_snapping_lerp_speed_rad]
const __DEFAULT_ROTATION_SNAPPING_LERP_SPEED: float = PI / 3.0

## Definitions about the transformation method on [member motion_vector].
enum MotionVectorDirection {
	MOTION_VECTOR_DIRECTION_UP_DIRECTION, ## The direction of the [member motion_vector] equals to [code]up_direction.rotated(PI/2.0)[/code].
	MOTION_VECTOR_DIRECTION_GLOBAL_ROTATION, ## The direction of the [member motion_vector] is rotated by [member Node2D.global_rotation].
	MOTION_VECTOR_DIRECTION_DEFAULT, ## The [member motion_vector] is an alternative identifier of [member CharacterBody2D.velocity].
}

## The mass of the body, which will affect the impulse that will be applied to the body.
@export_range(0.0, 99999.0, 0.1, "or_greater", "hide_slider", "suffix:kg") var mass: float = 1.0:
	set(value):
		PhysicsServer2D.body_set_param(get_rid(), PhysicsServer2D.BODY_PARAM_MASS, value)
	get:
		return PhysicsServer2D.body_get_param(get_rid(), PhysicsServer2D.BODY_PARAM_MASS)
## The option that defines which transformation method will be applied to [member motion_vector].
@export var motion_vector_direction: MotionVectorDirection = MotionVectorDirection.MOTION_VECTOR_DIRECTION_UP_DIRECTION
## The [member CharacterBody2D.velocity] of the body, transformed by a specific method defined by [member motion_vector_direction].
@export_custom(PROPERTY_HINT_NONE, "suffix: px/s") var motion_vector: Vector2:
	set(value):
		match (motion_vector_direction):
			MotionVectorDirection.MOTION_VECTOR_DIRECTION_DEFAULT:
				velocity = value
			MotionVectorDirection.MOTION_VECTOR_DIRECTION_UP_DIRECTION:
				velocity = value.rotated(up_direction.angle() + PI / 2.0)
			MotionVectorDirection.MOTION_VECTOR_DIRECTION_GLOBAL_ROTATION:
				velocity = value.rotated(global_rotation)
	get:
		match (motion_vector_direction):
			MotionVectorDirection.MOTION_VECTOR_DIRECTION_UP_DIRECTION:
				return velocity.rotated(-up_direction.angle() - PI / 2.0)
			MotionVectorDirection.MOTION_VECTOR_DIRECTION_GLOBAL_ROTATION:
				return velocity.rotated(-global_rotation)
		return velocity
## The scale of the gravity acceleration. The actual gravity acceleration is calculated as [code]gravity_scale * get_gravity[/code].
@export_range(0.0, 999.0, 0.1, "or_greater", "hide_slider", "suffix:x") var gravity_scale: float = 1.0
## The maximum
@export_range(0.0, 12500.0, 0.1, "or_greater", "hide_slider", "suffix:px/s") var max_falling_speed: float = 1500.0
@export_group("Rotation", "rotation_")
@export_range(0.0, 999.0, 0.1, "or_greater", "hide_slider", "suffix:°/s") var rotation_snapping_lerp_speed_degrees: float = rad_to_deg(__DEFAULT_ROTATION_SNAPPING_LERP_SPEED):
	set(value):
		rotation_snapping_lerp_speed_rad = deg_to_rad(rotation_snapping_lerp_speed_degrees)
	get:
		return rad_to_deg(rotation_snapping_lerp_speed_rad)

## The speed of rotation in rads, when the current body is synchronizing its [member Node2D.global_rotation] to [member CharacterBody2D.up_direction]
var rotation_snapping_lerp_speed_rad: float = __DEFAULT_ROTATION_SNAPPING_LERP_SPEED


## Moves the body
func move_kinebody(speed_scale: float = 1.0) -> bool:
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
	
	# Applying speed scale
	var tmp_v := velocity # Stored temporary previous velocity, used to recovery
	velocity *= speed_scale
	var ret := move_and_slide()
	velocity = tmp_v
	return ret


func __get_delta() -> float:
	return get_physics_process_delta_time() if Engine.is_in_physics_frame() else get_process_delta_time()
