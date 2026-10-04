<#
.SYNOPSIS
  Invoke ANTLR4 on the COBOL grammars, generating C# directly into Generated/ — portably (Windows AND Linux).
.DESCRIPTION
  - Expects:
      - antlr-4.13.2-complete.jar in ANTLR4/
      - CobolLexer.g4 in Grammar/Core/
      - CobolParserCore.g4 in Grammar/ (imports + tokenVocab resolve from Grammar/Core/)
      - java on PATH (the only external prerequisite besides pwsh itself)
  - Generates DIRECTLY into Generated/ (a BUILD OUTPUT — .gitignored, never checked in). No staging copy: a
    failed step fails the build (exit code propagated by GenerateIfNewer.ps1), and the next build's staleness
    check regenerates a partial folder, so atomicity buys nothing. ANTLR emits only its own lexer/parser/visitor
    files — the hand-maintained CobolParserCoreBase.cs (referenced via the superClass option) is never emitted,
    so nothing here can overwrite it.
  - The parser's -lib inputs (imported Core/*.g4 + CobolLexer.tokens) are STAGED in obj/antlr-lib/ — already
    ignored and cleaned by the SDK, and never globbed into Compile.

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
#>

function Invoke-Antlr4CSharp {
    [CmdletBinding()]
    param(
        [string]$JarPath = (Join-Path $PSScriptRoot 'ANTLR4' 'antlr-4.13.2-complete.jar'),
        [string]$OutputDir = (Join-Path $PSScriptRoot 'Generated'),
        [string]$PackageName = 'CobolNet.Frontend.Generated'
    )

    $grammarDir = Join-Path $PSScriptRoot 'Grammar'
    $coreDir = Join-Path $grammarDir 'Core'
    $libDir = Join-Path $PSScriptRoot 'obj' 'antlr-lib'

    # Validate prerequisites — fail LOUD with an actionable message (this script's exit code fails the build).
    if (-not (Get-Command java -ErrorAction SilentlyContinue)) {
        Write-Error "java not found on PATH - ANTLR generation requires a JRE (the jar is vendored in ANTLR4/)."
        return 1
    }
    if (-not (Test-Path $JarPath)) {
        Write-Error "ANTLR JAR not found at: $JarPath"
        return 1
    }
    $parserGPath = Join-Path $grammarDir 'CobolParserCore.g4'
    if (-not (Test-Path $parserGPath)) {
        Write-Error "Grammar file not found at: $parserGPath"
        return 1
    }
    $lexerGPath = Join-Path $coreDir 'CobolLexer.g4'
    if (-not (Test-Path $lexerGPath)) {
        Write-Error "Lexer grammar not found at: $lexerGPath"
        return 1
    }

    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    }
    if (Test-Path $libDir) {
        Remove-Item -Path $libDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $libDir -Force | Out-Null

    # Run one ANTLR generation from $workDir with a BARE grammar filename (flat output on every platform).
    function Invoke-AntlrStep([string]$workDir, [string]$grammarName, [string[]]$extraArgs) {
        Push-Location $workDir
        try {
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
                    Write-Error $text
                } elseif ($text) {
                    Write-Host $text
                }
            }
            if ($exitCode -ne 0 -or $hadDiag) {
                Write-Error "Generation failed for $grammarName (exit code $exitCode)."
                return $false
            }
            Write-Host "$grammarName generation succeeded." -ForegroundColor Green
            return $true
        }
        finally {
            Pop-Location
        }
    }

    # --- Step 1: lexer, from Grammar/Core (bare name => flat output: Generated/CobolLexer.*) ---
    if (-not (Invoke-AntlrStep $coreDir 'CobolLexer.g4' @())) { return 1 }

    # --- Step 2: stage the parser's -lib inputs: imported sub-grammars + the lexer tokens ---
    Copy-Item -Path (Join-Path $coreDir '*.g4') -Destination $libDir -Force
    $tokensFile = Join-Path $OutputDir 'CobolLexer.tokens'
    if (-not (Test-Path $tokensFile)) {
        Write-Error "CobolLexer.tokens not produced at: $tokensFile (flat-output assumption violated)."
        return 1
    }
    Copy-Item -Path $tokensFile -Destination $libDir -Force

    # --- Step 3: parser, from Grammar (bare name), imports + tokenVocab from the staged lib dir ---
    if (-not (Invoke-AntlrStep $grammarDir 'CobolParserCore.g4' @('-lib', $libDir))) { return 1 }

    Write-Host "All ANTLR generation completed successfully." -ForegroundColor Green
    return 0
}

# If script is called directly, invoke the function and PROPAGATE the exit code (a silent 0 here was half the
# DEVLOG-554 CI break: generation failed, the build continued on a stale checked-in parser).
if ($MyInvocation.MyCommand.Path -eq $PSCommandPath) {
    $result = Invoke-Antlr4CSharp
    exit $result
}
