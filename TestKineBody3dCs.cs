using Godot;
using System;

namespace Godot;

[Tool]
public partial class TestKineBody3dCs : KineBody3DCs
{
    public override void _PhysicsProcess(double delta)
    {
        if (Engine.IsEditorHint()) {
            return;
        }

        SetWalkingVelocity(Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down") * 480.0f * (float)delta);

        if (Input.IsActionJustPressed("ui_accept") && IsOnFloor()) {
            Jump(400.0f * (float)delta);
        }

        MoveKineBody();

        base._PhysicsProcess(delta);
    }
}
