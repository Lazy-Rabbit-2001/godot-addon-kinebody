using Godot;
using System;
using System.Threading.Tasks;

namespace Godot;

/// <summary>
/// C# edition of <c>KineBody2D</c>.<br/>
/// <b>Note:</b> During the high consumption of the <c>CharacterBody2D.MoveAndSlide()</c>, it is not couraged to run with the overnumbered use of <c>KineBody2DCs</c>.
/// </summary>
[Tool]
[GlobalClass, Icon("res://addons/kinebody/kinebody2d/kinebody2d_csharp.svg")]
public partial class KineBody2DCs : CharacterBody2D
{
    /// <summary>
    /// Definitions about the transformation method on <c>MotionVector</c>.<br/>
    /// <c>UpDirection</c>: The direction of the <c>MotionVector</c> equals to <c>UpDirection.Rotated(Math.PI / 2.0d)</c>.<br/>
    /// <c>GlobalRotation</c>: The direction of the <c>MotionVector</c> is rotated by <c>GlobalRotation</c>.<br/>
    /// <c>Default</c>: The <c>MotionVector</c> is an alternative identifier of <c>CharacterBody2D.Velocity</c>.
    /// </summary>
    /// <seealso cref="MotionVector"/>
    public enum MotionVectorDirectionEnum
    {
        UpDirection,
        GlobalRotation,
        Default,
    }

    /// <summary>
    /// Emitted when the body collides with the side of the other body.
    /// </summary>
    [Signal]
    public delegate void CollidedWallEventHandler();
    /// <summary>
    /// Emitted when the body collides with the bottom of the other body.
    /// </summary>
    [Signal]
    public delegate void CollidedCeilingEventHandler();
    /// <summary>
    /// Emitted when the body collides with the top of the other body
    /// </summary>
    [Signal]
    public delegate void CollidedFloorEventHandler();

    /// <summary>
    /// The mass of the body, which will affect the impulse that will be applied to the body.
    /// <b>Note</b> Due to the limitation of assignment for auto-implemented properties in C#, the default value is also the minimum that it can be modified to in the inspector.
    /// </summary>
    [Export(PropertyHint.Range, "0.0, 99999.0, 0.1, or_greater, hide_slider, suffix:kg")]
    public double Mass
    { 
        get => (double)PhysicsServer2D.BodyGetParam(GetRid(), PhysicsServer2D.BodyParameter.Mass); 
        set => PhysicsServer2D.BodySetParam(GetRid(), PhysicsServer2D.BodyParameter.Mass, Mathf.Max(0.001d, value));
    }
    /// <summary>
    /// The option that defines which transformation method will be applied to <c>MotionVector</c>.
    /// </summary>
    [Export]
    public MotionVectorDirectionEnum MotionVectorDirection { get; set; } = MotionVectorDirectionEnum.UpDirection;
    /// <summary>
    /// The <c>CharacterBody2D.velocity</c> of the body, transformed by a specific method defined by <c>MotionVectorDirection</c>.
    /// </summary>
    [Export(PropertyHint.None, "suffix:px/s")]
    public Vector2 MotionVector
    {
        get => GetMotionVector();
        set => SetMotionVector(value);
    }
    /// <summary>
    /// The scale of the gravity acceleration. The actual gravity acceleration is calculated as <c>GravityScale * GetGravity</c>.
    /// </summary>
    [Export(PropertyHint.Range, "0.0, 999.0, 0.1, or_greater, hide_slider, suffix:x")]
    public double GravityScale { get; set; } = 1.0d;
    /// <summary>
    /// The maximum of falling speed. If set to <c>0</c>, there will be no limit on maximum falling speed and the body will keep falling faster and faster.
    /// </summary>
    [Export(PropertyHint.Range, "0.0, 12500.0, 0.1, or_greater, hide_slider, suffix:px/s")]
    public double MaxFallingSpeed { get; set; } = 1500.0d;
    /// <summary>
    /// The speed of rotation synchronization. The higher the value, the faster the body will be rotated to fit to the up direction.
    /// </summary>
    [ExportGroup("Rotation Synchronization", "RotationSync")]
    [Export(PropertyHint.Range, "0.0, 9999.0, 0.1, radians_as_degrees, or_greater, hide_slider, suffix:°/s")]
    public double RotationSyncSpeed { get; set; } = Math.PI * 2.0d;

