using Godot;

/// <summary>Primitive silhouettes and health bars; the preview reuses only this visual node.</summary>
[GlobalClass]
public partial class VisualComponent : Node3D
{
    [Export] public string Kind { get; set; } = "standard";
    [Export] public Color Tint { get; set; } = new(0.2f, 0.9f, 0.5f);
    [Export] public float Opacity { get; set; } = 1;
    [Export] public float ModelScale { get; set; } = 1;
    [Export] public float HealthBarWidth { get; set; } = 1.5f;
    [Export] public float HealthBarHeight { get; set; } = 1.6f;
    private MeshInstance3D? _healthFill;
    public override void _Ready()
    {
        Build();
        Scale = Vector3.One * ModelScale;
        var health = GetParent().GetNodeOrNull<HealthComponent>("HealthComponent");
        if (health == null || Opacity < 1) return;
        AddMesh(new BoxMesh { Size = new Vector3(HealthBarWidth, 0.08f, 0.12f) }, new Vector3(0, HealthBarHeight, 0), new Color(0.1f, 0.12f, 0.15f));
        _healthFill = AddMesh(new BoxMesh { Size = new Vector3(HealthBarWidth, 0.09f, 0.13f) }, new Vector3(0, HealthBarHeight, 0), Tint);
        health.Changed += (current, max) => { if (_healthFill != null) _healthFill.Scale = new Vector3(Mathf.Max(0.001f, current / max), 1, 1); };
    }
    private void Build()
    {
        if (Kind == "bullet") { AddMesh(new SphereMesh { Radius = 0.22f, Height = 0.44f }, Vector3.Zero, Tint); return; }
        if (Kind == "core")
        {
            AddMesh(new CylinderMesh { TopRadius = 1.4f, BottomRadius = 1.7f, Height = 0.5f }, new Vector3(0, -0.3f, 0), Tint.Darkened(0.5f));
            var crystal = AddMesh(new PrismMesh { Size = new Vector3(1.6f, 2.2f, 1.6f) }, new Vector3(0, 0.7f, 0), Tint);
            crystal.RotationDegrees = new Vector3(0, 45, 0);
            return;
        }
        bool crawler = Kind == "crawler";
        AddMesh(new CylinderMesh { TopRadius = crawler ? 0.6f : 0.65f, BottomRadius = 0.85f, Height = 0.45f }, new Vector3(0, -0.35f, 0), Tint.Darkened(0.5f));
        Mesh body = Kind == "freeze" ? new PrismMesh { Size = new Vector3(1, 1.4f, 1) } : new BoxMesh { Size = new Vector3(1, crawler ? 0.35f : 0.8f, 1) };
        if (Kind == "player") body = new CapsuleMesh { Radius = 0.42f, Height = 1.2f };
        AddMesh(body, new Vector3(0, 0.1f, 0), Tint);
        int barrels = Kind == "explosive" ? 2 : 1;
        for (int i = 0; i < barrels; i++)
            AddMesh(new BoxMesh { Size = new Vector3(0.22f, 0.22f, crawler ? 0.35f : 1.1f) }, new Vector3(barrels == 2 ? (i == 0 ? -0.3f : 0.3f) : 0, 0.25f, -0.7f), Tint.Lightened(0.3f));
    }
    private MeshInstance3D AddMesh(Mesh mesh, Vector3 position, Color tint)
    {
        tint.A = Opacity;
        var material = new StandardMaterial3D { AlbedoColor = tint, Roughness = 0.65f };
        if (Opacity < 1) material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        var instance = new MeshInstance3D { Mesh = mesh, Position = position, MaterialOverride = material };
        AddChild(instance);
        return instance;
    }
    public void SetPreviewColor(Color color)
    {
        foreach (Node child in GetChildren())
            if (child is MeshInstance3D mesh && mesh.MaterialOverride is StandardMaterial3D material)
                material.AlbedoColor = new Color(color, Opacity);
    }
    public static void ShowPulse(Node parent, Vector3 position, Color color, float radius, float duration)
    {
        var material = new StandardMaterial3D { AlbedoColor = new Color(color, 0.35f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
        var pulse = new MeshInstance3D { Mesh = new SphereMesh { Radius = radius, Height = radius * 2 }, MaterialOverride = material, Position = position, Scale = Vector3.One * 0.1f };
        parent.AddChild(pulse);
        var tween = pulse.CreateTween();
        tween.TweenProperty(pulse, "scale", Vector3.One, duration);
        tween.TweenCallback(Callable.From(pulse.QueueFree));
    }
}
