using Godot;

/// <summary>Enemies approach the closest friendly entity; shooters stop at firing range.</summary>
[GlobalClass]
public partial class EnemyBrainComponent : Node
{
    [Export] public float StopDistance { get; set; } = 1.1f;
    [Export] public float AvoidanceRadius { get; set; } = 1.5f;
    [Export] public float AvoidanceStrength { get; set; } = 0.7f;
    public override void _PhysicsProcess(double delta)
    {
        var owner = GetParent<Node3D>();
        Node3D? target = null;
        float distance = float.MaxValue;
        foreach (Node node in GetTree().GetNodesInGroup("allies"))
        {
            if (node is not Actor actor || actor.Health.IsDead || actor.IsQueuedForDeletion()) continue;
            float next = owner.GlobalPosition.DistanceSquaredTo(actor.GlobalPosition);
            if (next < distance) { distance = next; target = actor; }
        }
        var move = GetParent().GetNode<MoveComponent>("MoveComponent");
        move.Direction = Vector3.Zero;
        if (target == null || distance <= StopDistance * StopDistance) return;
        Vector3 direction = (target.GlobalPosition - owner.GlobalPosition).Normalized();
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node == owner || node is not Node3D other) continue;
            Vector3 away = owner.GlobalPosition - other.GlobalPosition;
            if (away.LengthSquared() > 0.001f && away.LengthSquared() < AvoidanceRadius * AvoidanceRadius)
                direction += away.Normalized() * AvoidanceStrength;
        }
        direction.Y = 0;
        move.Direction = direction.Normalized();
        if (direction.LengthSquared() > 0.001f) owner.LookAt(owner.GlobalPosition + direction, Vector3.Up);
    }
}
