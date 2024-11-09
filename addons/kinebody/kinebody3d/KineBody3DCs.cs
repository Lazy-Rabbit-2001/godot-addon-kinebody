using Godot;
using System;

namespace Godot;

/// <summary>
/// C# edition of <c>KineBody3D</c>.<br/><br/>
/// <b>Note:</b> During the high consumption of the <c>CharacterBody3D.MoveAndSlide()</c>, it is not couraged to run with the overnumbered use of <c>KineBody3DCs</c>.<br/><br/>
/// </summary>

[GlobalClass, Icon("res://addons/kinebody/kinebody3d/kinebody3d_csharp.svg")]
public partial class KineBody3DCs : CharacterBody3D
{
/// <summary>
    /// Definitions about the transformation method to <c>MotionVector</c>.<br/>
    /// * <c>UpDirection</c>: The direction of the <c>MotionVector</c> is transformed bythe quaternion contructed by <c>CharacterBody3D.UpDirection</c>.<br/>
    /// * <c>GlobalBasis</c>: The direction of the <c>MotionVector</c> is rotated by <c>Node3D.GlobalBasis.GetRotationQuaternion()</c>.<br/>
    /// * <c>Default</c>: The <c>MotionVector</c> is an alternative identifier of <c>CharacterBody3D.Velocity</c>.
    /// </summary>
    /// <seealso cref="MotionVector"/>
    public enum MotionVectorDirectionEnum : byte
    {
        UpDirection,
        GlobalBasis,
        Default,
    }

    private MotionVectorDirectionEnum _motionVectorDirection = MotionVectorDirectionEnum.UpDirection;

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

    private float _mass = 1.0f;

    /// <summary>
    /// The mass of the body, which will affect the impulse that will be applied to the body.<br/><br/>
    /// </summary>
    [Export(PropertyHint.Range, "0.0, 99999.0, 0.1, or_greater, hide_slider, suffix:kg")]
    public float Mass
    { 
        get => (float)PhysicsServer3D.BodyGetParam(GetRid(), PhysicsServer3D.BodyParameter.Mass); 
        set => PhysicsServer3D.BodySetParam(GetRid(), PhysicsServer3D.BodyParameter.Mass, value);
    }
    /// <summary>
    /// The option that defines which transformation method will be applied to <c>MotionVector</c>.
    /// </summary>
    /// <seealso cref="MotionVectorDirectionEnum"/>
    [Export]
    public MotionVectorDirectionEnum MotionVectorDirection { 
        get => _motionVectorDirection; 
        set
        {
            _motionVectorDirection = value;
            SetMotionVector(MotionVector); // Using setter to update the motion vector and make it transformed by the new transformation method defined by new MotioVectorDirection.
        }
    }
    /// <summary>
    /// The <c>CharacterBody2D.velocity</c> of the body, transformed by a specific method defined by <c>MotionVectorDirection</c>.
    /// </summary>
    /// <seealso cref="MotionVectorDirection"/>
    [Export(PropertyHint.None, "suffix:m/s")]
    public Vector3 MotionVector
    {
        get => GetMotionVector();
        set => SetMotionVector(in value);
    }
    /// <summary>
    /// The scale of the gravity acceleration. The actual gravity acceleration is calculated as <c>GravityScale * GetGravity()</c>.
    /// </summary>
    [Export(PropertyHint.Range, "0.0, 999.0, 0.1, or_greater, hide_slider, suffix:x")]
    public float GravityScale { get; set; } = 1.0f;
    /// <summary>
    /// The maximum of falling speed. If set to <c>0</c>, there will be no limit on maximum falling speed and the body will keep falling faster and faster.
    /// </summary>
    [Export(PropertyHint.Range, "0.0, 12500.0, 0.1, or_greater, hide_slider, suffix:m/s")]
    public float MaxFallingSpeed { get; set; } = 1500.0f;
    /// <summary>
    /// The speed of rotation synchronization. The higher the value, the faster the body will be rotated to fit to the up direction.
    /// </summary>
    [ExportGroup("Rotation Synchronization", "RotationSync")]
    [Export(PropertyHint.Range, "0.0, 9999.0, 0.1, radians_as_degrees, or_greater, hide_slider, suffix:°/s")]
    public double RotationSyncSpeed { get; set; } = Math.PI * 2.0d;

    private Vector3 _prevVelocity;
    private bool _prevIsOnFloor;

