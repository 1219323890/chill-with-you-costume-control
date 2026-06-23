[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src\Sherry.CostumeControl\Sherry.CostumeControl.csproj"

dotnet build $project -c Release
