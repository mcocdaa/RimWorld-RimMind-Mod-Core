$sourceDir = "c:\ALLFileDir_CSQ\01_GAME\RE_RimWorld\MY-Mod\RimWorld-RimMind-Mod\RimMind-Core\Source"
$fixes = @(
    @{ File = "Adapters\UI\RequestOverlay.cs"; AddUsing = "using RimMind.Contracts.UI;" },
    @{ File = "Core\Internal\IOverlayService.cs"; AddUsing = "using RimMind.Contracts.UI;" },
    @{ File = "Core\Internal\OverlayService.cs"; AddUsing = "using RimMind.Contracts.UI;" },
    @{ File = "Npc\HybridStorageDriver.cs"; AddUsing = "using RimMind.Contracts.Npc;" },
    @{ File = "Npc\LocalStorageDriver.cs"; AddUsing = "using RimMind.Contracts.Npc;" },
    @{ File = "Npc\Player2StorageDriver.cs"; AddUsing = "using RimMind.Contracts.Npc;" },
    @{ File = "Npc\StorageDriverFactory.cs"; AddUsing = "using RimMind.Contracts.Npc;" },
    @{ File = "Core\Runtime\RimMindRuntime.cs"; AddUsing = "using RimMind.Contracts.Npc;" },
    @{ File = "RimMindAPI.cs"; AddUsing = "using RimMind.Contracts.Npc;" }
)

foreach ($fix in $fixes) {
    $filePath = Join-Path $sourceDir $fix.File
    if (Test-Path $filePath) {
        $content = [System.IO.File]::ReadAllText($filePath)
        if (-not $content.Contains($fix.AddUsing)) {
            $content = $fix.AddUsing + "`r`n" + $content
            [System.IO.File]::WriteAllText($filePath, $content)
            Write-Host "Added using to: $($fix.File)"
        } else {
            Write-Host "Already has using: $($fix.File)"
        }
    } else {
        Write-Host "File not found: $($fix.File)"
    }
}
