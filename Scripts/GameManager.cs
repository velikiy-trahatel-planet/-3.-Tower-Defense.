using Godot;
using System.Collections.Generic;

/// <summary>Scene-root singleton: entity bookkeeping, economy, waves and game outcome.</summary>
public partial class GameManager : Node3D
{
    public static GameManager? Instance { get; private set; }
    [Export] public int StartingMetal { get; set; } = 240;
    [Export] public int TotalWaves { get; set; } = 6;
    [Export] public int BaseEnemyCount { get; set; } = 10;
    [Export] public int WaveGrowth { get; set; } = 5;
    [Export] public float PreparationTime { get; set; } = 12;
    [Export] public float WaveDelay { get; set; } = 8;
    [Export] public float SpawnInterval { get; set; } = 0.65f;
    [Export] public float SpawnRadius { get; set; } = 20;
    [Export] public Godot.Collections.Array<PackedScene> EnemyScenes { get; set; } = new();
    public int Metal { get; private set; }
    public int Wave { get; private set; }
    public bool IsFinished { get; private set; }
    public string Result { get; private set; } = "";
    public float WaveCountdown => _waveCountdown;
    public int EnemyCount => _tracked.FindAll(a => IsInstanceValid(a) && !a.IsQueuedForDeletion() && a.IsInGroup("enemies")).Count;
    private readonly List<Actor> _tracked = new();
    private float _waveCountdown;
    private float _spawnCountdown;
    private int _remaining;
    private readonly RandomNumberGenerator _random = new();
    public override void _EnterTree()
    {
        Instance = this;
        Metal = StartingMetal;
        _waveCountdown = PreparationTime;
        _random.Randomize();
    }
    public void Register(Actor actor)
    {
        _tracked.Add(actor);
        actor.TreeExiting += () =>
        {
            _tracked.Remove(actor);
            // Reward destruction exactly once; scene unload/restart is never rewarded.
            if (!IsFinished && actor.Health.IsDead && (actor.IsInGroup("enemies") || actor.IsInGroup("towers")))
                Metal += actor.MetalReward;
        };
    }
    public bool TrySpend(int cost)
    {
        if (cost < 0 || IsFinished) return false;
        if (Metal < cost) { Notify($"Недостаточно металла: нужно {cost}, есть {Metal}", true); return false; }
        Metal -= cost;
        return true;
    }
    public void Notify(string message, bool error = false) => GetNode<Hud>("HUD").Notify(message, error);
    public override void _Process(double delta)
    {
        if (IsFinished) return;
        if (_remaining > 0)
        {
            _spawnCountdown -= (float)delta;
            if (_spawnCountdown <= 0)
            {
                var enemy = EnemyScenes[_remaining % EnemyScenes.Count].Instantiate<Actor>();
                enemy.GetNode<EnemyScalingComponent>("EnemyScalingComponent").WaveLevel = Wave;
                float angle = _random.RandfRange(0, Mathf.Tau);
                AddChild(enemy);
                enemy.Position = new Vector3(Mathf.Cos(angle) * SpawnRadius, 0.7f, Mathf.Sin(angle) * SpawnRadius);
                _remaining--;
                _spawnCountdown = SpawnInterval;
            }
        }
        else if (EnemyCount == 0)
        {
            if (Wave >= TotalWaves) { EndGame("Победа! Все волны отражены"); return; }
            _waveCountdown -= (float)delta;
            if (_waveCountdown <= 0) StartWave();
        }
    }
    private void StartWave()
    {
        Wave++;
        _remaining = BaseEnemyCount + (Wave - 1) * WaveGrowth;
        _waveCountdown = WaveDelay;
        _spawnCountdown = 0;
        Notify($"Волна {Wave}: защитите ядро!");
    }
    public void EndGame(string result)
    {
        if (IsFinished) return;
        IsFinished = true;
        Result = result;
        GetTree().Paused = true;
    }
    public override void _ExitTree() { if (Instance == this) Instance = null; }
}
