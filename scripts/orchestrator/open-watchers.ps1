#Requires -Version 7.0
<#
.SYNOPSIS
Open one Windows Terminal tab per Workflow subagent, in ONE named window, and keep opening tabs as agents start.

.DESCRIPTION
Watching is a token-free VIEW of agents that keep running as background Workflow subagents
(docs/rearchitecture/DESIGN-orchestrator-loop.md section 13). Every -PollSeconds it scans for agent transcripts
(`agent-*.jsonl` under -WorkflowDir, or under every `subagents\workflows\wf_*` of -SessionDir) and opens a tab for each
new one whose meta.json has appeared:
    wt.exe -w cobol-agents new-tab --title "<letter> <lead>" --tabColor <sonnet blue | opus orange> pwsh -NoProfile -File watch-agent.ps1 <transcript>
A rolling wave starts agents as slots free, so it keeps scanning until it is stopped (the supervisor stops it when the
wave unit ends; the tabs stay open). A tab that fails to open is reported and retried on the next scan; a watcher
failure never touches the agents.

.EXAMPLE
pwsh -NoProfile -File scripts/orchestrator/open-watchers.ps1 -WorkflowDir C:\Users\brent\.claude\projects\E--COBOL\<session>\subagents\workflows\wf_dcb48a76-667
#>
param(
    [string]$WorkflowDir = '',
    [string]$SessionDir = '',
    [string]$WtExe = 'C:\Users\brent\AppData\Local\Microsoft\WindowsApps\wt.exe',
    [string]$Window = 'cobol-agents',
    [int]$PollSeconds = 5,
    [switch]$Once
)
if (-not $WorkflowDir -and -not $SessionDir) { throw 'give -WorkflowDir or -SessionDir' }
$Colors = @{ sonnet = '#1E66F5'; opus = '#FE640B' }
$watch = Join-Path $PSScriptRoot 'watch-agent.ps1'
$opened = @{}
while ($true) {
    $dirs = if ($WorkflowDir) { @($WorkflowDir) } else {
        @(Get-ChildItem -Directory -Path (Join-Path $SessionDir 'subagents/workflows') -Filter 'wf_*' -ErrorAction SilentlyContinue |
            ForEach-Object FullName) }
    foreach ($d in $dirs) {
        foreach ($t in @(Get-ChildItem -Path $d -Filter 'agent-*.jsonl' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime)) {
            if ($opened.ContainsKey($t.FullName)) { continue }
            $meta = [IO.Path]::ChangeExtension($t.FullName, '.meta.json')
            if (-not (Test-Path $meta)) { continue }      # the label and model come from it: wait one scan
            try {
                $m = Get-Content $meta -Raw | ConvertFrom-Json
                $title = (($m.description -replace '^impl-', '') -replace '-', ' ') -replace '[;"]', ''
                $model = [string]$m.model
                $color = if ($Colors.ContainsKey($model)) { $Colors[$model] } else { '#7F849C' }
                & $WtExe -w $Window new-tab --title $title --tabColor $color pwsh -NoProfile -File $watch $t.FullName
                if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw "wt.exe exit $LASTEXITCODE" }
                $opened[$t.FullName] = $true
                Write-Host "opened tab '$title' ($model) for $($t.Name)"
            } catch {
                Write-Host "could not open a tab for $($t.Name): $_ (retrying next scan)"
            }
        }
    }
    if ($Once) { break }
    Start-Sleep -Seconds $PollSeconds
}
