from pathlib import Path

def write(path, text):
    p = Path(path)
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(text.strip() + '\n', encoding='utf-8')

write('project.godot', '''config_version=5
[application]
config/name="Core Defense — Лабораторная 3"
run/main_scene="res://Scenes/Main.tscn"
config/features=PackedStringArray("4.7", "C#", "GL Compatibility")
config/icon="res://icon.svg"
[display]
window/size/viewport_width=1280
window/size/viewport_height=720
window/stretch/mode="canvas_items"
[dotnet]
project/assembly_name="TowerDefense"
[rendering]
renderer/rendering_method="gl_compatibility"
environment/defaults/default_clear_color=Color(0.025, 0.04, 0.07, 1)
[input]
''')
actions = {'move_left':[65], 'move_right':[68], 'move_up':[87], 'move_down':[83], 'fire':[32], 'build':[69], 'cancel_build':[4194305], 'tower_1':[49], 'tower_2':[50], 'tower_3':[51], 'pause':[80], 'restart':[82]}
with Path('project.godot').open('a', encoding='utf-8') as f:
    for action, keys in actions.items():
        events = [f'Object(InputEventKey,"physical_keycode":{key})' for key in keys]
        if action == 'fire': events.append('Object(InputEventMouseButton,"button_index":1)')
        f.write(f'{action}={{\n"deadzone":0.2,\n"events":[{", ".join(events)}]\n}}\n')

def projectile(name, component, enemy=False):
    color = 'Color(1, 0.22, 0.16, 1)' if enemy else 'Color(0.5, 1, 0.65, 1)'
    write(f'Scenes/Projectiles/{name}.tscn', f'''[gd_scene load_steps=6 format=3]
[ext_resource type="Script" path="res://Scripts/Projectile.cs" id="1"]
[ext_resource type="Script" path="res://Scripts/Components/{component}.cs" id="2"]
[ext_resource type="Script" path="res://Scripts/Components/VisualComponent.cs" id="3"]
[sub_resource type="SphereShape3D" id="Shape"]
radius=0.3
[sub_resource type="StandardMaterial3D" id="Unused"]
[node name="{name}" type="Node3D" groups=["projectiles"]]
script=ExtResource("1")
Speed={14 if enemy else 19}.0
[node name="VisualComponent" type="Node3D" parent="."]
script=ExtResource("3")
Kind="bullet"
Tint={color}
[node name="DamagingComponent" type="Area3D" parent="."]
collision_layer=0
collision_mask={1 if enemy else 2}
script=ExtResource("2")
Damage={12 if enemy else 30 if component == 'ExplosionComponent' else 10 if component == 'FreezeComponent' else 20}.0
TargetGroups=Array[StringName]([&"{'allies' if enemy else 'enemies'}"])
[node name="CollisionShape3D" type="CollisionShape3D" parent="DamagingComponent"]
shape=SubResource("Shape")''')

for args in [('StandardBullet','DamagingComponent'),('ExplosiveBullet','ExplosionComponent'),('FreezeBullet','FreezeComponent'),('EnemyBullet','DamagingComponent',True)]: projectile(*args)

def actor(name, groups, kind, hp, enemy=False, speed=None, bullet=None, cost=50, reward=12, player=False):
    components = ([('EnemyScalingComponent','Node')] if enemy else []) + [('VisualComponent','Node3D'),('HealthComponent','Node')]
    if speed is not None: components += [('MoveComponent','Node')]
    if bullet: components += [('ShootComponent','Node')]
    if enemy: components += [('EnemyBrainComponent','Node')]
    if player: components += [('PlayerControllerComponent','Node'),('BuildComponent','Node')]
    if kind == 'crawler': components += [('DamagingComponent','Area3D')]
    resources = ['[ext_resource type="Script" path="res://Scripts/Actor.cs" id="actor"]']
    for component,_ in components: resources.append(f'[ext_resource type="Script" path="res://Scripts/Components/{component}.cs" id="{component}"]')
    if bullet: resources.append(f'[ext_resource type="PackedScene" path="res://Scenes/Projectiles/{bullet}.tscn" id="bullet"]')
    if player:
        for i,t in enumerate(['StandardTower','ExplosiveTower','FreezeTower']): resources.append(f'[ext_resource type="PackedScene" path="res://Scenes/Actors/{t}.tscn" id="tower{i}"]')
    radius = 1.4 if kind == 'core' else 0.65
    subs = f'[sub_resource type="SphereShape3D" id="Body"]\nradius={radius}\n'
    if kind == 'crawler': subs += '[sub_resource type="SphereShape3D" id="Contact"]\nradius=1.15\n'
    color = 'Color(1, 0.25, 0.18, 1)' if enemy else 'Color(0.4, 0.7, 1, 1)' if kind == 'core' else 'Color(0.5, 0.95, 0.2, 1)' if kind == 'explosive' else 'Color(0.3, 1, 0.75, 1)' if kind == 'freeze' else 'Color(0.2, 0.85, 0.4, 1)'
    text = f'[gd_scene format=3]\n' + '\n'.join(resources) + '\n' + subs
    text += f'[node name="{name}" type="CharacterBody3D" groups={str(groups + ["actors"]).replace(chr(39), chr(34))}]\ncollision_layer={2 if enemy else 1}\ncollision_mask=3\nscript=ExtResource("actor")\nMetalReward={reward}\nBuildCost={cost}\n'
    text += '[node name="CollisionShape3D" type="CollisionShape3D" parent="."]\nshape=SubResource("Body")\n'
    for component,node_type in components:
        text += f'[node name="{component}" type="{node_type}" parent="."]\nscript=ExtResource("{component}")\n'
        if component == 'VisualComponent': text += f'Kind="{kind}"\nTint={color}\n'
        if component == 'HealthComponent': text += f'MaxHealth={hp}.0\n'
        if component == 'MoveComponent': text += f'Speed={speed}\n'
        if component == 'ShootComponent':
            text += f'ProjectileScene=ExtResource("bullet")\nAutomatic={str(not player).lower()}\nTargetGroup=&"{"allies" if enemy else "enemies"}"\nFireInterval={1.4 if enemy else 0.22 if player else 1.3 if kind == "explosive" else 0.75 if kind == "freeze" else 0.55}\nRange={14 if enemy else 13}.0\n'
        if component == 'EnemyBrainComponent': text += f'StopDistance={12.5 if bullet else 1.1}\n'
        if component == 'BuildComponent': text += 'TowerScenes=Array[PackedScene]([ExtResource("tower0"), ExtResource("tower1"), ExtResource("tower2")])\n'
        if component == 'DamagingComponent': text += 'collision_layer=0\ncollision_mask=1\nTargetGroups=Array[StringName]([&"allies"])\nDamage=10.0\nOneShot=false\nDestroyOwner=false\n[node name="CollisionShape3D" type="CollisionShape3D" parent="DamagingComponent"]\nshape=SubResource("Contact")\n'
    write(f'Scenes/Actors/{name}.tscn', text)

