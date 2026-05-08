$sourceDir = "c:\ALLFileDir_CSQ\01_GAME\RE_RimWorld\MY-Mod\RimWorld-RimMind-Mod\RimMind-Core\Source"
$replacements = @{
    'using RimMind.Core.Internal;' = 'using RimMind.Contracts.Internal;'
    'using RimMind.Core.Extension;' = 'using RimMind.Contracts.Extension;'
    'using RimMind.Core.Extensions;' = 'using RimMind.Contracts.Extensions;'
    'using RimMind.Core.Flywheel;' = 'using RimMind.Contracts.Flywheel;'
    'using RimMind.Core.UI;' = 'using RimMind.Contracts.UI;'
    'using RimMind.Core.Sensor;' = 'using RimMind.Kernel.Context;'
}

Get-ChildItem $sourceDir -Recurse -Filter "*.cs" | ForEach-Object {
    $content = [System.IO.File]::ReadAllText($_.FullName)
    $modified = $false
    foreach ($old in $replacements.Keys) {
        if ($content.Contains($old)) {
            $content = $content.Replace($old, $replacements[$old])
            $modified = $true
        }
    }
    if ($modified) {
        [System.IO.File]::WriteAllText($_.FullName, $content)
        Write-Host "Updated: $($_.Name)"
    }
}
