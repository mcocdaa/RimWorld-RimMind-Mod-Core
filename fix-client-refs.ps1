$sourceDir = "c:\ALLFileDir_CSQ\01_GAME\RE_RimWorld\MY-Mod\RimWorld-RimMind-Mod\RimMind-Core\Source"
Get-ChildItem $sourceDir -Recurse -Filter "*.cs" | ForEach-Object {
    $content = [System.IO.File]::ReadAllText($_.FullName)
    $modified = $false
    if ($content.Contains("Client.StructuredTool")) {
        $content = $content.Replace("Client.StructuredTool", "StructuredTool")
        $modified = $true
    }
    if ($content.Contains("Client.ChatMessage")) {
        $content = $content.Replace("Client.ChatMessage", "ChatMessage")
        $modified = $true
    }
    if ($content.Contains("Client.ChatToolCall")) {
        $content = $content.Replace("Client.ChatToolCall", "ChatToolCall")
        $modified = $true
    }
    if ($content.Contains("Client.StructuredToolCall")) {
        $content = $content.Replace("Client.StructuredToolCall", "StructuredToolCall")
        $modified = $true
    }
    if ($modified) {
        [System.IO.File]::WriteAllText($_.FullName, $content)
        Write-Host "Updated: $($_.Name)"
    }
}
