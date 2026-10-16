<#
.SYNOPSIS
  Invoke ANTLR4 on the COBOL grammars, generating C# directly into the output directory — portably (Windows AND
  Linux).
.DESCRIPTION
  THE ONE GENERATION ENTRY POINT (kb/Work PB2719, PB2730). The csproj's EnsureGeneratedFiles target runs this script
  with `pwsh -File`, and it is the ONLY place that decides "up to date": its Inputs name every generation input (every
  Grammar/**/*.g4, this script, the vendored jar, and a stamp carrying the namespace and the jar path), so a changed
  input regenerates on a warm tree exactly as a fresh checkout does. This script therefore always generates; it keeps
  no staleness check of its own, and it has no defaults for what the csproj single-sources: -PackageName is the
  csproj's <AntlrNamespace>, -JarPath its <AntlrJar>, -OutputDir its Generated/ folder.

  - Expects:
      - CobolLexer.g4 in Grammar/Core/
      - CobolParserCore.g4 in Grammar/ (imports + tokenVocab resolve from Grammar/Core/)
      - java on PATH (the only external prerequisite besides pwsh itself)
  - The output directory is a BUILD OUTPUT (.gitignored, never checked in) and a pure function of the inputs: it is
    emptied before generation (an output a renamed grammar no longer produces cannot linger and be compiled) and
    emptied again when generation fails, so a failed run — including one whose files ANTLR wrote before it reported
    a warning — leaves no output newer than the inputs for the next build to mistake for up to date. ANTLR emits only
    its own lexer/parser/visitor files; the hand-maintained CobolParserCoreBase.cs (referenced via the superClass
    option) lives in Parsing/, so nothing here can overwrite it.
  - The parser's -lib inputs (imported Core/*.g4 + CobolLexer.tokens) are STAGED in obj/antlr-lib/ — already
    ignored and cleaned by the SDK, and never globbed into Compile.
  - Every failure exits nonzero, which fails the build (no stale-parser fallback: a silent 0 was half the DEVLOG-554
    CI break, where generation failed and the build continued on a stale checked-in parser).

  ENCODING (kb/Work PB1944): every grammar is read as UTF-8 (`-encoding UTF-8`), never in the JVM's default charset.
  ANTLR reads a .g4 with the JVM default, which is UTF-8 on Java 18+ locally and on Linux but Cp1252 on Windows CI's
  JVM, so a non-ASCII character in a grammar's code was one character locally and several (a `warning(180)`, an
  error under warnings-as-errors) on CI. GrammarEncodingDriftTests pins this flag and that every .g4 is UTF-8
  without a BOM; with the encoding declared, a grammar may hold non-ASCII text (a character that does not display
  is still written as a \uXXXX escape, DESIGN-frontend-grammar.md).

  PORTABILITY (the DEVLOG-554 CI break): ANTLR mirrors a grammar's RELATIVE DIRECTORY under -o, but it detects
  "has a directory" with the PLATFORM separator — `Core/CobolLexer.g4` is a bare name on Windows (flat output)
  yet a nested path on Linux (output lands in <out>/Core/), which silently broke the tokens hand-off. So each
  grammar is generated FROM ITS OWN DIRECTORY with a BARE filename — flat output everywhere.

  AntlrGenerationDriftTests pins this contract: mandatory parameters with no defaults, no self-invocation guard,
  and the csproj target as the one up-to-date decision over every input.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PackageName,
    [Parameter(Mandatory)][string]$JarPath,
    [Parameter(Mandatory)][string]$OutputDir
)

# Any cmdlet failure (a copy, a delete, a directory create) stops the script and reaches the trap below, which fails
# the build; nothing here continues past an error.
$ErrorActionPreference = 'Stop'

$grammarDir = Join-Path $PSScriptRoot 'Grammar'
$coreDir = Join-Path $grammarDir 'Core'
$libDir = Join-Path $PSScriptRoot 'obj' 'antlr-lib'

# Empty the output directory of every file (it holds only ANTLR's outputs).
function Clear-GeneratedOutput {
    if (Test-Path -LiteralPath $OutputDir) {
        Get-ChildItem -LiteralPath $OutputDir -File | Remove-Item -Force
    }
}

