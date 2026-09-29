# build-local.ps1 — the PER-COMMIT wave-local gate, as one command (kb/Work PB108; plan §0 "Gates"; the pwsh twin of
# build-local.sh): build the SOLUTION (Debug — the same binaries battery.sh PHASE 0 builds; a stale test-bin compiler
# DLL hides regressions, so no --no-build leg ever runs on an unbuilt tree), then Conformance filtered on the SUBJECT
# of the change, full Unit (~2 min), Characterization. NOT the comprehensive gate (battery.sh), NOT guard-fast.sh,
# NOT Release (the Windows CI leg). ⛔ The filter is REQUIRED: the wave-local gate is a filter chosen from what the
# change TOUCHES ("~Arithmetic|~Inspect"; add "~VersionMatrix" for an edition gate) — a default would re-create the
# PB36 mistake of filtering on where the new goldens sat. Shorthand terms ("~X|~Y") are expanded to
# FullyQualifiedName~X|FullyQualifiedName~Y — vstest silently matches NOTHING for a bare "~X" (and exits 0), so a leg
# with NO verdict line is RED here, never green by absence. That check is WHOLE-filter, so EVERY TERM is
# additionally put back to vstest before the legs run (scripts/filter_population.py, kb/Work PB708): a term
# naming no test anywhere is DEAD and the gate does not run; one whose tests live only in an assembly this
# gate runs UNFILTERED is INERT — it selected nothing, so it is named and the verdict line says so.
# Usage:  pwsh scripts/build-local.ps1 -Filter "~Collation|~Locale"
#         pwsh scripts/build-local.ps1 -Filter "~X" -Priority BelowNormal     (every IMPLEMENTER gate)
# ⛔ -Priority (owner decision 2026-09-22): the LANDER's gate is the serial bottleneck of the whole campaign and it
# shares one 32-core host with up to seventeen implementer gates — its whole-Conformance leg measured 9.6 min quiet,
# 18.5 min at battery #84 and 30.6 min in train 48. Windows passes BelowNormal/Idle DOWN to child processes (and
# only those two classes), so an implementer that sets it here runs its build, dotnet test, every testhost and every
# compiled COBOL program below the lander's Normal-priority gate. The lander and the battery leave it at Normal.
param([Parameter(Mandatory = $true)][string]$Filter,
      [ValidateSet('Normal', 'BelowNormal', 'Idle')][string]$Priority = 'Normal')
