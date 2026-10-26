using Godot;
using System;
using System.Threading.Tasks;

/// <summary>Runs real scenes and physics, including area hits, construction and end states.</summary>
public partial class IntegrationTests : Node
{
    private GameManager _game = null!;
    private int _checks;
    public override async void _Ready()
    {
        try
        {
            await Frames(1);
            _game = GD.Load<PackedScene>("res://Scenes/Main.tscn").Instantiate<GameManager>();
            GetTree().Root.AddChild(_game);
            GetTree().CurrentScene = _game;
            _game.SetProcess(false);
            await Frames(3);
            Check(GetTree().GetNodesInGroup("towers").Count == 3, "Three distinct towers in main scene");
            foreach (Node node in GetTree().GetNodesInGroup("enemies")) node.QueueFree();
            foreach (Node node in GetTree().GetNodesInGroup("towers")) node.QueueFree();
            await Frames(3);
            var player = (Actor)GetTree().GetFirstNodeInGroup("player");
            player.GetNode<PlayerControllerComponent>("PlayerControllerComponent").SetPhysicsProcess(false);
            var build = player.GetNode<BuildComponent>("BuildComponent");
            int metal = _game.Metal;
            build._UnhandledInput(new InputEventAction { Action = "tower_1", Pressed = true });
            Check(build.IsBuilding, "Player starts ghost preview");
            build._UnhandledInput(new InputEventAction { Action = "build", Pressed = true });
            Check(!build.IsBuilding && _game.Metal == metal - 50, "Construction places tower and charges scene price");
            build._UnhandledInput(new InputEventAction { Action = "tower_1", Pressed = true });
            build._UnhandledInput(new InputEventAction { Action = "build", Pressed = true });
            Check(build.IsBuilding && _game.Metal == metal - 50, "Occupied position rejected without charging");
            build.StopBuilding();
            foreach (Node node in GetTree().GetNodesInGroup("towers")) node.QueueFree();
            await Frames(3);
            var victim = Spawn("Crawler", new Vector3(10, 0.7f, 10));
            var damage = new DamagingComponent { Damage = 20, OneShot = false, CollisionMask = 0 };
            _game.AddChild(damage);
            Check(!damage.TryHit(player), "Friendly fire filtered by groups");
            float hp = victim.Health.CurrentHealth;
            Check(damage.TryHit(victim) && victim.Health.CurrentHealth == hp - 20, "Shared component applies damage");
            Check(!damage.TryHit(victim), "Contact cooldown prevents repeated damage in same frame");
            damage.QueueFree();
            var freeze = new FreezeComponent { CollisionMask = 0, FreezeDuration = 0.05f };
            _game.AddChild(freeze);
            freeze.ApplyDamage(victim);
            Check(victim.GetNode<MoveComponent>("MoveComponent").IsSlowed, "Freeze slows MoveComponent");
            await Frames(12);
            Check(!victim.GetNode<MoveComponent>("MoveComponent").IsSlowed, "Freeze expires and movement recovers");
            freeze.QueueFree();
            victim.QueueFree();
            await Frames(3);
            var first = Spawn("Crawler", new Vector3(10, 0.7f, 10));
            var second = Spawn("Crawler", new Vector3(12, 0.7f, 10));
            var explosion = new ExplosionComponent { Damage = 25, CollisionMask = 2 };
            _game.AddChild(explosion);
            explosion.GlobalPosition = first.GlobalPosition;
            explosion.ApplyDamage(first);
            await Frames(12);
            Check(first.Health.CurrentHealth == 45 && second.Health.CurrentHealth == 45, "Expanded blast damages multiple targets exactly once");
            explosion.QueueFree();
            first.QueueFree(); second.QueueFree();
            await Frames(3);
            var crawler = Spawn("Crawler", player.GlobalPosition + new Vector3(1, 0, 0));
            hp = player.Health.CurrentHealth;
            await Frames(12);
            Check(player.Health.CurrentHealth < hp, "Crawler damages player through actual Area3D contact");
            metal = _game.Metal;
            crawler.Health.TakeDamage(1000);
            await Frames(3);
            Check(_game.Metal == metal + crawler.MetalReward, "Enemy destruction credits metal once");
            var tower = Spawn("StandardTower", new Vector3(-10, 0.7f, 10));
            metal = _game.Metal;
            int reward = tower.MetalReward;
            tower.Health.TakeDamage(1000);
            await Frames(3);
            Check(_game.Metal == metal + reward, "Tower destruction credits metal");
            var bulletTarget = Spawn("Crawler", new Vector3(-12, 0.7f, -12));
            player.GlobalPosition = bulletTarget.GlobalPosition + new Vector3(0, 0, 5);
            Check(player.GetNode<ShootComponent>("ShootComponent").TryShoot(Vector3.Forward), "Player weapon spawns projectile");
            await Frames(25);
            Check(bulletTarget.Health.CurrentHealth < bulletTarget.Health.MaxHealth, "Moving bullet inflicts damage through its child component");
            bulletTarget.QueueFree();
            metal = _game.Metal;
            Check(!_game.TrySpend(metal + 1) && _game.Metal == metal, "Insufficient metal does not change balance");
            var veteran = Spawn("Shooter", new Vector3(-18, 0.7f, -18), 6);
            var veteranWeapon = veteran.GetNode<ShootComponent>("ShootComponent");
            Check(Mathf.IsEqualApprox(veteran.Health.MaxHealth, 202.5f)
                && veteran.Health.CurrentHealth == veteran.Health.MaxHealth,
                "Later waves spawn enemies at full scaled health");
            Check(veteranWeapon.Range == 21.5f
                && veteran.GetNode<EnemyBrainComponent>("EnemyBrainComponent").StopDistance == 20,
                "Wave six increases shooting and stopping ranges together");
            Check(veteran.GetNode<MoveComponent>("MoveComponent").Speed > 1.8f
                && veteranWeapon.FireInterval < 1.4f, "Veterans move and fire faster");
            player.GlobalPosition = veteran.GlobalPosition + new Vector3(0, 0, 20);
            int shots = GetTree().GetNodesInGroup("projectiles").Count;
            hp = player.Health.CurrentHealth;
            veteranWeapon._PhysicsProcess(0);
            Check(GetTree().GetNodesInGroup("projectiles").Count == shots + 1,
                "Later-wave shooter acquires a target twenty units away");
            await Frames(100);
            Check(Mathf.IsEqualApprox(hp - player.Health.CurrentHealth, 21),
                "Long-range bullet reaches target and carries increased damage");
            veteran.QueueFree();
            var veteranCrawler = Spawn("Crawler", new Vector3(18, 0.7f, 18), 6);
            Check(Mathf.IsEqualApprox(veteranCrawler.GetNode<DamagingComponent>("DamagingComponent").Damage, 17.5f),
                "Crawler contact damage also increases with wave level");
            veteranCrawler.QueueFree();
            var core = (Actor)GetTree().GetFirstNodeInGroup("core");
            core.Health.TakeDamage(10000);
            Check(_game.IsFinished && GetTree().Paused, "Core destruction ends and pauses game");
            GetTree().Paused = false;
            _game.QueueFree();
            await Frames(3);
            _game = GD.Load<PackedScene>("res://Scenes/Main.tscn").Instantiate<GameManager>();
            GetTree().Root.AddChild(_game);
            GetTree().CurrentScene = _game;
            _game.SetProcess(false);
            player = (Actor)GetTree().GetFirstNodeInGroup("player");
            player.Health.TakeDamage(10000);
            Check(_game.IsFinished && GetTree().Paused, "Player death ends game");
            GetTree().Paused = false;
            _game.QueueFree();
            await Frames(3);
            _game = GD.Load<PackedScene>("res://Scenes/Main.tscn").Instantiate<GameManager>();
            GetTree().Root.AddChild(_game);
            _game.SetProcess(false);
            GetTree().CurrentScene = _game;
            foreach (Node enemy in GetTree().GetNodesInGroup("enemies")) enemy.QueueFree();
            await Frames(3);
            _game._Process(100);
            for (int i = 0; i < 10; i++) _game._Process(1);
            Check(_game.Wave == 1 && _game.EnemyCount == 10, "First wave spawns ten enemies");
            foreach (Node enemy in GetTree().GetNodesInGroup("enemies")) enemy.QueueFree();
            await Frames(3);
            _game._Process(100);
            for (int i = 0; i < 15; i++) _game._Process(1);
            Check(_game.Wave == 2 && _game.EnemyCount == 15, "Second wave spawns fifteen stronger enemies");
            foreach (Node enemy in GetTree().GetNodesInGroup("enemies")) enemy.QueueFree();
            await Frames(3);
            _game.TotalWaves = 2;
            _game._Process(0);
            Check(_game.IsFinished && _game.Result.StartsWith("Победа"), "Clearing final wave wins game");
            GetTree().Paused = false;
            GD.Print($"PASS: {_checks} integration checks");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PrintErr(error);
            GetTree().Quit(1);
        }
    }
    private Actor Spawn(string scene, Vector3 position, int waveLevel = 1)
    {
        var actor = GD.Load<PackedScene>($"res://Scenes/Actors/{scene}.tscn").Instantiate<Actor>();
        var scaling = actor.GetNodeOrNull<EnemyScalingComponent>("EnemyScalingComponent");
        if (scaling != null) scaling.WaveLevel = waveLevel;
        _game.AddChild(actor);
        actor.GlobalPosition = position;
        actor.GetNodeOrNull<EnemyBrainComponent>("EnemyBrainComponent")?.SetPhysicsProcess(false);
        actor.GetNodeOrNull<ShootComponent>("ShootComponent")?.SetPhysicsProcess(false);
        return actor;
    }
    private async Task Frames(int count)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }
    private void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + description);
        _checks++;
        GD.Print("PASS: " + description);
    }
}
