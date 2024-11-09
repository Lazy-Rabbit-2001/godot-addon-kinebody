using Godot;
using System;

namespace GodotKineBody;

[Tool]
public partial class TestKineBody2DCs : KineBody2DCs
{
    public override void _PhysicsProcess(double delta)
    {
        if (Engine.IsEditorHint()) {
            return;
        }

        MotionVector = MotionVector with { X = Input.Singleton.GetAxis("ui_left", "ui_right") * 200 };
	
        if (Input.Singleton.IsActionJustPressed("ui_accept") && IsOnFloor()) {
            Jump(600);
        }

        MoveKineBody();
        base._PhysicsProcess(delta);
    }
}