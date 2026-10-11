#!/usr/bin/env pwsh
# Render docs/DIAGNOSTICS.md (a gitignored VIEW, kb/Work PB3026; scripts/rendered_views.py) from the first-class diagnostic catalogue
# (src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs), rearch PHASE 02 P2.10.
#
# The catalogue is C#, so the renderer lives in the drift test (DiagnosticRegistryDriftTests) — this script
# runs that test with COBOLNET_WRITE_DIAGNOSTICS_DOC=1 so it WRITES docs/DIAGNOSTICS.md instead of asserting.
# The same test, run normally, proves the catalogue renders one row per descriptor; nothing is compared with a copy.
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
    $filter = 'FullyQualifiedName~DiagnosticRegistryDriftTests.DiagnosticsDoc_RendersTheWholeCatalogue'
    python scripts/filter_population.py --filter $filter `
        --filtered tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj --allow-build
    if ($LASTEXITCODE -ne 0) {
        throw "the regeneration filter selects nothing (filter_population rc $LASTEXITCODE) — NOTHING WAS REGENERATED; see the finding above (kb/Work PB751)"
    }
    $env:COBOLNET_WRITE_DIAGNOSTICS_DOC = '1'
    # SCRUBBED (kb/Work PB1718): a stray leg handshake or VSTest* property would narrow even this one-test run.
    python scripts/test_population.py scrubbed dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj -c Debug --filter $filter
    if ($LASTEXITCODE -ne 0) { throw "regeneration test failed (exit $LASTEXITCODE)" }
    Write-Host 'Rendered docs/DIAGNOSTICS.md from DiagnosticCatalog.'
}
finally {
    Remove-Item Env:\COBOLNET_WRITE_DIAGNOSTICS_DOC -ErrorAction SilentlyContinue
    Pop-Location
}
