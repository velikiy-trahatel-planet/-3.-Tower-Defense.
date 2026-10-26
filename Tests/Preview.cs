using Godot;

/// <summary>Captures the rendered main scene for visual inspection.</summary>
public partial class Preview : Node
{
    public override async void _Ready()
    {
        var scene = GD.Load<PackedScene>("res://Scenes/Main.tscn").Instantiate<GameManager>();
        AddChild(scene);
        for (int i = 0; i < 10; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        GetViewport().GetTexture().GetImage().SavePng("res://.godot/preview.png");
        GetTree().Quit();
    }
}
