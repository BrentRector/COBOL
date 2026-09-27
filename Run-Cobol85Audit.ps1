param(
    [string]$RepoRoot = "e:\COBOL",
    [string]$OutDir   = "e:\COBOL\audit\cobol85"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host "== COBOL-85 Audit Runner =="

# Ensure output directory exists
New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

Push-Location $RepoRoot
try {
    Write-Host "1) Restoring and building solution..."
    dotnet restore | Tee-Object -FilePath (Join-Path $OutDir "dotnet-restore.log")
    dotnet build -c Release | Tee-Object -FilePath (Join-Path $OutDir "dotnet-build.log")

    Write-Host "2) Running unit tests..."
    dotnet test tests\CobolSharp.Tests.Unit\CobolSharp.Tests.Unit.csproj `
        -c Release `
        --logger "trx;LogFileName=unit.trx" `
        --results-directory $OutDir `
        | Tee-Object -FilePath (Join-Path $OutDir "unit-tests.log")

    Write-Host "3) Running integration tests..."
    dotnet test tests\CobolSharp.Tests.Integration\CobolSharp.Tests.Integration.csproj `
        -c Release `
        --logger "trx;LogFileName=integration.trx" `
        --results-directory $OutDir `
        | Tee-Object -FilePath (Join-Path $OutDir "integration-tests.log")

    Write-Host "4) Running NIST tests (if project exists)..."
    $nistProj = Join-Path $RepoRoot "tests\CobolSharp.Tests.Nist\CobolSharp.Tests.Nist.csproj"
    if (Test-Path $nistProj) {
        dotnet test $nistProj `
            -c Release `
            --logger "trx;LogFileName=nist.trx" `
            --results-directory $OutDir `
            | Tee-Object -FilePath (Join-Path $OutDir "nist-tests.log")
    } else {
        Write-Host "   NIST test project not found at $nistProj"
    }

    Write-Host "5) Snapshot key source directories..."
    $dirs = @(
        "src\CobolSharp.Compiler\Grammar",
        "src\CobolSharp.Compiler\Semantics\Bound\Binding",
        "src\CobolSharp.Compiler\Semantics\Symbol",
        "src\CobolSharp.Runtime",
        "tests\CobolSharp.Tests.Unit",
        "tests\CobolSharp.Tests.Integration",
        "tests\CobolSharp.Tests.Nist"
    )

    $snapshotFile = Join-Path $OutDir "source-snapshot.txt"
    Remove-Item $snapshotFile -ErrorAction SilentlyContinue
    foreach ($d in $dirs) {
        $full = Join-Path $RepoRoot $d
        if (Test-Path $full) {
            "=== $d ===" | Out-File -FilePath $snapshotFile -Append
            Get-ChildItem $full -Recurse | Select-Object FullName | Out-File -FilePath $snapshotFile -Append
            "" | Out-File -FilePath $snapshotFile -Append
        }
    }

    Write-Host "Audit artifacts written to: $OutDir"
}
finally {
    Pop-Location
}
