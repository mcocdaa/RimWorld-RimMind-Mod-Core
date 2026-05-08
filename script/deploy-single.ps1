# Deploy this RimMind mod to RimWorld Mods folder.
# Place at: RimMind-*/script/deploy-single.ps1
#
# Usage:
#   .\script\deploy-single.ps1
#   .\script\deploy-single.ps1 "D:\Games\RimWorld"
#   $env:RIMWORLD_PATH="D:\Games\RimWorld"; .\script\deploy-single.ps1

param(
    [string]$RimWorldDir = ""
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ModDir    = Resolve-Path (Join-Path $ScriptDir "..")
$ModName   = Split-Path -Leaf $ModDir

# 自动检测常见 RimWorld 安装路径
function Detect-RimWorldPath {
    $paths = @(
        "C:\Program Files (x86)\Steam\steamapps\common\RimWorld",
        "C:\Program Files\Steam\steamapps\common\RimWorld",
        "$env:USERPROFILE\scoop\apps\steam\current\steamapps\common\RimWorld"
    )
    foreach ($path in $paths) {
        if (Test-Path $path) {
            return $path
        }
    }
    return $null
}

# 解析 RimWorld 路径
if ($env:RIMWORLD_PATH) {
    $RimWorldDir = $env:RIMWORLD_PATH
} elseif ($RimWorldDir -eq "") {
    $RimWorldDir = Detect-RimWorldPath
    if (-not $RimWorldDir) {
        Write-Error "Cannot find RimWorld installation"
        Write-Host "  Usage: .\script\deploy-single.ps1 [RimWorldPath]" -ForegroundColor Yellow
        Write-Host "  Or:    `$env:RIMWORLD_PATH='D:\Games\RimWorld'; .\script\deploy-single.ps1" -ForegroundColor Yellow
        exit 1
    }
}

if (-not (Test-Path $RimWorldDir)) {
    Write-Error "RimWorld directory not found: $RimWorldDir"
    exit 1
}

$RimWorldMods = Join-Path $RimWorldDir "Mods"

# 确保 Mods 目录存在
if (-not (Test-Path $RimWorldMods)) {
    Write-Error "Mods directory not found at $RimWorldMods"
    exit 1
}

# 构建
$SourceDir = Join-Path $ModDir "Source"

if ($ModName -eq "RimMind-Core") {
    $contractsCsproj = Join-Path $SourceDir "Contracts\RimMindCore.Contracts.csproj"
    $kernelCsproj = Join-Path $SourceDir "Kernel\RimMindCore.Kernel.csproj"
    $coreCsproj = Get-ChildItem -Path $SourceDir -Filter "RimMindCore.csproj" -File -ErrorAction SilentlyContinue | Select-Object -First 1

    if (-not (Test-Path $contractsCsproj)) {
        Write-Error "Contracts csproj not found: $contractsCsproj"
        exit 1
    }
    if (-not (Test-Path $kernelCsproj)) {
        Write-Error "Kernel csproj not found: $kernelCsproj"
        exit 1
    }
    if (-not $coreCsproj) {
        Write-Error "Core csproj not found in $SourceDir"
        exit 1
    }

    Write-Host "=== Building $ModName (Contracts) ===" -ForegroundColor Cyan
    dotnet build $contractsCsproj -c Release --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { Write-Error "Contracts build failed"; exit $LASTEXITCODE }
    Write-Host "  Contracts build successful"

    Write-Host "=== Building $ModName (Kernel) ===" -ForegroundColor Cyan
    dotnet build $kernelCsproj -c Release --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { Write-Error "Kernel build failed"; exit $LASTEXITCODE }
    Write-Host "  Kernel build successful"

    Write-Host "=== Building $ModName (Core) ===" -ForegroundColor Cyan
    dotnet build $coreCsproj.FullName -c Release --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { Write-Error "Core build failed"; exit $LASTEXITCODE }
    Write-Host "  Core build successful"
} else {
    $CSPROJ = Get-ChildItem -Path $SourceDir -Filter "*.csproj" -File -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($CSPROJ) {
        Write-Host "=== Building $ModName ===" -ForegroundColor Cyan
        dotnet build $CSPROJ.FullName -c Release --nologo -v quiet
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Build failed"
            exit $LASTEXITCODE
        }
        Write-Host "  Build successful"
    } else {
        Write-Host "No .csproj found in Source\, skipping build" -ForegroundColor Yellow
    }
}

# 部署
$DestDir = Join-Path $RimWorldMods $ModName
Write-Host "=== Deploying $ModName -> $DestDir ===" -ForegroundColor Cyan

$Exclude = @('Sources', 'Tests', '*.csproj', '*.user', 'obj', '.git', '.gitignore', 'script')
$SourceItems = Get-ChildItem -Path $ModDir -Exclude $Exclude

if (Test-Path $DestDir) {
    Get-ChildItem -Path $DestDir | Remove-Item -Recurse -Force
}

foreach ($item in $SourceItems) {
    Copy-Item -Path $item.FullName -Destination $DestDir -Recurse -Force
}

# 验证部署
if (Test-Path $DestDir) {
    Write-Host "  Done: $DestDir" -ForegroundColor Green
} else {
    Write-Error "Deployment verification failed"
    exit 1
}
