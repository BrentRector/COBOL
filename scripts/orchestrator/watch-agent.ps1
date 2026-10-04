#Requires -Version 7.0
<#
.SYNOPSIS
Watch one Workflow subagent in this terminal tab: read-only, spends no tokens, safe to start and stop at will.

.DESCRIPTION
The tab's half of watching (docs/rearchitecture/DESIGN-orchestrator-loop.md section 13): it waits for the transcript to
appear, titles the tab from the agent's sibling meta.json (`impl-X-PB1407` -> "X PB1407 (sonnet)"), switches the
console to UTF-8, and runs watch_agent.py, the renderer that follows the file. If the renderer exits unexpectedly it
is restarted, so a renderer fault never closes the tab; Ctrl+C ends it.

.EXAMPLE
pwsh -NoProfile -File scripts/orchestrator/watch-agent.ps1 C:\Users\brent\.claude\projects\E--COBOL\<session>\subagents\workflows\wf_x\agent-a1.jsonl
#>
param([Parameter(Mandatory)][string]$Transcript, [string]$Python = 'python')
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$meta = [IO.Path]::ChangeExtension($Transcript, '.meta.json')
while (-not (Test-Path $Transcript)) { Start-Sleep -Seconds 1 }
$label = [IO.Path]::GetFileNameWithoutExtension($Transcript)
if (Test-Path $meta) {
    try {
        $m = Get-Content $meta -Raw | ConvertFrom-Json
        $label = "$(($m.description -replace '^impl-', '') -replace '-', ' ') ($($m.model))"
    } catch { }
}
$Host.UI.RawUI.WindowTitle = $label
Write-Host "watching $label — $Transcript (Ctrl+C ends; read-only)"
$renderer = Join-Path $PSScriptRoot 'watch_agent.py'
while ($true) {
    & $Python $renderer $Transcript
    if ($LASTEXITCODE -eq 0) { break }     # Ctrl+C or a clean end
    Write-Host "renderer exited $LASTEXITCODE; restarting in 5 s"
    Start-Sleep -Seconds 5
}