actor('StandardTower',['allies','towers'],'standard',140,bullet='StandardBullet',cost=50,reward=20)
actor('ExplosiveTower',['allies','towers'],'explosive',170,bullet='ExplosiveBullet',cost=80,reward=30)
actor('FreezeTower',['allies','towers'],'freeze',120,bullet='FreezeBullet',cost=70,reward=25)
actor('Player',['allies','player'],'player',120,speed=6.5,bullet='StandardBullet',player=True)
actor('Core',['allies','core'],'core',500)
actor('Crawler',['enemies'],'crawler',70,enemy=True,speed=2.6,reward=15)
actor('Shooter',['enemies'],'shooter',90,enemy=True,speed=1.8,bullet='EnemyBullet',reward=20)

write('Scenes/Main.tscn', '''[gd_scene format=3]
[ext_resource type="Script" path="res://Scripts/GameManager.cs" id="manager"]
[ext_resource type="Script" path="res://Scripts/Hud.cs" id="hud"]
[ext_resource type="PackedScene" path="res://Scenes/Actors/Player.tscn" id="player"]
[ext_resource type="PackedScene" path="res://Scenes/Actors/Core.tscn" id="core"]
[ext_resource type="PackedScene" path="res://Scenes/Actors/StandardTower.tscn" id="tower1"]
[ext_resource type="PackedScene" path="res://Scenes/Actors/ExplosiveTower.tscn" id="tower2"]
[ext_resource type="PackedScene" path="res://Scenes/Actors/FreezeTower.tscn" id="tower3"]
[ext_resource type="PackedScene" path="res://Scenes/Actors/Crawler.tscn" id="crawler"]
[ext_resource type="PackedScene" path="res://Scenes/Actors/Shooter.tscn" id="shooter"]
[sub_resource type="Environment" id="Environment"]
background_mode=1
background_color=Color(0.025, 0.04, 0.07, 1)
ambient_light_source=3
ambient_light_color=Color(0.65, 0.8, 1, 1)
ambient_light_energy=0.7
[sub_resource type="StandardMaterial3D" id="GroundMaterial"]
albedo_color=Color(0.055, 0.09, 0.13, 1)
[sub_resource type="BoxMesh" id="Ground"]
size=Vector3(44, 0.3, 44)
material=SubResource("GroundMaterial")
[node name="GameManager" type="Node3D"]
script=ExtResource("manager")
EnemyScenes=Array[PackedScene]([ExtResource("crawler"), ExtResource("shooter")])
[node name="WorldEnvironment" type="WorldEnvironment" parent="."]
environment=SubResource("Environment")
[node name="Sun" type="DirectionalLight3D" parent="."]
rotation_degrees=Vector3(-55, -25, 0)
light_energy=1.2
shadow_enabled=true
[node name="Camera3D" type="Camera3D" parent="."]
position=Vector3(0, 34, 25)
rotation_degrees=Vector3(-53.7, 0, 0)
projection=1
size=58.0
current=true
[node name="Ground" type="MeshInstance3D" parent="."]
position=Vector3(0, -0.2, 0)
mesh=SubResource("Ground")
[node name="Core" parent="." instance=ExtResource("core")]
position=Vector3(0, 0.7, 0)
[node name="Player" parent="." instance=ExtResource("player")]
position=Vector3(0, 0.7, 5)
[node name="StandardTower" parent="." instance=ExtResource("tower1")]
position=Vector3(-5, 0.7, -3)
[node name="ExplosiveTower" parent="." instance=ExtResource("tower2")]
position=Vector3(5, 0.7, -3)
[node name="FreezeTower" parent="." instance=ExtResource("tower3")]
position=Vector3(0, 0.7, -6)
[node name="Crawler" parent="." instance=ExtResource("crawler")]
position=Vector3(-15, 0.7, -12)
[node name="Shooter" parent="." instance=ExtResource("shooter")]
position=Vector3(15, 0.7, -12)
[node name="HUD" type="CanvasLayer" parent="."]
script=ExtResource("hud")
''')
