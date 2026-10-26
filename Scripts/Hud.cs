using Godot;

/// <summary>Live status, build selection and pause/restart controls, also active after defeat.</summary>
public partial class Hud : CanvasLayer
{
    [Export] public float MessageDuration { get; set; } = 4;
    private Label _stats = null!;
    private Label _message = null!;
    private Label _outcome = null!;
    private float _messageTime;
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        var top = new PanelContainer { Position = new Vector2(22, 18), Size = new Vector2(700, 98), MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(top);
        _stats = new Label { Text = "CORE / DEFENSE", OffsetLeft = 18, OffsetTop = 12 };
        _stats.AddThemeFontSizeOverride("font_size", 21);
        top.AddChild(_stats);
        var help = new Label { Text = "WASD — движение   •   Мышь — прицел   •   ЛКМ / Пробел — огонь\n1 — Пушка (50)   2 — Взрывная (80)   3 — Крио (70)\nE — предпросмотр / построить   •   Esc — отменить   •   P — пауза   •   R — заново", Position = new Vector2(22, 626) };
        help.AddThemeFontSizeOverride("font_size", 17);
        root.AddChild(help);
        _message = new Label { Position = new Vector2(22, 128) };
        _message.AddThemeFontSizeOverride("font_size", 20);
        root.AddChild(_message);
        _outcome = new Label { Position = new Vector2(370, 285), Size = new Vector2(540, 160), HorizontalAlignment = HorizontalAlignment.Center };
        _outcome.AddThemeFontSizeOverride("font_size", 30);
        root.AddChild(_outcome);
        Notify("Защитите синее ядро. Выберите башню клавишами 1–3.");
    }
    public void Notify(string message, bool error = false)
    {
        _message.Text = message;
        _message.Modulate = error ? new Color(1, 0.4f, 0.35f) : new Color(0.65f, 1, 0.8f);
        _messageTime = MessageDuration;
    }
    public override void _Process(double delta)
    {
        var manager = GameManager.Instance;
        if (manager == null) return;
        var player = GetTree().GetFirstNodeInGroup("player") as Actor;
        var core = GetTree().GetFirstNodeInGroup("core") as Actor;
        _stats.Text = $"CORE / DEFENSE     Металл: {manager.Metal}     Волна: {manager.Wave}/{manager.TotalWaves}\nЯдро: {core?.Health.CurrentHealth ?? 0:0}     Игрок: {player?.Health.CurrentHealth ?? 0:0}     Враги: {manager.EnemyCount}     До волны: {manager.WaveCountdown:0}с";
        _outcome.Text = manager.IsFinished ? manager.Result + "\n\nR — начать заново" : GetTree().Paused ? "Пауза\nP — продолжить" : "";
        _messageTime -= (float)delta;
        _message.Visible = _messageTime > 0;
    }
    public override void _UnhandledInput(InputEvent input)
    {
        if (input.IsActionPressed("restart"))
        {
            GetTree().Paused = false;
            GetTree().ReloadCurrentScene();
        }
        if (input.IsActionPressed("pause") && GameManager.Instance?.IsFinished == false)
            GetTree().Paused = !GetTree().Paused;
    }
}
