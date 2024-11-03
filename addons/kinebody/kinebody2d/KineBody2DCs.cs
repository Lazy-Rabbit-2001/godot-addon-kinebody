using Godot;
using System;

namespace GodotKineBody;

/// <summary>
/// C# Edition of <see cref="KineBody2D"/>.<br/>
/// </summary>
[GlobalClass, Icon("res://addons/kinebody/kinebody2d/KineBody2DCs.svg")]
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
        UpDirection,
        GlobalRotation,
        Default,
    }

    /// <summary>
    /// The mass of the body, which will affect the impulse that will be applied to the body.
    /// <b>Note</b> Due to the limitation of assignment for auto-implemented properties in C#, the default value is also the minimum that it can be modified to in the inspector.
    /// </summary>
    [Export(PropertyHint.Range, "0.1, 99999.0, 0.1, or_greater, hide_slider, suffix:kg")]
    public double Mass
    { 
        get => (double)PhysicsServer2D.BodyGetParam(GetRid(), PhysicsServer2D.BodyParameter.Mass); 
        set => PhysicsServer2D.BodySetParam(GetRid(), PhysicsServer2D.BodyParameter.Mass, Mathf.Max(0.001d, value));
    }
    /// <summary>
    /// 
    /// </summary>
    [Export]
    public MotionVectorDirectionEnum MotionVectorDirection { get; set; } = MotionVectorDirectionEnum.UpDirection;
    /// <summary>
    /// 
    /// </summary>
    [Export(PropertyHint.None, "suffix:px/s")]
    public Vector2 MotionVector
    {
        get => GetMotionVector();
        set => SetMotionVector(value);
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
    [ExportGroup("Rotation", "Rotation")]
    /// <summary>
    /// 
    /// </summary>
    [Export(PropertyHint.Range, "0.0, 999.0, 0.1, or_greater, hide_slider, suffix:s")]
    public double RotationSynchronizingDuration = 0.1d;

    private Tween _rotationSynchronizingTween;


    private double GetDelta()
    {
        return Engine.IsInPhysicsFrame() ? GetPhysicsProcessDeltaTime() : GetProcessDeltaTime();
    }

    /// <summary>
    /// Moves the kine body instance.<br/><br/>
    /// The <c>speedScale</c> will affect the final motion, while the <c>globalRotationSyncUpDirection</c> will synchronize <c>Node2D.GlobalRotation</c> to <c>CharacterBody2D.UpDirection</c> by calling <c>SynchronizeGlobalRotationToUpDirection()</c>.
    /// </summary>
    /// <param name="speedScale"></param>
    /// <param name="globalRotationSyncUpDirection"></param>
    /// <returns>returns [code]true[/code] when it collides with other physics bodies.</returns>
    public bool MoveKineBody(float speedScale = 1.0f, bool globalRotationSyncUpDirection = true)
    {
        var g = GetGravity();
        var gDir = GetGravity().Normalized();

        // `up_direction` will not work in floating mode
        if (MotionMode == CharacterBody2D.MotionModeEnum.Grounded) {
            if (!Mathf.IsNaN(gDir.X) && !Mathf.IsNaN(gDir.Y) && !gDir.IsZeroApprox()) {
                UpDirection = -gDir;
            }
        }

        // Applying gravity
        if (!IsOnFloor() && GravityScale > 0.0d) {
            Velocity += g * (float)(GravityScale * GetDelta());
            var fV = Velocity.Project(gDir);
            if (MaxFallingSpeed > 0.0d && !Mathf.IsNaN(fV.X) && !Mathf.IsNaN(fV.Y) && fV.Dot(gDir) > 0.0d && fV.LengthSquared() > Mathf.Pow(MaxFallingSpeed, 2.0d)) {
                Velocity -= fV - fV.Normalized() * (float)MaxFallingSpeed;
            }
        }

        // Synchronizing global rotation to up direction
        if (globalRotationSyncUpDirection) {
            GlobalRotation = UpDirection.Angle();
        }
        
        // Applying speed scale
        var tmpV = Velocity;
        Velocity *= speedScale;
        var ret = MoveAndSlide();
        Velocity = tmpV;
        return ret;
    }

    /// <summary>
    /// Synchronizes <c>Node2D.GlobalRotation</c> to <c>CharacterBody2D.UpDirection</c>,
    /// that is to say, the global rotation of the body will be synchronized to <c>UpDirection.Angle() + Math.PI / 2.0d</c>.<br/><br/>
    /// <b>Note:</b> This is achieved by creating an object <c>Tween</c>, which may take more space of memory. Make sure to call this method within certain instances.
    /// </summary>
    public async void SynchronizeGlobalRotationToUpDirection()
    {
        if (MotionMode != MotionModeEnum.Grounded) {
            return; // Non-ground mode does not support `up_direction`.
        }
        var targetRotation = UpDirection.Angle() + Math.PI / 2.0d;
        if (Mathf.IsEqualApprox(GlobalRotation, targetRotation)) {
            GlobalRotation = (float)targetRotation;
        } else if (_rotationSynchronizingTween == null) {
            // Creating a new tween for the rotation synchronization.
            _rotationSynchronizingTween = CreateTween().SetTrans(Tween.TransitionType.Sine);
            _rotationSynchronizingTween.TweenProperty(this, (NodePath)"GlobalRotation", targetRotation, RotationSynchronizingDuration);
            // Waiting for the tween to finish.
            await ToSignal(_rotationSynchronizingTween, Tween.SignalName.Finished);
            // Clearing the tween reference to avoid memory leak.
            _rotationSynchronizingTween.Kill();
            _rotationSynchronizingTween = null;
        }
    }

#region == Setters and Getters ==
    private void SetMotionVector(Vector2 value)
    {
        switch (MotionVectorDirection) {
            case MotionVectorDirectionEnum.Default:
                Velocity = value;
                break;
            case MotionVectorDirectionEnum.UpDirection:
                Velocity = value.Rotated((float)(UpDirection.Angle() + Math.PI / 2.0d));
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
                return Velocity.Rotated((float)(-UpDirection.Angle() - Math.PI / 2.0d));
            case MotionVectorDirectionEnum.GlobalRotation:
                return Velocity.Rotated(-GlobalRotation);
            default:
                break;
        }
        return Velocity;
    }
#endregion
}

