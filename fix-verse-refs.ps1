$sourceDir = "c:\ALLFileDir_CSQ\01_GAME\RE_RimWorld\MY-Mod\RimWorld-RimMind-Mod\RimMind-Core\Source"
Get-ChildItem $sourceDir -Recurse -Filter "*.cs" | ForEach-Object {
    $content = [System.IO.File]::ReadAllText($_.FullName)
    $modified = $false
    if ($content.Contains("Verse.UI.")) {
        $content = $content.Replace("Verse.UI.", "global::Verse.UI.")
        $modified = $true
    }
    if ($content.Contains("Verse.Log.")) {
        $content = $content.Replace("Verse.Log.", "global::Verse.Log.")
        $modified = $true
    }
    if ($content.Contains("Verse.Scribe_")) {
        $content = $content.Replace("Verse.Scribe_", "global::Verse.Scribe_")
        $modified = $true
    }
    if ($content.Contains("Verse.Find.")) {
        $content = $content.Replace("Verse.Find.", "global::Verse.Find.")
        $modified = $true
    }
    if ($modified) {
        [System.IO.File]::WriteAllText($_.FullName, $content)
        Write-Host "Updated: $($_.Name)"
    }
}
