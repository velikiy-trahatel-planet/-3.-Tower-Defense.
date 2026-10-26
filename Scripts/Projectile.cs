using Godot;

/// <summary>Only movement and lifetime. Damage belongs entirely to the child Area3D.</summary>
public partial class Projectile : Node3D
{
    [Export] public float Speed { get; set; } = 19;
    [Export] public float Lifetime { get; set; } = 2;
    public Vector3 Direction { get; set; } = Vector3.Forward;
    private float _age;
    public override void _PhysicsProcess(double delta)
    {
        Position += Direction * Speed * (float)delta;
        _age += (float)delta;
        if (_age >= Lifetime) QueueFree();
    }
}
