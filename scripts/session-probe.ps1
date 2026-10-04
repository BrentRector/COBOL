# session-probe.ps1 — the §0 SESSION BOOTSTRAP step-③ mechanizer (plan §0; added 2026-07-19, DEVLOG 922).
# Prints the live repo state a session must confirm before touching code. Mechanical, because manual state
# ledgers drift (the COBOLNET1573 collision class). Read-only; safe to run any time.
$ErrorActionPreference = 'Continue'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "=== SESSION PROBE ($(Get-Date -Format 'yyyy-MM-dd HH:mm')) ==="

# 1. Branch + HEAD + cleanliness
$branch = git rev-parse --abbrev-ref HEAD
$head = git rev-parse --short HEAD
$dirty = git status --porcelain
Write-Host "branch : $branch @ $head $(if ($branch -ne 'phase-14' -and $branch -ne 'main') { '⚠ UNEXPECTED BRANCH' })"
if ($dirty) { Write-Host "⚠ DIRTY TREE ($(@($dirty).Count) paths) — commit or explain before proceeding" } else { Write-Host "tree   : clean" }
$unpushed = git log --oneline '@{u}..HEAD' 2>$null
if ($unpushed) { Write-Host "⚠ UNPUSHED commits: $(@($unpushed).Count)" } else { Write-Host "push   : up to date" }

# 2. Diagnostic band — the next-free rule (the CEILING of the src scan, the catalog scan and every code already
#    reserved for an in-flight worktree) lives ONCE, in scripts/orchestrator/alloc.py (`code_scans`, which carries
#    the 1573/1518 and 1999/2096 collision history). The probe prints its line and never recomputes it.
$diag = & python scripts/orchestrator/alloc.py peek code --probe 2>&1
Write-Host "diag   : $diag"

# 3. VCR todo count (the P14 burn-down instrument)
$todo = (Select-String -Path 'docs/VERSION_CHANGE_REFERENCE.md' -Pattern '<!-- todo -->' -AllMatches | ForEach-Object { $_.Matches }).Count
Write-Host "VCR    : $todo todo anchors remaining (P14 Step 1 drives to zero)"

# 4. Corpus + negative counts (drift signal, not a gate)
$pos = (Get-ChildItem tests/conformance -Recurse -Filter *.out | Measure-Object).Count
$neg = (Get-ChildItem tests/conformance/negative -Filter *.err | Measure-Object).Count
Write-Host "corpus : $pos positive goldens · $neg negative fixtures"

# 5. Traceability inventory (exists after P14 Step 0)
if (Test-Path 'tests/version-matrix/traceability-inventory.json') {
    $inv = Get-Content 'tests/version-matrix/traceability-inventory.json' -Raw | ConvertFrom-Json
    $gaps = @($inv | Where-Object { $_.state -eq 'GAP' }).Count
    Write-Host "invent : $(@($inv).Count) rows · $gaps GAP (v1.0 = zero GAP)"
    # Once Phase B starts, the GAP count alone hides the SHAPE of what is left — a DIVERGES is a queued fix, a
    # NEEDS-OWNER-DECISION is blocked on the owner, and a CONFORMS still lacking a test is a golden away from
    # closing. Print the breakdown only when there is one, so this stays a single line before Phase B.
    $adjudicated = @($inv | Where-Object { $_.verdict })
    if ($adjudicated.Count -gt 0) {
        $byVerdict = $adjudicated | Group-Object verdict | Sort-Object Count -Descending |
            ForEach-Object { "$($_.Name) $($_.Count)" }
        Write-Host "       $($adjudicated.Count) adjudicated · $($byVerdict -join ' · ')"
        # ⛔ "test-needed" is a row that DID NOT CLOSE, not a row with an empty test-ref. The two differ: a row
        # can cite a NIST golden or a *_MatchesLegacy differential — which resolves on disk and is worth
        # recording — and still be open, because only a SPEC-DERIVED test closes a row. Keying on the empty
        # string under-reported this by 7 of 11 on the first Phase-B batch.
        $needTest = @($adjudicated | Where-Object { $_.verdict -eq 'CONFORMS' -and $_.state -eq 'GAP' }).Count
        if ($needTest -gt 0) { Write-Host "       $needTest CONFORMS still test-needed (each is one spec-derived golden from OK)" }
    }
    # ⛔ IS THE VAULT'S BURN-DOWN VIEW STILL TELLING THE TRUTH? kb/Conformance/ is a GENERATED summary of the very
    # file read above, and because nothing re-ran its generator it silently drifted: five of twelve clause notes
    # were wrong and §15's claimed 533 GAP where the live number was 481 — it UNDER-reported real progress and was
    # still read as current. build_inventory.py now regenerates it, so this only fires when the inventory was
    # edited another way. Absent is NOT stale: it is a gitignored build output and legitimately missing on a fresh
    # clone, which the checker reports as OK rather than as a red.
    # ⛔ THE WORK LIST, PRINTED, FROM THE ONE REGISTER. Deriving "what should I fix next" from a 2,400-line
    # prose queue is what led to picking PB39 (rule-id cosmetics, zero wrong answers) over PB24 (FUNCTION LENGTH
    # silently wrong) — both are [MAJOR], so severity could not separate them. kb/Work/ replaced FIVE overlapping
    # registers; this ranks on what a defect DOES to a user's program.
    if (Test-Path 'kb/Work') {
        & python scripts/spec/work.py next 2>&1 | ForEach-Object { Write-Host $_ }
        & python scripts/spec/work.py check *>$null
        if ($LASTEXITCODE -ne 0) {
            Write-Host "work   : the kb/Work register has malformed items - run: python scripts/spec/work.py check"
        }
    }
    if (Test-Path 'kb/Conformance') {
        $viewCheck = & python scripts/spec/gen_conformance_notes.py --check 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host "vault  : ⛔ kb/Conformance burn-down is STALE — run: python scripts/spec/gen_conformance_notes.py"
        }
    }
} else {
    Write-Host "invent : not built yet (P14 Step 0)"
}

