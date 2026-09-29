# PB1708 pivot evidence b3 — drive the ordering probe and print what xunit and vstest actually did.
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

function Names([string]$trxPath) {
    if (-not (Test-Path $trxPath)) { return @() }
    [xml]$trx = Get-Content -Encoding utf8 $trxPath
    @($trx.TestRun.Results.UnitTestResult | Where-Object { $_ })
}

function Scenario([string]$name, [bool]$usePlan, [bool]$fail, [bool]$stop, [string]$leg = $null, [string]$legPlan = $null, [bool]$throw = $false, [string]$bad = $null) {
    $log = Join-Path $work "$name.log"
    Remove-Item $log -ErrorAction SilentlyContinue
    $env:ORDER_PROBE_LOG = $log
    $env:ORDER_PROBE_PLAN = if ($usePlan) { $plan } else { $null }
    $env:ORDER_PROBE_FAIL = if ($fail) { '1' } else { $null }
    $env:ORDER_PROBE_LEG = if ($leg) { $leg } else { $null }
    $env:ORDER_PROBE_LEGPLAN = if ($legPlan) { $legPlan } else { $null }
    $env:ORDER_PROBE_THROW = if ($throw) { '1' } else { $null }
    $env:ORDER_PROBE_BAD = if ($bad) { $bad } else { $null }
    dotnet test --no-build --settings (Settings $stop) --logger "trx;LogFileName=$name.trx" --results-directory $work *> (Join-Path $work "$name.out")
    $exit = $LASTEXITCODE
    $results = Names (Join-Path $work "$name.trx")
    $outcomes = ($results | Group-Object outcome | Sort-Object Name | ForEach-Object { "$($_.Name) $($_.Count)" }) -join ', '
    $first = if (Test-Path $log) { (Get-Content $log | Select-Object -First 8 | ForEach-Object { ($_ -split ' ')[2] }) -join ' ' } else { '(none)' }
    $out = Get-Content (Join-Path $work "$name.out")
    $verdict = ($out | Select-String '^(Passed|Failed)!').Line
    $noTests = ($out | Select-String 'No test is available|No test matches' | Select-Object -First 1).Line
    $aborted = ($out | Select-String 'Test Run Aborted|Test host process crashed' | Select-Object -First 1).Line
    $errLine = ($out | Select-String 'GATE ENVIRONMENT INCOMPLETE|falling back' | Select-Object -First 1).Line
    "{0,-26} exit {1}; results in trx: {2,3} ({3})  first 8 started: {4}" -f $name, $exit, $results.Count, $outcomes, $first
    "{0,-26} verdict line: {1}" -f '', ($verdict ?? '(none)')
    if ($noTests) { "{0,-26} console: {1}" -f '', $noTests.Trim() }
    if ($aborted) { "{0,-26} ABORTED: {1}" -f '', $aborted.Substring(0, [Math]::Min(160, $aborted.Length)) }
    if ($errLine) { "{0,-26} console: {1}" -f '', $errLine.Trim().Substring(0, [Math]::Min(160, $errLine.Trim().Length)) }
}

$env:ORDER_PROBE_LEG = $null; $env:ORDER_PROBE_THROW = $null
$listed = @(dotnet test --no-build --list-tests | Select-String '^\s+OrderProbe\.' | ForEach-Object { $_.Line.Trim() })
"discovered by --list-tests: $($listed.Count) ($(@($listed | Sort-Object -Unique).Count) distinct names)"
Scenario 'default-order' $false $false $false
Scenario 'planned-order' $true $false $false
Scenario 'planned-red-no-stop' $true $true $false
Scenario 'planned-red-stop-on-fail' $true $true $true

# ── the gate legs: leg 1 = classes C00-C09 plus the duplicate-named theory; leg 2 = everything else ──
$legPlan = Join-Path $work 'legplan.txt'
$listed | Where-Object { $_ -match '^OrderProbe\.(C0\d\.|Dup\.)' } | Sort-Object -Unique | Set-Content -Encoding utf8 $legPlan
Scenario 'leg-1' $true $false $false '1' $legPlan
Scenario 'leg-2' $true $false $false '2' $legPlan
$union = @((Names (Join-Path $work 'leg-1.trx')) + (Names (Join-Path $work 'leg-2.trx')) | ForEach-Object { $_.testName })
$ran = @((Names (Join-Path $work 'leg-1.trx')) + (Names (Join-Path $work 'leg-2.trx')) | Where-Object { $_.outcome -in 'Passed', 'Failed' })
$diff = Compare-Object ($listed | Sort-Object) ($union | Sort-Object)
"union of the two legs' trx names vs --list-tests, as a MULTISET: $(if ($diff) { 'DIFFERENT' } else { 'EQUAL' }) ($($union.Count) vs $($listed.Count)); outcome Passed/Failed (ran): $($ran.Count)"
$diff | ForEach-Object { "    $($_.SideIndicator) $($_.InputObject)" }
function Definitions([string]$trxPath) {
    [xml]$trx = Get-Content -Encoding utf8 $trxPath
    @($trx.TestRun.TestDefinitions.UnitTest | Where-Object { $_ } | ForEach-Object { $_.name })
}
$defs = @((Definitions (Join-Path $work 'leg-1.trx')) + (Definitions (Join-Path $work 'leg-2.trx')))
$ddiff = Compare-Object ($listed | Sort-Object) ($defs | Sort-Object)
"union of the two legs' trx TEST DEFINITIONS vs --list-tests, as a MULTISET: $(if ($ddiff) { 'DIFFERENT' } else { 'EQUAL' }) ($($defs.Count) vs $($listed.Count)); leg 1 holds $(@(Definitions (Join-Path $work 'leg-1.trx')).Count) definitions for its $(@(Names (Join-Path $work 'leg-1.trx')).Count) results"
"duplicate-named theory in leg 1's trx: $(@((Names (Join-Path $work 'leg-1.trx')) | Where-Object { $_.testName -like 'OrderProbe.Dup.Long*' }).Count) results"

# ── an EMPTY leg: every discovered name is in leg 1, so leg 2 runs nothing ──
$allPlan = Join-Path $work 'allplan.txt'
$listed | Sort-Object -Unique | Set-Content -Encoding utf8 $allPlan
Scenario 'empty-leg-2' $true $false $false '2' $allPlan

# ── the framework THROWS at construction (a partial or mismatched gate environment) ──
Scenario 'throwing-framework' $false $false $false $null $null $true
# ── the executor refuses instead: throws, or reports every case as an ExecutionErrorTestCase ──
Scenario 'throwing-executor' $false $false $false $null $null $false 'throw'
Scenario 'error-cases' $false $false $false $null $null $false 'errorcases'
Remove-Item -Recurse -Force $work
