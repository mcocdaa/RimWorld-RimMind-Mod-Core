$sourceDir = "c:\ALLFileDir_CSQ\01_GAME\RE_RimWorld\MY-Mod\RimWorld-RimMind-Mod\RimMind-Core\Source"
Get-ChildItem $sourceDir -Recurse -Filter "*.cs" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw -Encoding UTF8
    if ($content -match 'using RimMind\.Core\.Client;') {
        $newContent = $content -replace 'using RimMind\.Core\.Client;', 'using RimMind.Contracts.Client;'
        [System.IO.File]::WriteAllText($_.FullName, $newContent)
        Write-Host "Updated: $($_.Name)"
    }
}
