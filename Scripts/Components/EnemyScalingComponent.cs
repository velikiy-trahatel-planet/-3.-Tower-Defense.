using Godot;

/// <summary>Scales each new enemy from its scene defaults before health initialization.</summary>
[GlobalClass]
public partial class EnemyScalingComponent : Node
{
    [Export] public int WaveLevel { get; set; } = 1;
    [Export] public float HealthGrowth { get; set; } = 0.25f;
    [Export] public float DamageGrowth { get; set; } = 0.15f;
    [Export] public float SpeedGrowth { get; set; } = 0.04f;
    [Export] public float FireRateGrowth { get; set; } = 0.08f;
    [Export] public float RangeGrowth { get; set; } = 1.5f;
    private bool _configured;

    public override void _EnterTree()
    {
        if (_configured) return;
        _configured = true;
        int steps = Mathf.Max(0, WaveLevel - 1);
        var actor = GetParent<Actor>();
        actor.Health.MaxHealth *= 1 + HealthGrowth * steps;
        var move = actor.GetNodeOrNull<MoveComponent>("MoveComponent");
        if (move != null) move.Speed *= 1 + SpeedGrowth * steps;
        var contact = actor.GetNodeOrNull<DamagingComponent>("DamagingComponent");
        if (contact != null) contact.Damage *= 1 + DamageGrowth * steps;
        var shoot = actor.GetNodeOrNull<ShootComponent>("ShootComponent");
        if (shoot == null) return;
        shoot.DamageMultiplier *= 1 + DamageGrowth * steps;
        shoot.FireInterval /= 1 + FireRateGrowth * steps;
        float rangeIncrease = RangeGrowth * steps;
        shoot.Range += rangeIncrease;
        // Keep the original gap between the stopping distance and weapon range.
        actor.GetNode<EnemyBrainComponent>("EnemyBrainComponent").StopDistance += rangeIncrease;
    }
}
