using Godot;

/// <summary>Connects composed entity health to lifecycle; no actor inheritance tree is needed.</summary>
public partial class Actor : CharacterBody3D
{
    [Export] public int MetalReward { get; set; } = 12;
    [Export] public int BuildCost { get; set; } = 50;
    [Export] public float DeathEffectRadius { get; set; } = 1.4f;
    [Export] public float DeathEffectDuration { get; set; } = 0.25f;
    public HealthComponent Health => GetNode<HealthComponent>("HealthComponent");
    public override void _Ready()
    {
        Health.Died += Die;
        GameManager.Instance?.Register(this);
    }
    private void Die()
    {
        if (IsInGroup("core")) GameManager.Instance?.EndGame("Ядро уничтожено");
        else if (IsInGroup("player")) GameManager.Instance?.EndGame("Защитник погиб");
        VisualComponent.ShowPulse(GetTree().CurrentScene, GlobalPosition, IsInGroup("enemies") ? Colors.OrangeRed : Colors.LightGreen, DeathEffectRadius, DeathEffectDuration);
        QueueFree();
    }
}
