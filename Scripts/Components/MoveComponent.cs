using Godot;

/// <summary>Shared planar movement; temporary slows restore the exported base speed.</summary>
[GlobalClass]
public partial class MoveComponent : Node
{
    [Export] public float Speed { get; set; } = 5;
    [Export] public float ArenaLimit { get; set; } = 21;
    public Vector3 Direction { get; set; }
    public bool IsSlowed => _slowRemaining > 0;
    private float _slowRemaining;
    private float _multiplier = 1;
    public void ApplySlow(float multiplier, float duration)
    {
        _multiplier = Mathf.Min(_multiplier, multiplier);
        _slowRemaining = Mathf.Max(_slowRemaining, duration);
    }
    public override void _PhysicsProcess(double delta)
    {
        if (_slowRemaining > 0)
        {
            _slowRemaining -= (float)delta;
            if (_slowRemaining <= 0) _multiplier = 1;
        }
        var body = GetParent<CharacterBody3D>();
        body.Velocity = Direction.LimitLength() * Speed * _multiplier;
        body.MoveAndSlide();
        var position = body.Position;
        position.X = Mathf.Clamp(position.X, -ArenaLimit, ArenaLimit);
        position.Z = Mathf.Clamp(position.Z, -ArenaLimit, ArenaLimit);
        body.Position = position;
    }
}
