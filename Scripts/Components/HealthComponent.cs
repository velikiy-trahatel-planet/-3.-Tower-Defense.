using Godot;

/// <summary>Reusable health and death events, independent of the owning entity.</summary>
[GlobalClass]
public partial class HealthComponent : Node
{
    [Export] public float MaxHealth { get; set; } = 100;
    public float CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;
    [Signal] public delegate void ChangedEventHandler(float current, float maximum);
    [Signal] public delegate void DiedEventHandler();

    public override void _Ready() => CurrentHealth = MaxHealth;

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0) return;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        EmitSignal(SignalName.Changed, CurrentHealth, MaxHealth);
        if (IsDead) EmitSignal(SignalName.Died);
    }
}
