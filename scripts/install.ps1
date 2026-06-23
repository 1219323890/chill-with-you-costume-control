[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$propsPath = Join-Path $repoRoot "GameReferences.props"

if (!(Test-Path $propsPath)) {
    throw "缺少 GameReferences.props。请先复制 GameReferences.props.example 为 GameReferences.props，并确认 GameRoot 指向游戏安装目录。"
}

[xml]$props = Get-Content -Raw -Encoding UTF8 $propsPath
$gameRoot = $props.Project.PropertyGroup.GameRoot
if ([string]::IsNullOrWhiteSpace($gameRoot)) {
    throw "GameReferences.props 中缺少 GameRoot。"
}

$project = Join-Path $repoRoot "src\Sherry.CostumeControl\Sherry.CostumeControl.csproj"
dotnet build $project -c Release

$dll = Join-Path $repoRoot "src\Sherry.CostumeControl\bin\Release\net472\Sherry.CostumeControl.dll"
$targetDir = Join-Path $gameRoot "BepInEx\plugins"
New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
Copy-Item -LiteralPath $dll -Destination (Join-Path $targetDir "Sherry.CostumeControl.dll") -Force

Write-Host "已安装到：$targetDir"
