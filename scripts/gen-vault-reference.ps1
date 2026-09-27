#!/usr/bin/env pwsh
# ─────────────────────────────────────────────────────────────────────────────────────────────────────────
# gen-vault-reference.ps1 — generate Obsidian reference notes for the WiseOwl COBOL type surface.
#
# Parses the hand-written C# under the greenfield source trees and emits one markdown note per type into
# kb/Reference/<Section>/ (a GITIGNORED build output — regenerated, never hand-edited), carrying each type's
# `///` <summary>, base type (as a wiki-link), kind, and source location. Also emits per-section indexes and a
# top index with a documentation-debt list (types lacking a `///` summary).
#
# The code-reference layer of the derived knowledge base (docs/DOC_INDEX.md "Derived knowledge base").
# Convention: like the other scripts/gen-*.ps1 generators it is normally run by hand; a "runs clean" drift test
# (VaultReferenceGeneratorDriftTests) guards it in CI, and an OPT-IN MSBuild target (-p:GenerateVaultReference=true)
# runs it on build. It is NOT a mandatory build step — the build does not depend on these notes (unlike ANTLR).
#
#   pwsh scripts/gen-vault-reference.ps1            # write kb/Reference/
#   pwsh scripts/gen-vault-reference.ps1 -Check     # verify-only: parse into a temp dir, report, exit code
# ─────────────────────────────────────────────────────────────────────────────────────────────────────────
[CmdletBinding()]
param(
    [switch]$Check   # verify mode: generate into a temp dir (don't touch kb/), still exits non-zero on error
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent

# (Section name, source dir relative to repo, recurse?)
# Bound (the IR node defs) is its own section; Compiler covers the REST of Cobol.Net.Compiler (binder / codegen /
# model / passes / validation / OO), excluding Binding/Bound so the two don't overlap.
$sections = @(
    [pscustomobject]@{ Name = 'Bound';    Dir = 'src/Cobol.Net.Compiler/Binding/Bound'; Recurse = $false; Exclude = $null },
    [pscustomobject]@{ Name = 'Compiler';  Dir = 'src/Cobol.Net.Compiler';              Recurse = $true;  Exclude = '[\\/]Binding[\\/]Bound[\\/]' },
    [pscustomobject]@{ Name = 'SourceGen'; Dir = 'src/Cobol.Net.Compiler.SourceGen';     Recurse = $true;  Exclude = $null },
    [pscustomobject]@{ Name = 'Editions';  Dir = 'src/Cobol.Net.Editions';               Recurse = $true;  Exclude = $null },
    [pscustomobject]@{ Name = 'Runtime';  Dir = 'src/Cobol.Net.Runtime';                Recurse = $true;  Exclude = $null },
    [pscustomobject]@{ Name = 'Frontend'; Dir = 'src/Cobol.Net.Frontend';               Recurse = $true;  Exclude = $null }
)

# ⛔ A -Check run writes to its OWN directory — the twin of gen-grammar-diagrams.ps1's fix (wave 59 H): a FIXED
# %TEMP% path is shared by every concurrent gate on the host (VaultReferenceGeneratorDriftTests runs this from each
# worktree's Unit leg), and one run's opening Remove-Item deletes the directory under another's WriteAllText.
$outRoot = if ($Check) { Join-Path ([System.IO.Path]::GetTempPath()) ("cobolnet-vault-ref-check-" + [guid]::NewGuid().ToString('N')) }
           else        { Join-Path $repo 'kb/Reference' }

# Modifiers are OPTIONAL (a top-level type may be modifier-less = internal; nested types are real too). The
# PascalCase name + start-of-line anchor keep this from matching `where T : class` or a `record`-named local.
$declRx     = [regex]'^\s*(?:(?:public|internal|private|protected|abstract|sealed|partial|static|file|readonly|ref|unsafe|new)\s+)*(record\s+struct|record\s+class|record|class|struct|interface|enum)\s+([A-Z][A-Za-z0-9_]*)'
$baseCtorRx = [regex]'\)\s*:\s*([A-Za-z_][A-Za-z0-9_.]*)'
$basePlainRx= [regex]'(?:record\s+struct|record\s+class|record|class|struct|interface|enum)\s+[A-Za-z0-9_]+(?:<[^>]*>)?\s*:\s*([A-Za-z_][A-Za-z0-9_.]*)'
$summaryRx  = [regex]'(?s)<summary>(.*?)</summary>'

function Get-Summary([string]$doc) {
    $m = $summaryRx.Match($doc)
    if (-not $m.Success) { return '' }
    $s = $m.Groups[1].Value
    $s = [regex]::Replace($s, '<see\s+cref="[^"]*?([A-Za-z0-9_]+)"\s*/>', '`$1`')
    $s = [regex]::Replace($s, '<see\s+cref="[^"]*"\s*>(.*?)</see>', '`$1`')
    $s = [regex]::Replace($s, '<paramref\s+name="([^"]*)"\s*/>', '`$1`')
    $s = [regex]::Replace($s, '<[^>]+>', '')
    $s = [regex]::Replace($s, '\s+', ' ').Trim()
    return [System.Net.WebUtility]::HtmlDecode($s)
}

# The frontmatter `description:` is the PROGRESSIVE-DISCLOSURE hook: it lets a reader (human or agent) judge a
# note's relevance from the index alone, without opening it. Without it, deciding whether `MoveClassifier` matters
# to the task at hand costs a full file read — 634 times over. First sentence, one line, YAML-safe.
function Get-Description([string]$summary) {
    if (-not $summary) { return 'No ``///`` summary in the source — documentation debt.' }
    # First sentence, but do not break on the period inside an ISO citation (§14.9.24.4) or a decimal.
    $m = [regex]::Match($summary, '^(.*?[.!?])(?:\s|$)')
    $d = if ($m.Success -and $m.Groups[1].Value.Length -ge 25) { $m.Groups[1].Value } else { $summary }
    if ($d.Length -gt 200) { $d = $d.Substring(0, 197).TrimEnd() + '...' }
    # YAML double-quoted scalar: escape backslash and quote; strip newlines (already collapsed by Get-Summary).
    return $d.Replace('\', '\\').Replace('"', '\"')
}

# ── pass 1: collect every type across all sections ──────────────────────────────────────────────────────
$types = [ordered]@{}
foreach ($sec in $sections) {
    $dir = Join-Path $repo $sec.Dir
    if (-not (Test-Path $dir)) { continue }
    $gci = @{ Path = $dir; Filter = '*.cs'; File = $true }
    if ($sec.Recurse) { $gci.Recurse = $true }
    $excl = $sec.Exclude
    $files = Get-ChildItem @gci |
        Where-Object { $_.FullName -notmatch '[\\/](obj|bin|Generated)[\\/]' -and $_.Name -notlike '*.g.cs' `
                       -and (-not $excl -or $_.FullName -notmatch $excl) } |
        Sort-Object FullName
    foreach ($f in $files) {
        $rel   = ($f.FullName.Substring($repo.Length + 1)) -replace '\\', '/'
        $lines = @(Get-Content -LiteralPath $f.FullName)
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $m = $declRx.Match($lines[$i])
            if (-not $m.Success) { continue }
            $name = $m.Groups[2].Value
            if ($types.Contains($name)) { continue }
            # The base clause lives in the type HEADER only (declaration .. first '{' or ';'). Never scan into the
            # body — a plain class's method code contains '`) : when`', '? :', etc. that false-match a base.
            $hdr = ''
            for ($k = $i; $k -lt [Math]::Min($i + 15, $lines.Count); $k++) {
                $b = $lines[$k].IndexOf('{'); $s = $lines[$k].IndexOf(';')
                $cut = if ($b -ge 0 -and ($s -lt 0 -or $b -lt $s)) { $b } elseif ($s -ge 0) { $s } else { -1 }
                if ($cut -ge 0) { $hdr += ' ' + $lines[$k].Substring(0, $cut); break }
                $hdr += ' ' + $lines[$k]
            }
            $bm = $baseCtorRx.Match($hdr); if (-not $bm.Success) { $bm = $basePlainRx.Match($hdr) }
            $base = if ($bm.Success) { $bm.Groups[1].Value } else { $null }
            $j = $i - 1
            while ($j -ge 0 -and $lines[$j].TrimStart().StartsWith('[')) { $j-- }
            $doc = New-Object System.Collections.Generic.List[string]
            while ($j -ge 0 -and $lines[$j].TrimStart().StartsWith('///')) {
                $doc.Insert(0, $lines[$j].TrimStart().Substring(3).Trim()); $j--
            }
            $types[$name] = [pscustomobject]@{
                Name = $name; Kind = ($m.Groups[1].Value -replace '\s+', ' '); Base = $base
                Summary = (Get-Summary(($doc -join "`n"))); Src = $rel; Line = ($i + 1); Section = $sec.Name
            }
        }
    }
}