# Fail LOUD: name the cause on stderr, leave no output behind, and exit nonzero (MSBuild's Exec fails the build).
function Stop-Generation([string]$message) {
    [Console]::Error.WriteLine("ANTLR generation FAILED: $message")
    try { Clear-GeneratedOutput }
    catch { [Console]::Error.WriteLine("ANTLR generation could not empty ${OutputDir}: $_") }
    exit 1
}

trap { Stop-Generation "$_" }

# Validate prerequisites with an actionable message.
if (-not (Get-Command java -ErrorAction SilentlyContinue)) {
    Stop-Generation "java not found on PATH - ANTLR generation requires a JRE (the jar is vendored in ANTLR4/)."
}
if (-not (Test-Path -LiteralPath $JarPath)) {
    Stop-Generation "ANTLR JAR not found at: $JarPath"
}
$parserGPath = Join-Path $grammarDir 'CobolParserCore.g4'
if (-not (Test-Path -LiteralPath $parserGPath)) {
    Stop-Generation "Grammar file not found at: $parserGPath"
}
$lexerGPath = Join-Path $coreDir 'CobolLexer.g4'
if (-not (Test-Path -LiteralPath $lexerGPath)) {
    Stop-Generation "Lexer grammar not found at: $lexerGPath"
}

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
Clear-GeneratedOutput
if (Test-Path -LiteralPath $libDir) {
    Remove-Item -LiteralPath $libDir -Recurse -Force
}
New-Item -ItemType Directory -Path $libDir -Force | Out-Null

# Run one ANTLR generation from $workDir with a BARE grammar filename (flat output on every platform). Any warning
# or error ANTLR reports fails the generation, as does a nonzero exit.
function Invoke-AntlrStep([string]$workDir, [string]$grammarName, [string[]]$extraArgs) {
    Push-Location $workDir
    try {
        # ANTLR reports on stderr; its lines are judged below, never by the script-wide 'Stop' (which some pwsh
        # versions apply to a native command's redirected stderr).
        $ErrorActionPreference = 'Continue'
        Write-Host "Generating C# from: $grammarName" -ForegroundColor Cyan
        $antlrOutput = & java -jar $JarPath `
            -encoding UTF-8 `
            -Dlanguage=CSharp `
            -no-listener -visitor `
            -package $PackageName `
            -o $OutputDir `
            @extraArgs `
            $grammarName 2>&1
        $exitCode = $LASTEXITCODE
        $hadDiag = $false
        foreach ($line in $antlrOutput) {
            $text = $line.ToString().Trim()
            if ($text -match '^(warning|error)\(') {
                $hadDiag = $true
                [Console]::Error.WriteLine($text)
            } elseif ($text) {
                Write-Host $text
            }
        }
        if ($exitCode -ne 0 -or $hadDiag) {
            Stop-Generation "$grammarName (java exit code $exitCode; ANTLR's diagnostics are above)."
        }
        Write-Host "$grammarName generation succeeded." -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
}

# --- Step 1: lexer, from Grammar/Core (bare name => flat output: <OutputDir>/CobolLexer.*) ---
Invoke-AntlrStep $coreDir 'CobolLexer.g4' @()

# --- Step 2: stage the parser's -lib inputs: imported sub-grammars + the lexer tokens ---
Copy-Item -Path (Join-Path $coreDir '*.g4') -Destination $libDir -Force
$tokensFile = Join-Path $OutputDir 'CobolLexer.tokens'
if (-not (Test-Path -LiteralPath $tokensFile)) {
    Stop-Generation "CobolLexer.tokens not produced at: $tokensFile (flat-output assumption violated)."
}
Copy-Item -LiteralPath $tokensFile -Destination $libDir -Force

# --- Step 3: parser, from Grammar (bare name), imports + tokenVocab from the staged lib dir ---
Invoke-AntlrStep $grammarDir 'CobolParserCore.g4' @('-lib', $libDir)

Write-Host "All ANTLR generation completed successfully." -ForegroundColor Green
exit 0
