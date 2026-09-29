# PB1708 pivot evidence b6 — one measured run of the continuity cells (or a shard of them), cold, at BelowNormal.
#
#     pwsh -File run-leg.ps1 -Label E1 -Filter 'FullyQualifiedName~Cobol85Program_StillCompilesAtLaterEdition'
#     pwsh -File run-leg.ps1 -Label E2 -Filter '…' -EnvPairs 'DOTNET_gcServer=1'
#
# Build the solution first (`dotnet build CobolSharp.sln`). Appends "<label> rc=<rc> wall=<s>" to walls.txt in -Out
# (default: the system temp directory). The shards of walls.txt were launched CONCURRENTLY, one process each, e.g.
#   '…&(FullyQualifiedName~_P0.|FullyQualifiedName~_P1.|FullyQualifiedName~_P2.)'  (P0..P2; the dot keeps _P1 off _P10)
param([Parameter(Mandatory)][string]$Label, [Parameter(Mandatory)][string]$Filter, [string[]]$EnvPairs = @(),
      [string]$Out = (Join-Path ([IO.Path]::GetTempPath()) 'pb1708-b6'))
[System.Diagnostics.Process]::GetCurrentProcess().PriorityClass = 'BelowNormal'
Set-Location (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..\..'))
New-Item -ItemType Directory -Force $Out | Out-Null
foreach ($p in $EnvPairs) { $k, $v = $p.Split('=', 2); Set-Item "env:$k" $v }
$env:COBOLNET_COMPILE_CACHE = 'off'
$sw = [Diagnostics.Stopwatch]::StartNew()
& dotnet test tests/Cobol.Net.Tests.Conformance --no-build --filter $Filter --logger "trx;LogFileName=$Label.trx" --results-directory $Out *> (Join-Path $Out "$Label.log")
$rc = $LASTEXITCODE
"$Label rc=$rc wall=$([math]::Round($sw.Elapsed.TotalSeconds, 1)) s env=$($EnvPairs -join ',')" | Tee-Object -Append (Join-Path $Out 'walls.txt')