# ── pass 2: emit notes ──────────────────────────────────────────────────────────────────────────────────
if (Test-Path $outRoot) { Remove-Item -Recurse -Force $outRoot }
New-Item -ItemType Directory -Path $outRoot | Out-Null

$note = @'
---
title: {0}
description: "{8}"
kind: {1}
base: {2}
source: {3}
sourceLine: {4}
generated: true
tags:
  - cobolsharp
  - reference
  - generated
  - {5}
---

# `{0}`

> ⚙ **Generated** from `{3}` (line {4}) — do not edit; regenerate with `scripts/gen-vault-reference.ps1`.

**Kind:** {1} · **Base:** {6}

{7}

## See also
- [[kb/Reference/{5}/_Index|{5} reference index]] · [[kb/Reference/_Index]]
'@

$debt = New-Object System.Collections.Generic.List[object]
foreach ($sec in $sections) {
    New-Item -ItemType Directory -Path (Join-Path $outRoot $sec.Name) -Force | Out-Null
}
foreach ($t in $types.Values) {
    if ($t.Base -and $types.Contains($t.Base)) {
        $bsec = $types[$t.Base].Section
        $baselink = "[[kb/Reference/$bsec/$($t.Base)|$($t.Base)]]"
    } elseif ($t.Base) { $baselink = '`' + $t.Base + '`' } else { $baselink = '—' }
    $summary = if ($t.Summary) { $t.Summary } else { '> ⚠ No `///` summary in the source — documentation debt.' }
    if (-not $t.Summary) { $debt.Add($t) }
    $content = $note -f $t.Name, $t.Kind, ($t.Base ?? '—'), $t.Src, $t.Line, $t.Section, $baselink, $summary, (Get-Description $t.Summary)
    [System.IO.File]::WriteAllText((Join-Path $outRoot (Join-Path $t.Section ($t.Name + '.md'))), $content)
}

