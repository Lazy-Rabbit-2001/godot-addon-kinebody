@icon("./kinebody2d.svg")
class_name KineBody2D
extends CharacterBody2D

## A type of [CharacterBody2D] specific to the development of platform games, with some features brought from [RigidBody2D].
##
## A [KineBody2D], or [b]KinematicBody2D[/b] in full, is a specific type for game developers working on platform games, or physics game lovers that want to use 
## a simpler and more easy-to-control edition of [RigidBody2D]. The name "kinematic" represents the way how the body is manipulated by the codes and how you (should) code the bahavior of this body.[br][br]
## 
## A [KineBody2D] contains a lot of properties and methods for easy deployment of a platform game character. You can set its gravity and make the body fall by calling [method move_kinebody].
## Also, you can call [method walking_speed_up], [method walking_slow_down_to_zero], and [method jump] to control the movement of your character.
## For physics lovers, you can also set the [member mass] of the body and affect the velocity by calling [method apply_momentum] or [method set_momentum].
## Meanwhile, you can call [method accelerate] to apply an acceleration to the body directly, if you prefer pure velocity control.[br][br]
##
## Although you will be benefic a lot from [KineBody2D], there are still something that you should be careful about.
## Because some calls, like [method jump], [method turn_wall], and [method bounce_jumping_falling] relys on [member CharacterBody2D.up_direction],
## the up direction will keep always being the opposite direction of the gravity. This would bring appearence incoherence: Imagine a character in a
## upside-down gravity space "standing" as if it were in the normal gravity space. Therefore, a new concept "rotation synchronization" is introduced to
## fix this problem. By rotating the body and make the global rotation fit to the up direction, the body will look reasonably.[br][br]
##
## Another thing worth noticing is that, when you call some methods to accelerate the body instance, bear in mind to multiply a [code]delta[/code] time to ensure
## the acceleration is time-independent. Otherwise, the result of acceleration will be incorrect.[br][br]
##
## [b]Note:[/b] During the high consumption of the [method CharacterBody2D.move_and_slide], it is not couraged to run the game with the overnumbered use of [KineBody2D].

## Definitions about the transformation method on [member motion_vector].
enum MotionVectorDirection {
	UP_DIRECTION, ## The direction of the [member motion_vector] equals to [code]up_direction.rotated(PI/2.0)[/code].
	GLOBAL_ROTATION, ## The direction of the [member motion_vector] is rotated by [member Node2D.global_rotation].
	DEFAULT, ## The [member motion_vector] is an alternative identifier of [member CharacterBody2D.velocity].
}

## Emitted when the body collides with the side of the other body.
signal collided_wall
## Emitted when the body collides with the bottom of the other body.
signal collided_ceiling
## Emitted when the body collides with the top of the other body.
signal collided_floor

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
## The maximum of falling speed. If set to [code]0[/code], there will be no limit on maximum falling speed and the body will keep falling faster and faster.
@export_range(0.0, 12500.0, 0.1, "or_greater", "hide_slider", "suffix:px/s") var max_falling_speed: float = 1500.0
@export_group("Rotation", "rotation_")
## Duration of the rotation synchronization. See [method synchronize_global_rotation_to_up_direction].
@export_range(0.0, 10.0, 0.1, "or_greater", "hide_slider", "suffix:s") var rotation_synchronizing_duration: float = 0.1

# Velocity in previous frame
var __prev_velocity: Vector2
# Tween for the rotation synchronization.
var __rotation_synchronizing_tween: Tween


#region == main physics methods ==
## Moves the kine body instance and returns [code]true[/code] when it collides with other physics bodies.[br][br]
## The [param speed_scale] will affect the final motion, 
## while the [param global_rotation_sync_up_direction] will synchronize [member Node2D.global_rotation] to [member CharacterBody2D.up_direction] by calling [method synchronize_global_rotation_to_up_direction].
func move_kinebody(speed_scale: float = 1.0, global_rotation_sync_up_direction: bool = true) -> bool:
	__prev_velocity = velocity
	
	var g := get_gravity()
	var gdir := g.normalized()
	
	# `up_direction` will not work in floating mode
	if motion_mode == MotionMode.MOTION_MODE_GROUNDED:
		if gdir != Vector2(NAN, NAN) and not gdir.is_zero_approx():
			up_direction = -gdir
	
	# Applying gravity
	if gravity_scale > 0.0:
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

	# Handling signal emissions
	if ret:
		if is_on_wall():
			collided_wall.emit()
		if is_on_ceiling():
			collided_ceiling.emit()
		if is_on_floor():
			collided_floor.emit()

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
		__rotation_synchronizing_tween.tween_property(self, ^"global_rotation", target_rotation, rotation_synchronizing_duration)
		# Waiting for the tween to finish.
		await __rotation_synchronizing_tween.finished
		# Clearing the tween reference to avoid memory leak.
		__rotation_synchronizing_tween.kill()
		__rotation_synchronizing_tween = null
