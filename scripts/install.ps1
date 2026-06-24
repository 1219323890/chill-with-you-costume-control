[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$propsPath = Join-Path $repoRoot "GameReferences.props"

if (!(Test-Path $propsPath)) {
    throw "Missing GameReferences.props. Copy GameReferences.props.example to GameReferences.props and set GameRoot."
}

[xml]$props = Get-Content -Raw -Encoding UTF8 $propsPath
$gameRoot = $props.Project.PropertyGroup.GameRoot
if ([string]::IsNullOrWhiteSpace($gameRoot)) {
    throw "GameRoot is missing in GameReferences.props."
}

$project = Join-Path $repoRoot "src\Sherry.CostumeControl\Sherry.CostumeControl.csproj"
dotnet build $project -c Release

$dll = Join-Path $repoRoot "src\Sherry.CostumeControl\bin\Release\net472\Sherry.CostumeControl.dll"
$targetDir = Join-Path $gameRoot "BepInEx\plugins"
New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
Copy-Item -LiteralPath $dll -Destination (Join-Path $targetDir "Sherry.CostumeControl.dll") -Force

Write-Host "Installed to: $targetDir"
