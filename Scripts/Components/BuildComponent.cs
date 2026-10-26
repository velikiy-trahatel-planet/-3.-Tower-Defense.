using Godot;

/// <summary>Player-relative ghost placement, obstruction checks and atomic metal spending.</summary>
[GlobalClass]
public partial class BuildComponent : Node
{
    [Export] public Godot.Collections.Array<PackedScene> TowerScenes { get; set; } = new();
    [Export] public float BuildDistance { get; set; } = 3;
    [Export] public float Clearance { get; set; } = 1.8f;
    [Export] public float ArenaLimit { get; set; } = 19;
    [Export] public float GhostOpacity { get; set; } = 0.45f;
    [Export] public float CoreClearance { get; set; } = 0.8f;
    public bool IsBuilding { get; private set; }
    public int SelectedIndex { get; private set; }
    private VisualComponent? _ghost;
    private Vector3 _position;
    private bool _valid;
    private int _cost;
    public override void _UnhandledInput(InputEvent input)
    {
        if (input.IsActionPressed("cancel_build")) { StopBuilding(); return; }
        for (int i = 0; i < TowerScenes.Count; i++)
            if (input.IsActionPressed($"tower_{i + 1}")) { SelectedIndex = i; StartBuilding(); }
        if (!input.IsActionPressed("build")) return;
        if (!IsBuilding) { StartBuilding(); return; }
        UpdateGhost();
        var manager = GameManager.Instance!;
        if (!_valid) { manager.Notify("Нельзя строить: место занято или за границей поля", true); return; }
        if (!manager.TrySpend(_cost)) return;
        var tower = TowerScenes[SelectedIndex].Instantiate<Actor>();
        manager.AddChild(tower);
        tower.GlobalPosition = _position;
        manager.Notify("Башня установлена");
        StopBuilding();
    }
    private void StartBuilding()
    {
        StopBuilding();
        IsBuilding = true;
        var template = TowerScenes[SelectedIndex].Instantiate<Actor>();
        var visual = template.GetNode<VisualComponent>("VisualComponent");
        _cost = template.BuildCost;
        _ghost = new VisualComponent { Kind = visual.Kind, Tint = visual.Tint, ModelScale = visual.ModelScale, Opacity = GhostOpacity };
        GameManager.Instance!.AddChild(_ghost);
        template.Free();
        UpdateGhost();
        GameManager.Instance.Notify($"Постройка: {_cost} металла • E — установить • Esc — отмена");
    }
    public override void _PhysicsProcess(double delta) { if (IsBuilding) UpdateGhost(); }
    private void UpdateGhost()
    {
        var owner = GetParent<Node3D>();
        _position = owner.GlobalPosition + owner.GetNode<PlayerControllerComponent>("PlayerControllerComponent").AimDirection * BuildDistance;
        _valid = Mathf.Abs(_position.X) < ArenaLimit && Mathf.Abs(_position.Z) < ArenaLimit;
        foreach (Node node in GetTree().GetNodesInGroup("actors"))
            if (node is Node3D actor && actor != owner && !actor.IsQueuedForDeletion() && actor.GlobalPosition.DistanceTo(_position) < Clearance + (actor.IsInGroup("core") ? CoreClearance : 0)) _valid = false;
        if (_ghost == null) return;
        _ghost.GlobalPosition = _position;
        _ghost.SetPreviewColor(_valid && GameManager.Instance!.Metal >= _cost ? Colors.LightGreen : Colors.IndianRed);
    }
    public void StopBuilding()
    {
        IsBuilding = false;
        if (IsInstanceValid(_ghost)) _ghost!.QueueFree();
        _ghost = null;
    }
    public override void _ExitTree() { if (IsInstanceValid(_ghost)) _ghost!.QueueFree(); }
}
