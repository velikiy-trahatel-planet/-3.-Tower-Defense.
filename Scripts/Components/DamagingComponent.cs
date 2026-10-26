using Godot;
using System.Collections.Generic;

/// <summary>Shared hit filtering, contact cooldowns, damage and one-shot disposal.</summary>
[GlobalClass]
public partial class DamagingComponent : Area3D
{
    [Export] public float Damage { get; set; } = 18;
    [Export] public Godot.Collections.Array<StringName> TargetGroups { get; set; } = new() { "enemies" };
    [Export] public bool OneShot { get; set; } = true;
    [Export] public bool DestroyOwner { get; set; } = true;
    [Export] public float HitInterval { get; set; } = 0.8f;
    private readonly Dictionary<ulong, double> _lastHits = new();
    private double _elapsed;
    private bool _consumed;

    public override void _Ready() => BodyEntered += OnBodyEntered;
    private void OnBodyEntered(Node3D body) => Callable.From(() => { if (IsInstanceValid(body)) TryHit(body); }).CallDeferred();

    public override void _PhysicsProcess(double delta)
    {
        _elapsed += delta;
        if (!OneShot)
            foreach (Node3D body in GetOverlappingBodies()) TryHit(body);
    }

    public bool CanDamage(Node target)
    {
        if (!IsInstanceValid(target) || target.IsQueuedForDeletion()) return false;
        var health = target.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (health == null || health.IsDead) return false;
        foreach (StringName group in TargetGroups)
            if (target.IsInGroup(group)) return true;
        return false;
    }

    public bool TryHit(Node3D target)
    {
        if (_consumed || !CanDamage(target)) return false;
        ulong id = target.GetInstanceId();
        if (_lastHits.TryGetValue(id, out double last) && _elapsed - last < HitInterval) return false;
        if (!OneShot) _lastHits[id] = _elapsed;
        ApplyDamage(target);
        if (OneShot) { _consumed = true; DestroyAfterHit(); }
        return true;
    }

    public virtual void ApplyDamage(Node3D target) => target.GetNode<HealthComponent>("HealthComponent").TakeDamage(Damage);

    public virtual void DestroyAfterHit()
    {
        if (DestroyOwner) GetParent().QueueFree();
        else QueueFree();
    }
}
