#!/usr/bin/env pwsh
# Regenerate the "Gating status index" block of docs/VERSION_CHANGE_REFERENCE.md (rearch PHASE 03, P3.6).
#
# The block is derived from the VCR's own <!-- gate:construct-id --> anchors + tests/version-matrix/constructs.json
# (a construct's `status` — active->done / pending->pending — is itself fixture-gated by VersionMatrixTests). The
# renderer lives in VcrDriftTests (the catalogue is read there), so this script runs that test with
# COBOLNET_WRITE_VCR=1 so it WRITES the block instead of asserting. Run normally in CI, the same test fails if the
# committed block drifts, if a gate anchor names a non-existent construct, or if a spec citation does not resolve
# (unknown clause, a quoted fragment no longer inside it, or a resurrected spec LINE number).
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    # ⛔ THE FILTER MUST NAME A REAL TEST BEFORE ITS EXIT CODE MEANS ANYTHING (kb/Work PB751). The whole
    # regeneration is one env var plus this ONE test, so the filter is the whole selection — and vstest answers a
    # filter that matches nothing with a PASSING run of zero tests. Rename the test (or partition its class, as
    # A13 did to NistDifferentialTests) and the selection goes dead, `dotnet test` exits 0, the throw below never
    # fires, and this script prints 'Regenerated …' having regenerated nothing. `--allow-build` because the run
    # below is what builds the project: nothing is built yet at this point on a clean tree.
    $filter = 'FullyQualifiedName~VcrDriftTests.GeneratedStatusIndex_IsInSync'
    python scripts/filter_population.py --filter $filter `
        --filtered tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj --allow-build
    if ($LASTEXITCODE -ne 0) {
        throw "the regeneration filter selects nothing (filter_population rc $LASTEXITCODE) — NOTHING WAS REGENERATED; see the finding above (kb/Work PB751)"
    }
    $env:COBOLNET_WRITE_VCR = '1'
    # SCRUBBED (kb/Work PB1718): a stray leg handshake or VSTest* property would narrow even this one-test run.
    python scripts/test_population.py scrubbed dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj -c Debug --filter $filter
    if ($LASTEXITCODE -ne 0) { throw "regeneration test failed (exit $LASTEXITCODE)" }
    Write-Host 'Regenerated the VERSION_CHANGE_REFERENCE.md gating status index.'
}
finally {
    Remove-Item Env:\COBOLNET_WRITE_VCR -ErrorAction SilentlyContinue
    Pop-Location
}
