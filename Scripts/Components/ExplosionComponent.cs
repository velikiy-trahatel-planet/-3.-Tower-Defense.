using Godot;

/// <summary>Impact creates a separate larger damaging area, hitting every victim once.</summary>
[GlobalClass]
public partial class ExplosionComponent : DamagingComponent
{
    [Export] public float BlastRadius { get; set; } = 3.2f;
    [Export] public float EffectDuration { get; set; } = 0.35f;

    public override void ApplyDamage(Node3D target)
    {
        var blast = new DamagingComponent
        {
            Name = "BlastDamage", Damage = Damage, TargetGroups = TargetGroups.Duplicate(),
            OneShot = false, DestroyOwner = false, HitInterval = EffectDuration + 1,
            CollisionLayer = 0, CollisionMask = CollisionMask
        };
        blast.AddChild(new CollisionShape3D { Shape = new SphereShape3D { Radius = BlastRadius } });
        GetTree().CurrentScene.AddChild(blast);
        blast.GlobalPosition = GlobalPosition;
        // Deferred shape activation avoids modifying the physics world inside BodyEntered.
        var timer = new Timer { WaitTime = EffectDuration, OneShot = true };
        blast.AddChild(timer);
        timer.Timeout += blast.QueueFree;
        timer.Start();
        VisualComponent.ShowPulse(GetTree().CurrentScene, GlobalPosition, new Color(0.7f, 1, 0.25f), BlastRadius, EffectDuration);
    }
}
