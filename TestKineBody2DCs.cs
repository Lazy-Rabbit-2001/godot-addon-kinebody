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
        var t = Time.GetTicksUsec();
        MoveKineBody();
        base._PhysicsProcess(delta);
        GD.Print(Time.GetTicksUsec() - t);
    }
}