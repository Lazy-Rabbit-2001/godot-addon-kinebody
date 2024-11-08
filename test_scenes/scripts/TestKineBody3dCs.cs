using Godot;
using System;

namespace Godot;

[Tool]
public partial class TestKineBody3DCs : KineBody3DCs
{
    public override void _PhysicsProcess(double delta)
    {
        if (Engine.IsEditorHint()) {
            return;
        }

        SetWalkingVelocity(Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down") * 10.0f);

        if (Input.IsActionJustPressed("ui_accept") && IsOnFloor()) {
            Jump(10.0f);
        }

        MoveKineBody();
    }
}