$ErrorActionPreference = 'Continue'
if ($Priority -ne 'Normal') {
    [System.Diagnostics.Process]::GetCurrentProcess().PriorityClass = [System.Diagnostics.ProcessPriorityClass]::$Priority
    Write-Host "build-local: running at $Priority priority (inherited by build, test hosts and compiled programs)"
}
Set-Location (Split-Path -Parent $PSScriptRoot)
$Filter = [regex]::Replace($Filter, '(^|[|&(])(!=|=|~)', '$1FullyQualifiedName$2')
$rc = 0
# ⛔ THE CITATION AUDITS RUN FIRST, BEFORE THE BUILD — see the note in build-local.sh: a wrong § is the one
# defect class no test can catch, the checks cost a second, and every baseline is zero. THREE of them: clause vs
# the CONSTRUCT the comment names, a QUOTED fragment vs the clause it is filed under (kb/Work PB379), and —
# over the other half of the same problem — a FROZEN evidence file's citation of the TREE, which no one may
# repair by editing the record, so it must be marked `superseded_by` instead (kb/Work PB785). The same four also
# gate CI's `audits` job on every push, docs-only included (kb/Work PB1574): this gate warns early, CI refuses.
python scripts/spec/audit_code_citations.py --check
if ($LASTEXITCODE -ne 0) { Write-Host '=== CITATIONS: RED (see above) ==='; $rc = 1 }
python scripts/spec/audit_doc_citations.py --check
if ($LASTEXITCODE -ne 0) { Write-Host '=== DOC CITATIONS: RED (see above) ==='; $rc = 1 }
python scripts/spec/audit_evidence_supersession.py --check
if ($LASTEXITCODE -ne 0) { Write-Host '=== EVIDENCE SUPERSESSION: RED (see above) ==='; $rc = 1 }
# The drift-rule index (docs/DRIFT_RULES.md) is GENERATED from every *DriftTests summary — stale = red.
python scripts/spec/drift_rules.py --check
if ($LASTEXITCODE -ne 0) { Write-Host '=== DRIFT RULES INDEX: RED (run python scripts/spec/drift_rules.py) ==='; $rc = 1 }
# The PreToolUse guard hook (git stash / direct push to main / heredoc escapes / bare test filters) proves itself.
python scripts/hooks/test_forbidden_commands.py
if ($LASTEXITCODE -ne 0) { Write-Host '=== GUARD HOOK SELF-TEST: RED ==='; $rc = 1 }
python scripts/hooks/test_readonly_repo.py
if ($LASTEXITCODE -ne 0) { Write-Host '=== READ-ONLY HOOK SELF-TEST: RED ==='; $rc = 1 }
# ⛔ The inventory's WITNESS COUNT (kb/Work PB959) — the axis the resolution drift test deliberately does not
# measure: RED on any code-location/test-ref lost since the merge-base with main without a retirement mark.
python scripts/spec/audit_witness_loss.py --check
if ($LASTEXITCODE -ne 0) { Write-Host '=== WITNESS LOSS: RED (see above) ==='; $rc = 1 }
# The GPL GnuCOBOL corpus is git-ignored and PER WORKTREE (scripts/fetch-gnucobol-tests.ps1): a fresh worktree has
# none, and ExternalCorpusPopulationDriftTests in the UNFILTERED unit leg is RED BY DESIGN when it is absent
# (kb/Work PB209 — a missing population is not an empty one). Fetch it here so the gate measures the population in
# every worktree, not only where someone remembered to; three landing trains re-attributed that pair by hand before
# train 23 made it automatic.
# ⛔ A FAILED FETCH REFUSES THE GATE (kb/Work PB897). It used to print a line saying the two population reds were
# environmental and carry on — which, while the fetch was broken on every GNU-tar host, trained every agent to
# ignore a permanently red gate. The reds are ATTRIBUTED here, by cause and exit code, and the verdict line says
# so; a gate that could not measure its population must not read as green.
$corpusNote = ''
if (-not (Test-Path 'tests/external/gnucobol/tests/testsuite.src')) {
    Write-Host '=== EXTERNAL CORPUS: absent in this worktree — fetching (GPL, git-ignored, never committed) ==='
    pwsh -NoProfile -File scripts/fetch-gnucobol-tests.ps1
    $fetchRc = $LASTEXITCODE
    if ($fetchRc -ne 0 -or -not (Test-Path 'tests/external/gnucobol/tests/testsuite.src')) {
        Write-Host "=== EXTERNAL CORPUS: FETCH FAILED (exit $fetchRc; the FETCH FAILED line above names the cause) — the ExternalCorpusPopulationDriftTests reds in the unit leg are ATTRIBUTABLE TO IT, not to the change under test, and this gate is RED because it could not measure that population ==="
        $corpusNote = ' — EXTERNAL CORPUS FETCH FAILED, POPULATION UNMEASURED'
        $rc = 1
    }
}
dotnet build CobolSharp.sln -v quiet
if ($LASTEXITCODE -ne 0) { Write-Host '=== WAVE-LOCAL GATE: BUILD FAILED ==='; exit 1 }
# ⛔ EVERY TERM OF THE FILTER MUST NAME A REAL TEST (kb/Work PB708) — the NO-VERDICT-LINE check on each
# leg is WHOLE-filter: it fires only when EVERY term is dead, so one dead term OR'd among live ones selects
# the others, prints a verdict line and is never named. PB691's gate carried
# `FullyQualifiedName~SpecTraceabilityInventory` against Conformance — where that test does not live, it is
# in Unit — and reported `Passed! … 1640` on every run. vstest answers for its own filter language, one
# discovery probe per term (~1.6 s each); `filter_population.py --self-test` proves every arm fires.
python scripts/filter_population.py --filter "$Filter" --filtered tests/Cobol.Net.Tests.Conformance --unfiltered tests/Cobol.Net.Tests.Unit --unfiltered tests/Cobol.Net.Tests.Characterization
$pop = $LASTEXITCODE
# ⛔ ANY code but 0 (all live) or 3 (inert, named) REFUSES the gate — a missing python or a crashed probe
# must not become a silent skip of the guard (feedback_green_gates_arent_evidence).
if ($pop -ne 0 -and $pop -ne 3) { Write-Host "=== WAVE-LOCAL GATE: NOT RUN — the filter does not name what it claims, or the check itself could not run (rc=$pop, filter $Filter) ==="; exit 2 }
$inert = if ($pop -eq 3) { ' — WITH INERT FILTER TERM(S), SEE ABOVE' } else { '' }
# ⛔ WHAT A LEG PRINTS IS DECIDED IN ONE PLACE — scripts/test_leg_report.py (kb/Work PB1573). A GREEN leg prints its
# verdict line; a RED one prints its COMPLETE output, untrimmed. This function used to keep only the lines matching
# `error|[FAIL]|Passed!|Failed!`, the last 20 of them — which kept a failing test's name and its `Error Message:`
# label and DROPPED the message and the stack, so every red arrived unattributable. The full log is always kept
# under TestResults/build-local/ (git-ignored). The same reporter serves build-local.sh and guard-fast.sh.
$legLogs = Join-Path (Get-Location) 'TestResults/build-local'
New-Item -ItemType Directory -Force $legLogs | Out-Null
function Leg([string]$name, [string[]]$testArgs) {
    $log = Join-Path $legLogs "$name.log"
    # SCRUBBED (kb/Work PB1718): no leg handshake or VSTest*/RunSettingsFilePath property reaches the test host.
    & python scripts/test_population.py scrubbed dotnet test @testArgs *> $log
    $legRc = $LASTEXITCODE
    python scripts/test_leg_report.py --name $name --log $log --rc $legRc
    if ($LASTEXITCODE -ne 0) { $script:rc = 1 }
}
Leg 'conformance'      @('tests/Cobol.Net.Tests.Conformance', '--no-build', '--filter', $Filter)
Leg 'unit'             @('tests/Cobol.Net.Tests.Unit', '--no-build')
Leg 'characterization' @('tests/Cobol.Net.Tests.Characterization', '--no-build')
if ($rc -eq 0) { Write-Host "=== WAVE-LOCAL GATE: GREEN (filter $Filter)$inert ===" } else { Write-Host "=== WAVE-LOCAL GATE: RED (filter $Filter)$inert$corpusNote ===" }
exit $rc
