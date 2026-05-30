param(
    [switch]$UseSymbolicLink
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$target = Join-Path $repoRoot '.agents\skills'
$entries = @(
    '.claude\skills',
    '.codex\skills',
    '.antigravitycli\skills'
)

if (-not (Test-Path -LiteralPath $target -PathType Container)) {
    throw "找不到 skills 來源目錄：$target"
}

$itemType = if ($UseSymbolicLink) { 'SymbolicLink' } else { 'Junction' }

foreach ($entry in $entries) {
    $path = Join-Path $repoRoot $entry
    $parent = Split-Path -Parent $path

    New-Item -ItemType Directory -Path $parent -Force | Out-Null

    if (Test-Path -LiteralPath $path) {
        $item = Get-Item -LiteralPath $path -Force
        $existingTargets = @($item.Target)
        $isExpectedLink = $item.LinkType -in @('Junction', 'SymbolicLink') -and
            $existingTargets -contains $target

        if ($isExpectedLink) {
            Write-Host "已存在：$entry -> $target"
            continue
        }

        throw "路徑已存在但不是指向 .agents\skills 的連結：$path"
    }

    New-Item -ItemType $itemType -Path $path -Target $target | Out-Null
    Write-Host "已建立：$entry -> $target"
}