# ⛔ A DISABLED GATE MUST NOT BE INVISIBLE. Reported here because the probe is the one thing every session reads,
# and "CI is off" is exactly the kind of live state that rots into a false sense of coverage
# (feedback_green_gates_arent_evidence) — the battery is NOT a superset of CI: it is Windows/DEBUG only, so the
# Linux legs and the Release configuration are verified by CI and by nothing else.
# ⚙ EARNED 2026-08-06, when the workflow was turned off during GitHub's Actions outage and five pushes queued
# nothing. It prints ONLY while some workflow is non-active, so it clears itself the moment CI comes back — a
# check that keeps announcing a resolved condition is one people learn to ignore. Re-enabling is an OWNER
# decision; the re-entry sequence (dispatch, confirm all five jobs get RUNNERS, then land any workflow change) is
# in plan §0.
# Network-tolerant: any failure to reach GitHub is silent, so the probe still works offline.
try {
    $wf = & gh api repos/BrentRector/COBOL/actions/workflows --jq '.workflows[] | select(.state != "active") | "\(.name) [\(.state)]"' 2>$null
    if ($LASTEXITCODE -eq 0 -and $wf) {
        Write-Host "ci     : ⛔ DISABLED — $wf  (re-enable: gh workflow enable 'Build and Test')"
        Write-Host "         every gate is LOCAL until then: bash scripts/battery.sh"
    }
} catch { }

# ⛔ AND IS THE ENABLED WORKFLOW ACTUALLY PASSING? The check above answers "is CI running at all" and says nothing
# about the VERDICT, so a red main is invisible to a session that reads only this probe: main was red for 29 hours
# across 16 consecutive completed runs (2026-09-05 → 2026-09-06) while every landing in that window reported green,
# because no step of the landing protocol looked at CI after the push and the battery cannot see the Linux legs
# (kb/Work/PB796). Printed unconditionally — a green line is the positive evidence a silence never is
# (feedback_green_gates_arent_evidence). Degrades to one quiet line with no gh, no network and no runs.
if (Get-Command gh -ErrorAction SilentlyContinue) {
    try {
        $lastRun = & gh run list --branch main --limit 1 --json conclusion,headSha,status,displayTitle 2>$null | ConvertFrom-Json
        if ($lastRun) {
            $runSha = $lastRun.headSha.Substring(0, 8)
            if ($lastRun.status -ne 'completed') {
                Write-Host "ci     : last run on main @ $runSha is $($lastRun.status) — no verdict yet"
            } elseif ($lastRun.conclusion -eq 'success') {
                Write-Host "ci     : last run on main @ $runSha = success"
            } else {
                Write-Host "ci     : ⛔ last run on main @ $runSha = $($lastRun.conclusion) — attribute it before landing: gh run view --branch main --log-failed"
            }
        } else {
            Write-Host "ci     : no runs found on main"
        }
    } catch { Write-Host "ci     : gh unavailable — CI verdict UNREAD (do not infer green)" }
} else {
    Write-Host "ci     : gh unavailable — CI verdict UNREAD (do not infer green)"
}

# ⛔ AND IS THE REQUIRED CHECK STILL IN PLACE? `main` requires the `ci-gate` status check with
# `enforce_admins: true` (owner decision 2026-09-06, question 23; kb/Work/PB796), and that rule lives on the
# SERVER, outside this repository — so it is the ONE part of the landing gate no test in this tree can pin, and
# nothing but this line would notice it being turned off. A session reading a green `ci` line over a protection
# that has quietly gone is the PB796 shape exactly: an instrument answering a different question than the one
# being asked. Printed unconditionally, green included, and a 404 is told apart from an unreadable API — "I could
# not look" must never render as "it is off", nor as silence (feedback_green_gates_arent_evidence).
if (Get-Command gh -ErrorAction SilentlyContinue) {
    $prot = & gh api 'repos/{owner}/{repo}/branches/main/protection' 2>&1
    if ($LASTEXITCODE -ne 0) {
        if ("$prot" -match 'Branch not protected|HTTP 404') {
            Write-Host "gate   : ⛔ main has NO branch protection — the required ci-gate check is GONE (kb/Work/PB796)"
        } else {
            Write-Host "gate   : gh could not read main's protection — required-check state UNREAD (do not infer it is on)"
        }
    } else {
        try {
            $j = "$prot" | ConvertFrom-Json
            $ctx = @($j.required_status_checks.contexts)
            $adm = [bool]$j.enforce_admins.enabled
            if (($ctx -contains 'ci-gate') -and $adm) {
                Write-Host "gate   : main requires ci-gate (enforce_admins on) — land with: bash scripts/push-main.sh"
            } else {
                Write-Host "gate   : ⛔ main protection DRIFTED — contexts=[$($ctx -join ',')] enforce_admins=$adm (expected ci-gate + True)"
            }
        } catch {
            Write-Host "gate   : main's protection did not parse — required-check state UNREAD (do not infer it is on)"
        }
    }
} else {
    Write-Host "gate   : gh unavailable — main's required-check state UNREAD (do not infer it is on)"
}
# The probe is informational: a 404 from the protection lookup must not become the SCRIPT's exit code, which is
# the last native command's when nothing overrides it (this block is the last one before the closing banner).
$global:LASTEXITCODE = 0

Write-Host "=== next: confirm the battery green (plan §9) before code changes; the worklist is kb/Work (work.py next, above); plan 0 is live state ==="