# ── per-section indexes + top index ─────────────────────────────────────────────────────────────────────
foreach ($sec in $sections) {
    $items = $types.Values | Where-Object { $_.Section -eq $sec.Name } | Sort-Object Name
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.Append("---`ntitle: $($sec.Name) Reference (generated)`ngenerated: true`ntags:`n  - cobolsharp`n  - reference`n  - generated`n  - moc`n---`n`n")
    [void]$sb.Append("# $($sec.Name) Reference (generated)`n`n")
    [void]$sb.Append("> ⚙ Generated from ``$($sec.Dir)`` by ``scripts/gen-vault-reference.ps1`` — **$($items.Count) types**. Build output (gitignored); do not edit.`n`n")
    [void]$sb.Append("## Types ($($items.Count))`n")
    foreach ($t in $items) {
        $s = if ($t.Summary) { ' — ' + $t.Summary } else { '' }
        [void]$sb.Append("- [[kb/Reference/$($sec.Name)/$($t.Name)|$($t.Name)]]$s`n")
    }
    [System.IO.File]::WriteAllText((Join-Path $outRoot (Join-Path $sec.Name '_Index.md')), $sb.ToString())
}

$top = New-Object System.Text.StringBuilder
[void]$top.Append("---`ntitle: Code Reference (generated)`ngenerated: true`ntags:`n  - cobolsharp`n  - reference`n  - generated`n  - moc`n---`n`n")
[void]$top.Append("# Code Reference (generated)`n`n")
[void]$top.Append("> ⚙ Generated by ``scripts/gen-vault-reference.ps1`` from the source ``///`` summaries — **$($types.Count) types** across the greenfield trees. Drift-proof build output (gitignored). Companion to the hand-curated lookup tables under [[kb/Spec/Lookup/Index]].`n`n")
[void]$top.Append("## Sections`n")
foreach ($sec in $sections) {
    $c = ($types.Values | Where-Object { $_.Section -eq $sec.Name }).Count
    [void]$top.Append("- [[kb/Reference/$($sec.Name)/_Index|$($sec.Name)]] — $c types (``$($sec.Dir)``)`n")
}
[void]$top.Append("`n## Documentation debt ($($debt.Count) types without a ``///`` summary)`n")
if ($debt.Count -gt 0) {
    foreach ($t in ($debt | Sort-Object Section, Name)) { [void]$top.Append("- ``$($t.Name)`` — ``$($t.Src):$($t.Line)`` [$($t.Section)]`n") }
} else { [void]$top.Append("- none — every type has a ``///`` summary. 🎉`n") }
[System.IO.File]::WriteAllText((Join-Path $outRoot '_Index.md'), $top.ToString())

# ── report ──────────────────────────────────────────────────────────────────────────────────────────────
foreach ($sec in $sections) {
    $c = ($types.Values | Where-Object { $_.Section -eq $sec.Name }).Count
    Write-Host ("  {0,-9} {1,4} types" -f $sec.Name, $c)
}
Write-Host "generated $($types.Count) type notes into $outRoot (documentation debt: $($debt.Count))"
if ($Check) { Remove-Item -Recurse -Force $outRoot -ErrorAction SilentlyContinue }
if ($types.Count -lt 100) { Write-Error "only $($types.Count) types parsed — the C# parse likely broke"; exit 1 }
exit 0
