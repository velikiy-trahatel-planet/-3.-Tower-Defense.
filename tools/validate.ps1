param([string]$Godot = 'C:\Users\GL_11_09_2025\Desktop\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe')
$ErrorActionPreference = 'Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    & dotnet build
    if ($LASTEXITCODE -ne 0) { throw 'C# build failed' }
    & $Godot --headless --path . --log-file .godot/integration.log res://Tests/IntegrationTests.tscn
    if ($LASTEXITCODE -ne 0) { throw 'Integration tests failed' }
    $log = Get-Content .godot/integration.log -Raw
    if ($log -notmatch 'PASS: 27 integration checks') { throw 'Integration tests did not complete' }
    if ($log -match 'SCRIPT ERROR|FAIL:|C# backtrace') { throw 'Runtime errors detected' }
} finally {
    Pop-Location
}
