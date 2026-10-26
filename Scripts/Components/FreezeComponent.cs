using Godot;

/// <summary>Damage plus a timed, non-stacking movement penalty.</summary>
[GlobalClass]
public partial class FreezeComponent : DamagingComponent
{
    [Export(PropertyHint.Range, "0,1")] public float SpeedMultiplier { get; set; } = 0.3f;
    [Export] public float FreezeDuration { get; set; } = 2.8f;
    [Export] public float EffectRadius { get; set; } = 1;
    [Export] public float EffectDuration { get; set; } = 0.3f;
    public override void ApplyDamage(Node3D target)
    {
        base.ApplyDamage(target);
        target.GetNodeOrNull<MoveComponent>("MoveComponent")?.ApplySlow(SpeedMultiplier, FreezeDuration);
        VisualComponent.ShowPulse(GetTree().CurrentScene, target.GlobalPosition, new Color(0.45f, 1, 0.85f), EffectRadius, EffectDuration);
    }
}