    private double GetDelta()
    {
        return Engine.IsInPhysicsFrame() ? GetPhysicsProcessDeltaTime() : GetProcessDeltaTime();
    }
    private static bool IsComponentNotNan(Vector3 vec)
    {
        for (byte i = 0; i < 3; i++) {
            if (Mathf.IsNaN(vec[i])) {
                return false;
            }
        }
        return true;
    }

#region == Main physics methods ==
    /// <summary>
    /// Moves the kine body instance.<br/><br/>
    /// The <c>speedScale</c> will affect the final motion, while the <c>globalRotationSyncUpDirection</c> will synchronize <c>Node3D.GlobalRotation</c> to <c>CharacterBody3D.UpDirection</c> by calling <c>SynchronizeGlobalRotationToUpDirection()</c>.
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
                Velocity -= fV - fV.Normalized() * MaxFallingSpeed;
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
            return; // Non-ground mode does not support `up_direction`.
        }
        var targetRotationQuaternion = GetUpDirectionRotationQuaternion();
        var globalRotationQuaternion = GlobalBasis.GetRotationQuaternion();
        var globalBasisScale = GlobalBasis.Scale;
        if (IsOnFloor() || _prevIsOnFloor || globalRotationQuaternion.IsEqualApprox(targetRotationQuaternion)) {
            GlobalBasis = new Basis(targetRotationQuaternion).Scaled(globalBasisScale);
        } else {
            GlobalBasis = new Basis(globalRotationQuaternion.Slerp(targetRotationQuaternion, (float)(RotationSyncSpeed * GetDelta()))).Scaled(globalBasisScale);
        }
    }
#endregion


#region == Helper physics methods ==
    /// <summary>
    /// Accelerates the body by the given <c>acceleration</c>.
    /// </summary>
    /// <param name="acceleration"></param>
    public void Accelerate(in Vector3 acceleration) => Velocity += acceleration;
    /// <summary>
    /// Accelerates the body to the target velocity by the given <c>acceleration</c>.
    /// </summary>
    /// <param name="acceleration"></param>
    /// <param name="to"></param>
    public void AccelerateTo(float acceleration, in Vector3 to) => Velocity = Velocity.MoveToward(to, acceleration);
    /// <summary>
    /// Applies the given <c>momentum</c> to the body.<br/><br/>
    /// Momentum is a vector that represents the multiplication of mass and velocity, so the more momentum applied, the faster the body will move.
    /// However, the more mass the body has, the less velocity it will have, with the same momentum applied.<br/><br/>
    /// For platform games, the momentum is manipulated more suitable than the force.
    /// </summary>
    /// <param name="momentum"></param>
    public void ApplyMomentum(in Vector3 momentum) => Velocity += momentum / (float)Mass;
    /// <summary>
    /// Sets the momentum of the body to the given <c>momentum</c>. See <c>ApplyMomentum()</c> for details about what is momentum.
    /// </summary>
    /// <param name="momentum"></param>
    public void SetMomentum(in Vector3 momentum) => Velocity = momentum / (float)Mass;
    /// <summary>
    /// Returns the momentum of the body. See <c>ApplyMomentum()</c> for details about what is momentum.
    /// </summary>
    /// <returns></returns>
    public Vector3 GetMomentum() => Velocity * (float)Mass;
    /// <summary>
    /// Adds the motion vector by given acceleration.
    /// </summary>
    /// <param name="addedMotionVector"></param>
    public void AddMotionVector(in Vector3 addedMotionVector) => MotionVector += addedMotionVector;
    /// <summary>
    /// Adds the motion vector to the target motion vector by given acceleration.
    /// </summary>
    /// <param name="addedMotionVector"></param>
    /// <param name="to"></param>
    public void AddMotionVectorTo(float addedMotionVector, in Vector3 to) => MotionVector = MotionVector.MoveToward(to, addedMotionVector);
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
    /// Adds the <c>Z</c> component of the motion vector by given acceleration to the target value.
    /// This is useful for fast achieving jumping or falling acceleration of a character.
    /// </summary>
    /// <param name="addedYSpeed"></param>
    /// <param name="to"></param>
    public void AddMotionVectorZSpeedTo(float addedZSpeed, float to) => MotionVector = MotionVector with { Z = Mathf.MoveToward(MotionVector.Z, to, addedZSpeed) };
    /// <summary>
    /// Returns the friction the body receives when it is on the floor.<br/><br/>
    /// <b>Note:</b> This method is a bit performance-consuming, as it uses <c>PhysicsBody2D.TestMove()</c>, which takes a bit more time to get the result. Be careful when using it frequently, if you are caring about performance.
    /// </summary>
    public float GetFloorFriction()
    {
        if (!IsOnFloor()) {
            return 0.0f;
        };

        var friction = 0.0f;
        var kc = new KinematicCollision3D();
        TestMove(GlobalTransform, -GetFloorNormal(), kc);
        if (kc != null && kc.GetCollider() != null) {
            return (float)PhysicsServer3D.BodyGetParam(kc.GetColliderRid(), PhysicsServer3D.BodyParameter.Friction);
        }

        return friction;
    }
    /// <summary>
    /// Returns the velocity in previous frame.
    /// </summary>
    /// <returns></returns>
    public Vector3 GetPreviousVelocity() => _prevVelocity;
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
    /// Sets walking velocity of the character. The walking velocity is the plane with <c>UpDirection</c> as its normal.<br/><br/>
    /// <b>Note:</b> The <c>x</c> component of the parameter will be the <c>x</c> component of the motion vector, while the <c>y</c> component will be the <c>z</c> component of the motion vector.
    /// </summary>
    /// <param name="to"></param>
    public void SetWalkingVelocity(in Vector2 to) => MotionVector = MotionVector with { X = to.X, Z = to.Y };
    /// <summary>
    /// Returns the walking velocity of the character. See <c>SetWalkingVelocity()</c> for details about what is the walking velocity.<br/><br/>
    /// <b>Note:</b> The <c>x</c> component of the returned value is from the <c>x</c> component of the motion vector, while the <c>y</c> component is form the <c>z</c> component of the motion vector.
    /// </summary>
    /// <returns></returns>
    public Vector2 GetWalkingVelocity() => new (MotionVector.X, MotionVector.Z);
    /// <summary>
    /// Speed up the walking velocity, for the convenience of platform games. See <c>SetWalkingVelocity()</c> for details about what is the walking velocity.
    /// </summary>
    /// <param name="acceleration"></param>
    /// <param name="to"></param>
    public void WalkingSpeedUp(float acceleration, in Vector2 to) => SetWalkingVelocity(GetWalkingVelocity().MoveToward(to, acceleration));
    /// <summary>
    /// Slows down the walking velocity to <c>Vector2.ZERO</c>, for the convenience of platform games. See <c>SetWalkingVelocity()</c> for details about what is the walking velocity.
    /// </summary>
    /// <param name="deceleration"></param>
    public void WalkingSlowDownToZero(float deceleration) => SetWalkingVelocity(GetWalkingVelocity().MoveToward(Vector2.Zero, deceleration));