    private Vector2 _prevVelocity;
    private bool _prevIsOnFloor;

    private double GetDelta()
    {
        return Engine.IsInPhysicsFrame() ? GetPhysicsProcessDeltaTime() : GetProcessDeltaTime();
    }
    private static bool IsComponentNotNan(Vector2 vec)
    {
        for (byte i = 0; i < 2; i++) {
            if (Mathf.IsNaN(vec[i])) {
                return false;
            }
        }
        return true;
    }

#region == Main physics methods ==
    /// <summary>
    /// Moves the kine body instance.<br/><br/>
    /// The <c>speedScale</c> will affect the final motion, while the <c>globalRotationSyncUpDirection</c> will synchronize <c>Node2D.GlobalRotation</c> to <c>CharacterBody2D.UpDirection</c> by calling <c>SynchronizeGlobalRotationToUpDirection()</c>.
    /// </summary>
    /// <param name="speedScale"></param>
    /// <param name="globalRotationSyncUpDirection"></param>
    /// <returns>returns [code]true[/code] when it collides with other physics bodies.</returns>
    public bool MoveKineBody(float speedScale = 1.0f, bool globalRotationSyncUpDirection = true)
    {
        _prevVelocity = Velocity;
        _prevIsOnFloor = IsOnFloor();

        var g = GetGravity();
        var gDir = g.Normalized();

        // UpDirection will not work in floating mode
        if (MotionMode == MotionModeEnum.Grounded && IsComponentNotNan(gDir) && !gDir.IsZeroApprox()) {
            UpDirection = -gDir;
        }

        if (GravityScale > 0.0d) {
            Velocity += g * (float)(GravityScale * GetDelta());
            var fV = Velocity.Project(gDir); // Falling velocity
            if (MaxFallingSpeed > 0.0d && IsComponentNotNan(fV) && fV.Dot(gDir) > 0.0d && fV.LengthSquared() > Mathf.Pow(MaxFallingSpeed, 2.0d)) {
                Velocity -= fV - fV.Normalized() * (float)MaxFallingSpeed;
            }
        }

        if (globalRotationSyncUpDirection) {
            SynchronizeGlobalRotationToUpDirection();
        }

        Velocity *= speedScale;
        var ret = MoveAndSlide();
        Velocity /= speedScale;

        if (ret) {
            if (IsOnWall()) {
                EmitSignal(SignalName.CollidedWall);
            }
            if (IsOnCeiling()) {
                EmitSignal(SignalName.CollidedCeiling);
            }
            if (IsOnFloor()) {
                EmitSignal(SignalName.CollidedFloor);
            }
        }

        return ret;
    }
    /// <summary>
    /// Synchronizes <c>Node2D.GlobalRotation</c> to <c>CharacterBody2D.UpDirection</c>,
    /// that is to say, the global rotation of the body will be synchronized to the result of <c>GetUpDirectionRotationOrthogonal()</c>.
    /// </summary>
    public void SynchronizeGlobalRotationToUpDirection()
    {
        if (MotionMode != MotionModeEnum.Grounded) {
            return; // Non-ground mode does not support UpDirection.
        }
        var targetRotation = (float)GetUpDirectionRotation();
        if (IsOnFloor() || _prevIsOnFloor || Mathf.IsEqualApprox(GlobalRotation, targetRotation)) {
            GlobalRotation = targetRotation;
        } else {
            // To avoid the issue when GlobalRotation encounters PI or -PI.
            if (Mathf.IsEqualApprox(GlobalRotation, targetRotation)) {
                GlobalRotation *= -1.0f;
            }
            GlobalRotation = Mathf.LerpAngle(GlobalRotation, targetRotation, (float)(RotationSyncSpeed * GetDelta()));
        }
    }
#endregion

#region == Helper physics methods ==
    /// <summary>
    /// Accelerates the body by the given <c>acceleration</c>.
    /// </summary>
    /// <param name="acceleration"></param>
    public void Accelerate(Vector2 acceleration) => Velocity += acceleration;
    /// <summary>
    /// Accelerates the body to the target velocity by the given <c>acceleration</c>.
    /// </summary>
    /// <param name="acceleration"></param>
    /// <param name="to"></param>
    public void AccelerateTo(float acceleration, Vector2 to) => Velocity = Velocity.MoveToward(to, acceleration);
    /// <summary>
    /// Applies the given <c>momentum</c> to the body.<br/><br/>
    /// Momentum is a vector that represents the multiplication of mass and velocity, so the more momentum applied, the faster the body will move.
    /// However, the more mass the body has, the less velocity it will have, with the same momentum applied.<br/><br/>
    /// For platform games, the momentum is manipulated more suitable than the force.
    /// </summary>
    /// <param name="momentum"></param>
    public void ApplyMomentum(Vector2 momentum) => Velocity += momentum / (float)Mass;
    /// <summary>
    /// Sets the momentum of the body to the given <c>momentum</c>. See <c>ApplyMomentum()</c> for details about what is momentum.
    /// </summary>
    /// <param name="momentum"></param>
    public void SetMomentum(Vector2 momentum) => Velocity = momentum / (float)Mass;
    /// <summary>
    /// Returns the momentum of the body. See <c>ApplyMomentum()</c> for details about what is momentum.
    /// </summary>
    /// <returns></returns>
    public Vector2 GetMomentum() => Velocity * (float)Mass;
    /// <summary>
    /// Adds the motion vector by given acceleration.
    /// </summary>
    /// <param name="addedMotionVector"></param>
    public void AddMotionVector(Vector2 addedMotionVector) => MotionVector += addedMotionVector;
    /// <summary>
    /// Adds the motion vector to the target motion vector by given acceleration.
    /// </summary>
    /// <param name="addedMotionVector"></param>
    /// <param name="to"></param>
    public void AddMotionVectorTo(float addedMotionVector, Vector2 to) => MotionVector = MotionVector.MoveToward(to, addedMotionVector);
    /// <summary>
    /// Adds the <c>X</c> component of the motion vector by given acceleration to the target value.
    /// This is useful for fast achieving walking acceleration of a character's.
    /// </summary>
    /// <param name="addedXSpeed"></param>
    /// <param name="to"></param>
    public void AddMotionVectorXSpeedTo(float addedXSpeed, float to) => MotionVector = MotionVector with { X = Mathf.MoveToward(MotionVector.X, to, addedXSpeed) };
    /// <summary>
    /// Adds the <c>Y</c> component of the motion vector by given acceleration to the target value.
    /// This is useful for fast achieving jumping or falling acceleration of a character.
    /// </summary>
    /// <param name="addedYSpeed"></param>
    /// <param name="to"></param>
    public void AddMotionVectorYSpeedTo(float addedYSpeed, float to) => MotionVector = MotionVector with { Y = Mathf.MoveToward(MotionVector.Y, to, addedYSpeed) };
    /// <summary>
    /// Returns the friction the body receives when it is on the floor.<br/><br/>
    /// <b>Note:</b> This method is a bit performance-consuming, as it uses <c>PhysicsBody2D.TestMove()</c>, which takes a bit more time to get the result. Be careful when using it frequently, if you are caring about performance.
    /// </summary>
    public double GetFloorFriction()
    {
        if (!IsOnFloor()) {
            return 0.0d;
        };

        var friction = 0.0d;
        var kc = new KinematicCollision2D();
        TestMove(GlobalTransform, -GetFloorNormal(), kc);
        if (kc != null && kc.GetCollider() != null) {
            return (double)PhysicsServer2D.BodyGetParam(kc.GetColliderRid(), PhysicsServer2D.BodyParameter.Friction);
        }

        return friction;
    }
    /// <summary>
    /// Returns the velocity in previous frame.
    /// </summary>
    /// <returns></returns>
    public Vector2 GetPreviousVelocity() => _prevVelocity;
#endregion

#region == Platform game (wrapper) methods ==
    /// <summary>
    /// Makes the body jump along the up direction with the given <c>jumpingSpeed</c>.
    /// If <c>accumulating</c> is <c>true</c>, the <c>jumpingSpeed</c> will be added to the velocity directly. Otherwise, the component of the velocity along the up direction will be set to <c>jumpingSpeed</c>.
    /// </summary>
    /// <param name="jumpingSpeed"></param>
    /// <param name="accumulating"></param>
    public void Jump(float jumpingSpeed, bool accumulating = false) => Velocity += accumulating ? UpDirection * jumpingSpeed : -Velocity.Project(UpDirection) + UpDirection * jumpingSpeed;
    /// <summary>
    /// Reverses the velocity of the body, as if the body collided with a wall whose edge is parallel to the up direction of the body.
    /// </summary>
    public void TurnBackWalk() => Velocity = _prevVelocity.IsZeroApprox() ? Velocity.Reflect(UpDirection) : _prevVelocity.Reflect(UpDirection);
    /// <summary>
    /// Reverses the velocity of the body, as if the body collided with a ceiling or floor whose bottom or top is perpendicular to the up direction of the body.
    /// </summary>
    public void BounceJumpingFalling() => Velocity = _prevVelocity.IsZeroApprox() ? Velocity.Bounce(UpDirection) : _prevVelocity.Bounce(UpDirection);
    /// <summary>
    /// A wrapper method of <c>AddMotionVectorXSpeedTo()</c> for the convenience of platform games.
    /// </summary>
    /// <param name="acceleration"></param>
    /// <param name="to"></param>
    public void WalkingSpeedUp(float acceleration, float to) => AddMotionVectorXSpeedTo(acceleration, to);
    /// <summary>
    /// A wrapper method of <c>AddMotionVectorXSpeedTo()</c>, while the parameter <c>to></c> is always <c>0</c>, for the convenience of platform games.
    /// </summary>
    /// <param name="deceleration"></param>
    public void WalkingSlowDownToZero(float deceleration) => AddMotionVectorXSpeedTo(deceleration, 0.0f);
#endregion

#region == Helper methods ==
    /// <summary>
    /// Returns the angle of the up direction rotated by [code]PI / 2.0[/code].
    /// </summary>
    /// <returns></returns>
    public float GetUpDirectionRotation() => Vector2.Up.AngleTo(UpDirection);
#endregion

#region == Cross-dimensional methods ==
    /// <summary>
    /// Converts the unit of the given value from pixels to meters.
    /// </summary>
    /// <param name="pixels"></param>
    /// <returns></returns>
    public static double PixelsToMeters(double pixels) => pixels * 3779.527559d;
#endregion

#region == Setters and getters ==
    private void SetMotionVector(Vector2 value)
    {
        switch (MotionVectorDirection) {
            case MotionVectorDirectionEnum.Default:
                Velocity = value;
                break;
            case MotionVectorDirectionEnum.UpDirection:
                Velocity = value.Rotated(GetUpDirectionRotation());
                break;
            case MotionVectorDirectionEnum.GlobalRotation:
                Velocity = value.Rotated(GlobalRotation);
                break;
            default:
                break;
        }
    }
    private Vector2 GetMotionVector()
    {
        switch (MotionVectorDirection) {
            case MotionVectorDirectionEnum.UpDirection:
                return Velocity.Rotated(-GetUpDirectionRotation());
            case MotionVectorDirectionEnum.GlobalRotation:
                return Velocity.Rotated(-GlobalRotation);
            default:
                break;
        }
        return Velocity;
    }
#endregion

#region == Property settings ==
    public override bool _PropertyCanRevert(StringName property)
    {
        if (property == (StringName)"Mass") {
            return true;
        }
        return base._PropertyCanRevert(property);
    }
    public override Variant _PropertyGetRevert(StringName property)
    {
        if (property == (StringName)"Mass") {
            return 1.0d;
        }
        return base._PropertyGetRevert(property);
    }
#endregion
}

