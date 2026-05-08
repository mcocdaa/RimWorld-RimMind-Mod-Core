$sourceDir = "c:\ALLFileDir_CSQ\01_GAME\RE_RimWorld\MY-Mod\RimWorld-RimMind-Mod\RimMind-Core\Source"

$usingFixes = @(
    @{ File = "Core\Agent\PawnPerceiver.cs"; Usings = @("using RimMind.Kernel.Bus;") },
    @{ File = "Core\Agent\PawnActor.cs"; Usings = @("using RimMind.Kernel.Bus;") },
    @{ File = "Core\Agent\PawnRecorder.cs"; Usings = @("using RimMind.Kernel.Bus;") },
    @{ File = "Core\Agent\PawnThinker.cs"; Usings = @("using RimMind.Kernel.Bus;") },
    @{ File = "Core\Agent\PawnAgent.cs"; Usings = @("using RimMind.Kernel.Bus;") },
    @{ File = "Core\Perception\PerceptionBridge.cs"; Usings = @("using RimMind.Kernel.Bus;") },
    @{ File = "Core\Runtime\RimMindRuntime.cs"; Usings = @("using RimMind.Kernel.Bus;") },
    @{ File = "Core\Runtime\RuntimeOverrides.cs"; Usings = @("using RimMind.Kernel.Bus;") },
    @{ File = "Core\AIDebugLog.cs"; Usings = @("using RimMind.Contracts.Internal;") },
    @{ File = "Core\Sensor\SensorManager.cs"; Usings = @("using RimMind.Contracts.Sensor;") },
    @{ File = "Settings\AICoreSettings.cs"; Usings = @("using RimMind.Contracts;") },
    @{ File = "Npc\INpcManager.cs"; Usings = @("using RimMind.Contracts.Npc;") },
    @{ File = "Npc\NpcManager.cs"; Usings = @("using RimMind.Contracts.Npc;") },
    @{ File = "Npc\ResponseDispatcher.cs"; Usings = @("using RimMind.Contracts.Npc;") },
    @{ File = "Npc\StorageDriverFactory.cs"; Usings = @("using RimMind.Contracts;") },
    @{ File = "RimMindAPI.cs"; Usings = @("using RimMind.Kernel.Bus;") }
)

foreach ($fix in $usingFixes) {
    $filePath = Join-Path $sourceDir $fix.File
    if (Test-Path $filePath) {
        $content = [System.IO.File]::ReadAllText($filePath)
        $modified = $false
        foreach ($using in $fix.Usings) {
            if (-not $content.Contains($using)) {
                $firstUsingIndex = $content.IndexOf("using ")
                if ($firstUsingIndex -ge 0) {
                    $lineEnd = $content.IndexOf("`n", $firstUsingIndex)
                    $content = $content.Insert($lineEnd + 1, $using + "`r`n")
                    $modified = $true
                }
            }
        }
        if ($modified) {
            [System.IO.File]::WriteAllText($filePath, $content)
            Write-Host "Updated: $($fix.File)"
        } else {
            Write-Host "No changes: $($fix.File)"
        }
    } else {
        Write-Host "Not found: $($fix.File)"
    }
}
