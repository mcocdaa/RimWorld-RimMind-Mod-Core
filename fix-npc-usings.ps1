$sourceDir = "c:\ALLFileDir_CSQ\01_GAME\RE_RimWorld\MY-Mod\RimWorld-RimMind-Mod\RimMind-Core\Source"
Get-ChildItem $sourceDir -Recurse -Filter "*.cs" | ForEach-Object {
    $content = [System.IO.File]::ReadAllText($_.FullName)
    $modified = $false
    if ($content.Contains("using RimMind.Core.Npc;") -and -not $content.Contains("using RimMind.Contracts.Npc;")) {
        $content = $content.Replace("using RimMind.Core.Npc;", "using RimMind.Contracts.Npc;")
        $modified = $true
    }
    if ($modified) {
        [System.IO.File]::WriteAllText($_.FullName, $content)
        Write-Host "Updated: $($_.Name)"
    }
}