#endregion

#region == Helper methods ==
    /// <summary>
    /// Returns the <c>Quaternion</c> that stands for the transformation of the up direction.
    /// </summary>
    /// <returns></returns>
    public Quaternion GetUpDirectionRotationQuaternion() 
    {
        // To avoid the error "!is_inside_tree() is true" thrown in tool mode, which is led by the global basis not initialized in 3D gaming environment,
		// we need to use Basis instead of GlobalBasis here during the initialization in the editor.
        var basis = Engine.IsEditorHint() ? Basis : GlobalBasis;
        var yDir = basis.Y.Normalized();
        return (new Quaternion(yDir, UpDirection) * basis.GetRotationQuaternion()).Normalized(); // Code arranged from https://ghostyii.com/ringworld/ by Ghostyii
    }
#endregion

#region == Cross-dimensional methods ==
    /// <summary>
    /// Converts the unit of the given value from meters to pixels.
    /// </summary>
    /// <param name="pixels"></param>
    /// <returns></returns>
    public static double MetersToPixels(double meters) => meters / 3779.527559d;
#endregion

#region == Setters and getters ==
    private void SetMotionVector(in Vector3 value)
    {
        switch (MotionVectorDirection) {
            case MotionVectorDirectionEnum.Default:
                Velocity = value;
                break;
            case MotionVectorDirectionEnum.UpDirection:
                Velocity = GetUpDirectionRotationQuaternion() * value;
                break;
            case MotionVectorDirectionEnum.GlobalBasis:
                Velocity = GlobalBasis.GetRotationQuaternion() * value;
                break;
            default:
                break;
        }
    }
    private Vector3 GetMotionVector()
    {
        switch (MotionVectorDirection) {
            case MotionVectorDirectionEnum.UpDirection:
                return GetUpDirectionRotationQuaternion().Inverse() * Velocity;
            case MotionVectorDirectionEnum.GlobalBasis:
                return GlobalBasis.GetRotationQuaternion().Inverse() * Velocity;
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
            return 1.0f;
        }
        return base._PropertyGetRevert(property);
    }
#endregion
}