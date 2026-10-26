using Godot;

/// <summary>Reusable weapon: projectile scene determines the effect, not the shooter.</summary>
[GlobalClass]
public partial class ShootComponent : Node
{
    [Export] public PackedScene ProjectileScene { get; set; } = null!;
    [Export] public float FireInterval { get; set; } = 0.5f;
    [Export] public float Range { get; set; } = 12;
    [Export] public float MuzzleOffset { get; set; } = 1.1f;
    [Export] public StringName TargetGroup { get; set; } = "enemies";
    [Export] public bool Automatic { get; set; } = true;
    [Export] public float DamageMultiplier { get; set; } = 1;
    [Export] public float ProjectileLifetimeMargin { get; set; } = 0.2f;
    private float _cooldown;
    public override void _PhysicsProcess(double delta)
    {
        _cooldown = Mathf.Max(0, _cooldown - (float)delta);
        if (!Automatic) return;
        var owner = GetParent<Node3D>();
        Node3D? closest = null;
        float distance = Range * Range;
        foreach (Node node in GetTree().GetNodesInGroup(TargetGroup))
        {
            if (node is not Node3D candidate || candidate.IsQueuedForDeletion()) continue;
            if (candidate.GetNodeOrNull<HealthComponent>("HealthComponent")?.IsDead != false) continue;
            float next = owner.GlobalPosition.DistanceSquaredTo(candidate.GlobalPosition);
            if (next < distance) { closest = candidate; distance = next; }
        }
        if (closest == null) return;
        Vector3 direction = (closest.GlobalPosition - owner.GlobalPosition) with { Y = 0 };
        if (direction.LengthSquared() < 0.001f) return;
        owner.LookAt(owner.GlobalPosition + direction, Vector3.Up);
        TryShoot(direction);
    }
    public bool TryShoot(Vector3 direction)
    {
        if (_cooldown > 0 || direction.LengthSquared() < 0.001f || ProjectileScene == null) return false;
        var owner = GetParent<Node3D>();
        var projectile = ProjectileScene.Instantiate<Projectile>();
        projectile.GetNode<DamagingComponent>("DamagingComponent").Damage *= DamageMultiplier;
        // Long-range shots must survive long enough to reach their selected target.
        projectile.Lifetime = Mathf.Max(projectile.Lifetime,
            (Range + MuzzleOffset) / Mathf.Max(projectile.Speed, 0.01f) + ProjectileLifetimeMargin);
        GetTree().CurrentScene.AddChild(projectile);
        projectile.GlobalPosition = owner.GlobalPosition + direction.Normalized() * MuzzleOffset;
        projectile.Direction = direction.Normalized();
        _cooldown = FireInterval;
        return true;
    }
}
