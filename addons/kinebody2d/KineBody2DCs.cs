using Godot;
using System;

namespace GodotKineBody;

/// <summary>
/// C# Edition of <see cref="KineBody2D"/>.<br/>
/// <b>Note:</b> Due to the unability to assign an auto-implemented property that shadows the mass parameter via the physics server,
/// to make the node work as intended, A <see cref="[Tool]"/> feature is attached, and you should attach this on all of its derived classes as well to make this work.
/// </summary>

[Tool]
[GlobalClass]
public partial class KineBody2DCs : CharacterBody2D
{
    /// <summary>
    /// Definitions about the transformation method on <c>MotionVector"</c>.<br/>
    /// <c>MotionVectorDirectionUpDirection</c>: The direction of the <c>MotionVector"</c> equals to <c>UpDirection.Rotated(Math.PI / 2.0d)</c>.<br/>
    /// <c>MotionVectorDirectionGlobalRotation</c>: The direction of the <c>MotionVector"</c> is rotated by <c>GlobalRotation</c>.<br/>
    /// <c>MotionVectorDirectionDefault</c>: The <c>MotionVector"</c> is an alternative identifier of <c>CharacterBody2D.Velocity</c>.
    /// </summary>
    /// <seealso cref="MotionVector"/>
    public enum MotionVectorDirectionEnum
    {
        MotionVectorDirectionUpDirection,
        MotionVectorDirectionGlobalRotation,
        MotionVectorDirectionDefault,
    }

    // Rad of the RotationSnappingLerpSpeedDegrees
    private double _rotationSnappingLerpSpeed = Math.PI / 3.0d;

    /// <summary>
    /// The mass of the body, which will affect the impulse that will be applied to the body.
    /// </summary>
    [Export(PropertyHint.Range, "0.0, 99999.0, 0.1, or_greater, hide_slider, suffix:kg")]
    public double Mass
    { 
        get => (double)PhysicsServer2D.BodyGetParam(GetRid(), PhysicsServer2D.BodyParameter.Mass); 
        set => PhysicsServer2D.BodySetParam(GetRid(), PhysicsServer2D.BodyParameter.Mass, value);
    }
    /// <summary>
    /// 
    /// </summary>
    [Export]
    public MotionVectorDirectionEnum MotionVectorDirection { get; set; } = MotionVectorDirectionEnum.MotionVectorDirectionUpDirection;
    /// <summary>
    /// 
    /// </summary>
    [Export(PropertyHint.None, "suffix:px/s")]
    public Vector2 MotionVector
    {
        get => _getMotionVector();
        set => _setMotionVector(value);
    }
    /// <summary>
    /// 
    /// </summary>
    [Export(PropertyHint.Range, "0.0, 999.0, 0.1, or_greater, hide_slider, suffix:x")]
    public double GravityScale { get; set; } = 1.0d;
    /// <summary>
    /// 
    /// </summary>
    [Export(PropertyHint.Range, "0.0, 12500.0, 0.1, or_greater, hide_slider, suffix:px/s")]
    public double MaxFallingSpeed { get; set; } = 1500.0d;
    [ExportGroup("Rotation", "Rotation_")]
    /// <summary>
    /// 
    /// </summary>
    [Export(PropertyHint.Range, "0.0, 999.0, 0.1, or_greater, hide_slider, suffix:°/s")]
    public double RotationSnappingLerpSpeedDegrees
    { 
        get => Mathf.RadToDeg(_rotationSnappingLerpSpeed); 
        set => Mathf.DegToRad(value);
    }

    private double GetDelta()
    {
        return Engine.IsInPhysicsFrame() ? GetPhysicsProcessDeltaTime() : GetProcessDeltaTime();
    }

    /// <summary>
    /// Moves the body with the given speed scale.
    /// </summary>
    /// <param name="speedScale"></param>
    /// <returns></returns>
    public bool MoveKineBody(float speedScale = 1.0f)
    {
        var g = GetGravity();
        var gDir = GetGravity().Normalized();

        // `up_direction` will not work in floating mode
        if (MotionMode == MotionModeEnum.Grounded) {
            if (!Mathf.IsNaN(gDir.X) && !Mathf.IsNaN(gDir.Y) && !gDir.IsZeroApprox()) {
                UpDirection = -gDir;
            }
        }

        // Applying gravity
        if (!IsOnFloor() && GravityScale > 0.0d) {
            Velocity = new Vector2(Velocity.X, Velocity.Y) + g * (float)(GravityScale * GetDelta());
            var fV = Velocity.Project(gDir);
            if (MaxFallingSpeed > 0.0d && !Mathf.IsNaN(fV.X) && !Mathf.IsNaN(fV.Y) && fV.Dot(gDir) > 0.0d && fV.LengthSquared() > Mathf.Pow(MaxFallingSpeed, 2.0d)) {
                Velocity -= fV - fV.Normalized() * (float)MaxFallingSpeed;
            }
        }
        
        // Applying speed scale
        var tmpV = Velocity;
        Velocity = new Vector2(Velocity.X, Velocity.Y) * (float)speedScale;
        var ret = MoveAndSlide();
        Velocity = tmpV;
        return ret;
    }

#region == Setters and Getters ==
    private void _setMotionVector(Vector2 value)
    {
        switch (MotionVectorDirection) {
            case MotionVectorDirectionEnum.MotionVectorDirectionDefault:
                Velocity = value;
                break;
            case MotionVectorDirectionEnum.MotionVectorDirectionUpDirection:
                Velocity = value.Rotated((float)(UpDirection.Angle() + Math.PI / 2.0d));
                break;
            case MotionVectorDirectionEnum.MotionVectorDirectionGlobalRotation:
                Velocity = value.Rotated(GlobalRotation);
                break;
            default:
                break;
        }
    }
    private Vector2 _getMotionVector()
    {
        switch (MotionVectorDirection) {
            case MotionVectorDirectionEnum.MotionVectorDirectionUpDirection:
                return Velocity.Rotated((float)(-UpDirection.Angle() - Math.PI / 2.0d));
            case MotionVectorDirectionEnum.MotionVectorDirectionGlobalRotation:
                return Velocity.Rotated(-GlobalRotation);
            default:
                break;
        }
        return Velocity;
    }
#endregion

#region == Complicated property settings ==
    public override bool _PropertyCanRevert(StringName property)
    {
        switch (property) {
            case "Mass":
            case "RotationSnappingLerpSpeedDegrees":
                return true;
            default:
                break;  
        }
        return base._PropertyCanRevert(property);
    }
    public override Variant _PropertyGetRevert(StringName property)
    {
        switch (property) {
            case "Mass":
                return 1.0f;
            case "RotationSnappingLerpSpeedDegrees":
                return Mathf.RadToDeg(Math.PI / 3.0d);
            default:
                break;  
        }
        return base._PropertyGetRevert(property);
    }
#endregion
}

