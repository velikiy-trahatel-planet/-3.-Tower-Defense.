param([string]$Repository = 'https://github.com/velikiy-trahatel-planet/-3.-Tower-Defense.')
$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path $PSScriptRoot -Parent
$stagingRoot = Join-Path $sourceRoot ('.pipeline/publish-' + [Guid]::NewGuid().ToString('N'))

function Invoke-Git {
    param([string[]]$Arguments)
    & git @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Git failed: $($Arguments -join ' ')" }
}

# Clone first to retain the existing remote history and any unrelated files.
Invoke-Git -Arguments @('clone', '--branch', 'main', $Repository, $stagingRoot)
$items = @('Scenes', 'Scripts', 'Tests', 'tools', 'README.md', 'TowerDefense.csproj', 'project.godot', 'icon.svg', '.editorconfig', '.gitattributes', '.gitignore')
foreach ($item in $items) {
    Copy-Item -LiteralPath (Join-Path $sourceRoot $item) -Destination $stagingRoot -Recurse -Force
}
Get-ChildItem -LiteralPath $sourceRoot -Filter '*.md' | Where-Object Name -ne 'README.md' | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $stagingRoot -Force
}
New-Item -ItemType Directory -Path (Join-Path $stagingRoot '.pipeline') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $sourceRoot '.pipeline/create_scenes.py') -Destination (Join-Path $stagingRoot '.pipeline') -Force
Push-Location $stagingRoot
try {
    Invoke-Git -Arguments @('add', '--all')
    $changes = & git status --porcelain
    if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect Git status' }
    if ($changes) {
        Invoke-Git -Arguments @('commit', '-m', 'Implement Tower Defense laboratory with components and metal economy')
        Invoke-Git -Arguments @('push', 'origin', 'HEAD:main')
    }
    Write-Host "Published: $Repository"
    Write-Host "Local checkout: $stagingRoot"
} finally {
    Pop-Location
}
