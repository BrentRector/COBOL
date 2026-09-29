# PB1708 pivot evidence b3 — drive the ordering probe and print what xunit actually did.
#     pwsh -File run.ps1 > b3.txt      (from this directory; every input is here)
$ErrorActionPreference = 'Continue'
Set-Location $PSScriptRoot
$work = Join-Path ([IO.Path]::GetTempPath()) "pb1708-b3-$PID"
New-Item -ItemType Directory -Force $work | Out-Null
dotnet build -v quiet | Out-Null
$plan = Join-Path $work 'plan.txt'
Set-Content $plan "C17.T2`nC05.T3`nC22.T0"

function Settings([bool]$stop) {
    $f = Join-Path $work "s-$stop.runsettings"
    Set-Content $f "<RunSettings><xUnit><MaxParallelThreads>4</MaxParallelThreads><StopOnFail>$($stop.ToString().ToLower())</StopOnFail></xUnit></RunSettings>"
    $f
}

function Scenario([string]$name, [bool]$usePlan, [bool]$fail, [bool]$stop) {
    $log = Join-Path $work "$name.log"
    $env:ORDER_PROBE_LOG = $log
    $env:ORDER_PROBE_PLAN = if ($usePlan) { $plan } else { $null }
    $env:ORDER_PROBE_FAIL = if ($fail) { '1' } else { $null }
    dotnet test --no-build --settings (Settings $stop) --logger "trx;LogFileName=$name.trx" --results-directory $work *> (Join-Path $work "$name.out")
    [xml]$trx = Get-Content (Join-Path $work "$name.trx")
    $results = @($trx.TestRun.Results.UnitTestResult | Where-Object { $_ })
    $first = (Get-Content $log | Select-Object -First 8 | ForEach-Object { ($_ -split ' ')[2] }) -join ' '
    $out = Get-Content (Join-Path $work "$name.out")
    $verdict = ($out | Select-String '^(Passed|Failed)!').Line
    $aborted = ($out | Select-String 'Test Run Aborted|Test host process crashed' | Select-Object -First 1).Line
    "{0,-26} results in trx: {1,3} (failed {2})  first 8 started: {3}" -f $name, $results.Count, @($results | Where-Object outcome -eq 'Failed').Count, $first
    "{0,-26} verdict line: {1}" -f '', ($verdict ?? '(none)')
    if ($aborted) { "{0,-26} ABORTED: {1}" -f '', $aborted.Substring(0, [Math]::Min(160, $aborted.Length)) }
}

$listed = @(dotnet test --no-build --list-tests | Select-String '^\s+OrderProbe\.').Count
"discovered by --list-tests: $listed"
Scenario 'default-order' $false $false $false
Scenario 'planned-order' $true $false $false
Scenario 'planned-red-no-stop' $true $true $false
Scenario 'planned-red-stop-on-fail' $true $true $true
Remove-Item -Recurse -Force $work
