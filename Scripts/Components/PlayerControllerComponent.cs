using Godot;

/// <summary>Maps WASD and the mouse ray onto composed movement and weapon nodes.</summary>
[GlobalClass]
public partial class PlayerControllerComponent : Node
{
    public Vector3 AimDirection { get; private set; } = Vector3.Forward;
    public override void _PhysicsProcess(double delta)
    {
        var owner = GetParent<Node3D>();
        Vector2 input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        owner.GetNode<MoveComponent>("MoveComponent").Direction = new Vector3(input.X, 0, input.Y);
        Camera3D camera = GetViewport().GetCamera3D();
        Vector2 mouse = GetViewport().GetMousePosition();
        Vector3? hit = new Plane(Vector3.Up, owner.GlobalPosition.Y).IntersectsRay(camera.ProjectRayOrigin(mouse), camera.ProjectRayNormal(mouse));
        if (hit.HasValue)
        {
            Vector3 direction = hit.Value - owner.GlobalPosition;
            direction.Y = 0;
            if (direction.LengthSquared() > 0.01f)
            {
                AimDirection = direction.Normalized();
                owner.LookAt(owner.GlobalPosition + AimDirection, Vector3.Up);
            }
        }
        if (Input.IsActionPressed("fire") && !owner.GetNode<BuildComponent>("BuildComponent").IsBuilding)
            owner.GetNode<ShootComponent>("ShootComponent").TryShoot(AimDirection);
    }
}