#endregion

#region == helper physics methods ==
## Accelerates the body by the given [param acceleration].
func accelerate(acceleration: Vector2) -> void:
	velocity += acceleration

## Accelerates the body to the target velocity by the given [param acceleration].
func accelerate_to(acceleration: float, to: Vector2) -> void:
	velocity = velocity.move_toward(to, acceleration);

## Applies the given [param momentum] to the body.[br][br]
## Momentum is a vector that represents the multiplication of mass and velocity, so the more momentum applied, the faster the body will move.
## However, the more mass the body has, the less velocity it will have, with the same momentum applied.[br][br]
## For platform games, the momentum is manipulated more suitable than the force.
func apply_momentum(momentum: Vector2) -> void:
	velocity += momentum / mass

## Sets the momentum of the body to the given [param momentum]. See [method apply_momentum] for details about what is momentum.
func set_momentum(momentum: Vector2) -> void:
	velocity = momentum / mass

## Returns the momentum of the body. See [method apply_momentum] for details about what is momentum.
func get_momentum() -> Vector2:
	return velocity * mass

## Adds the motion vector by given acceleration.
func add_motion_vector(added_motion_vector: Vector2) -> void:
	motion_vector += added_motion_vector

## Adds the motion vector to the target motion vector by given acceleration.
func add_motion_vector_to(added_motion: float, to: Vector2) -> void:
	motion_vector = motion_vector.move_toward(to, added_motion)

## Adds the [code]x[/code] component of the motion vector by given acceleration to the target value.
## This is useful for fast achieving walking acceleration of a character's.
func add_motion_vector_x_speed_to(added_x_speed: float, to: float) -> void:
	motion_vector.x = move_toward(motion_vector.x, to, added_x_speed)

## Adds the [code]y[/code] component of the motion vector by given acceleration to the target value.
## This is useful for fast achieving jumping or falling acceleration of a character.
func add_motion_vector_y_speed_to(added_y_speed: float, to: float) -> void:
	motion_vector.y = move_toward(motion_vector.x, to, added_y_speed)

## Returns the friction the body receives when it is on the floor.[br][br]
## [b]Note:[/b] This method is a bit performance-consuming, as it uses [method PhysicsBody2D.test_move] which takes a bit more time to get the result. Be careful when using it frequently, if you are caring about performance.
func get_floor_friction() -> float:
	if !is_on_floor():
		return 0.0

	var friction := 0.0
	var kc := KinematicCollision2D.new()
	test_move(global_transform, -get_floor_normal(), kc)
	if kc and kc.get_collider():
		return PhysicsServer2D.body_get_param(kc.get_collider_rid(), PhysicsServer2D.BODY_PARAM_FRICTION)

	return friction

## Returns the velocity in previous frame.
func get_previous_velocity() -> Vector2:
	return __prev_velocity
#endregion

#region == platform game (wrapper) methods ==
## Makes the body jump along the up direction with the given [param jumping_speed].
## If [param accumulating] is [code]true[/code], the [param jumping_speed] will be added to the velocity directly. Otherwise, the component of the velocity along the up direction will be set to [param jumping_speed].
func jump(jumping_speed: float, accumulating: bool = false) -> void:
	velocity += up_direction * jumping_speed if accumulating else -velocity.project(up_direction) + up_direction * jumping_speed

## Reverses the velocity of the body, as if the body collided with a wall whose edge is parallel to the up direction of the body.
func turn_back_walk() -> void:
	velocity = velocity.reflect(up_direction) if __prev_velocity.is_zero_approx() else __prev_velocity.reflect(up_direction)

## Reverses the velocity of the body, as if the body collided with a ceiling or floor whose bottom or top is perpendicular to the up direction of the body.
func bounce_jumping_falling() -> void:
	velocity = velocity.bounce(up_direction) if __prev_velocity.is_zero_approx() else __prev_velocity.bounce(up_direction)

## A wrapper method of [method add_motion_vector_x_speed_to] for the convenience of platform games.
func walking_speed_up(acceleration: float, to: float) -> void:
	add_motion_vector_x_speed_to(acceleration, to)

## A wrapper method of [method add_motion_vector_x_speed_to], while the parameter [param to] is always [code]0[/code], for the convenience of platform games.
func walking_slow_down_to_zero(deceleration: float) -> void:
	add_motion_vector_x_speed_to(deceleration, 0)
#endregion

# Returns the time delta used in physics or idle iteration.
func __get_delta() -> float:
	return get_physics_process_delta_time() if Engine.is_in_physics_frame() else get_process_delta_time()
