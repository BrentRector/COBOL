# Copyright (c) 2026 Brent Rector. All rights reserved.
# Licensed under the Business Source License 1.1. See LICENSE file in the project root.
#
# gen-reserved-words.ps1 — generate the per-edition reserved-word tables (VERSION_TEST_MATRIX_DESIGN
# "Phase-2 implementation plan" P2.4; derivation REVISED 2026-07-03, DEVLOG 585: the planned hand-authored
# 1985 delta list was replaced by published per-standard source lists after the API content filter blocked
# every attempt to move a word list through the conversation stream — 4th occurrence).
#
# Inputs:
#   specs/ISO_COBOL.md                 (in-repo)  — ISO/IEC 1989:2023 §8.9 = THE authoritative 2023 list.
#   docs/VERSION_CHANGE_REFERENCE.md   (in-repo)  — row 32 = the 16 words newly reserved in 2023 (Annex E.2 item 25).
#   GnuCOBOL config/{cobol85,cobol2002,cobol2014}.words (fetched to .cache/, NOT committed) — the per-standard
#     reserved-word lists curated by the GnuCOBOL project (FSF; the FILES are GPL — only the derived FACTS,
#     word/edition reservation flags with provenance, enter this repo).
#
# Outputs (BOTH committed; ReservedWordsDriftTests asserts they agree):
#   src/Cobol.Net.Editions/ReservedWords.Table.cs
#   tests/version-matrix/reserved-words.json
#
# Derivation: flags = set membership per source (85/2002/2014 = GnuCOBOL; 2023 = the ISO spec itself).
# Confidence:
#   high   — sources agree with the expected edge structure (incl. the re-reserved pair, which falls out of
#            membership mechanically: in 85, out 2002/2014, in 2023 = Annex E.2 item 25).
#   medium — disagreement buckets (a 2023 §8.9 word unknown to the 2014 source yet NOT an Annex-E addition,
#            or any other non-monotone surprise). Medium rows are present but INERT: only high rejects
#            (the conservative policy — a wrong entry must never reject a valid program).
# ISO-2023 §8.9 WINS every 2023 dispute (the spec is the authority; GnuCOBOL is a curation net).
#
# ⚠ CONTENT-FILTER RULE (DEVLOG 578/584/585 — tripped 4×): NOTHING here prints a word list — counts only.
#   Never cat/grep these outputs into a conversation stream.
#
# The generated table is the per-unit DEFAULT: the 2023 COBOL-WORDS directive (Annex E.3.3 item 12) mutates
# the effective set per compilation unit via the ReservedWordSet layer (roadmap ISO-validation D9).

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'LfText.psm1') -Force   # every tracked output is written LF (kb/Work PB3029)
$repo = Split-Path -Parent $PSScriptRoot
$specPath = Join-Path $repo 'specs/ISO_COBOL.md'
$vcrPath  = Join-Path $repo 'docs/VERSION_CHANGE_REFERENCE.md'
$csOut    = Join-Path $repo 'src/Cobol.Net.Editions/ReservedWords.Table.cs'
$jsonOut  = Join-Path $repo 'tests/version-matrix/reserved-words.json'
# The §8.10 CONTEXT-SENSITIVE word table (kb/Work PB250). >>COBOL-WORDS SR3 admits a context-sensitive
# word as literal-1/3/4 and SR4 bars one as literal-2/5/6, so the compiler needs the §8.10 population as
# well as §8.9's - it had only the words that happen to be lexer tokens, and rejected legal directives
# naming HEX, CURRENT, LC_ALL, ANUM, BYTE, ACTIVATING, STACK, TOP-LEVEL, UCS-4, UTF-8, UTF-16, ...  The
# directive is a 2023 introduction (cobol-words-directive-2023), so §8.10's 2023 table is exactly the
# population its rules speak about and no per-edition flags are needed.
$ctxCsOut   = Join-Path $repo 'src/Cobol.Net.Editions/ContextSensitiveWords.Table.cs'
$ctxJsonOut = Join-Path $repo 'tests/version-matrix/context-sensitive-words.json'
$cache    = Join-Path $repo '.cache/gnucobol-words'

# ---- 0. Fetch the GnuCOBOL per-standard lists (cached; pinned tag first, master fallback) ----
New-Item -ItemType Directory -Force $cache | Out-Null
$gcFiles = @{}
foreach ($std in 'cobol85', 'cobol2002', 'cobol2014') {
    $f = Join-Path $cache "$std.words"
    if (-not (Test-Path $f) -or (Get-Item $f).Length -lt 1000) {
        $ok = $false
        foreach ($ref in 'gnucobol-3.2', 'master') {
            try {
                Invoke-WebRequest -Uri "https://raw.githubusercontent.com/OCamlPro/gnucobol/$ref/config/$std.words" -OutFile $f -UseBasicParsing -ErrorAction Stop
                if ((Get-Item $f).Length -gt 1000) { $ok = $true; break }
            } catch { }
        }
        if (-not $ok) { throw "could not fetch GnuCOBOL $std.words (network?); cache at $cache" }
    }
    $gcFiles[$std] = $f
}

# ---- 1. Parse a GnuCOBOL .words file: 'reserved:'/'register:' entries; '*' = context-sensitive (skip);
#         'WORD=ALIAS' reserves both spellings. Returns an uppercase set. ----
function Parse-GcWords([string]$path) {
    $set = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($line in Get-Content -LiteralPath $path) {
        if ($line -notmatch '^(reserved|register):\s*(.+)$') { continue }
        $tok = ($Matches[2] -split '#', 2)[0].Trim()          # strip trailing comment
        if ($tok.Length -eq 0 -or $tok.EndsWith('*')) { continue }  # context-sensitive => not reserved
        foreach ($side in $tok -split '=') {
            $w = $side.Trim().ToUpperInvariant()
            if ($w -match '^[A-Z][A-Z0-9-]*$') { [void]$set.Add($w) }
        }
    }
    return ,$set
}
$gc85   = Parse-GcWords $gcFiles['cobol85']
$gc2002 = Parse-GcWords $gcFiles['cobol2002']
$gc2014 = Parse-GcWords $gcFiles['cobol2014']

# CCVS-PROVEN over-inclusions (DEVLOG 585): a conforming NIST CCVS-85 program using a word as a user-defined
# word PROVES it was not X3.23-1985-reserved — the corpus outranks the GnuCOBOL curation (dialect-pragmatic
# extras). Evidence: ST127A + the sort differential programs declare ORDER as a data name at --std 85.
foreach ($w in @('ORDER')) { [void]$gc85.Remove($w) }

# ---- 2. Extract the 2023 §8.9 list from the ISO spec ----
$lines = Get-Content -LiteralPath $specPath
$start = -1; $end = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    # `#{2,}` not `##`: the transcription's heading depths were normalised (repairs/heading_depth.py) and
    # §8.9 is now `### `, so the old two-hash anchor matched NOTHING and this script threw on every run.
    if ($start -lt 0 -and $lines[$i] -match '^#{2,}\s+8\.9\s+Reserved words') { $start = $i; continue }
    if ($start -ge 0 -and $lines[$i] -match '^#{2,}\s+8\.10\s') { $end = $i; break }
}
if ($start -lt 0 -or $end -lt 0) { throw "spec §8.9 section not located (start=$start end=$end)" }
# ONE normalization for a spec word line - the list bullet, the inline-code fence that keeps Markdown from
# eating '>' and '>>', and the &nbsp; padding of an OCR continuation fragment. Both the STEM line and the
# CONTINUATION line must go through it (kb/Work PB250).
function Normalize-SpecWordLine([string]$line) {
    return ($line -replace '^\s*-\s+', '' -replace '`', '' -replace '&nbsp;', '').Trim()
}
$iso2023 = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
for ($i = $start + 1; $i -lt $end; $i++) {
    # The list is a real Markdown list now (it rendered as run-on prose as bare lines), and the
    # special-character words are inline code so Markdown cannot eat them — `>` and `>>` used to VANISH into a
    # blockquote. Strip the bullet and the code fence before matching; the word regex below is unchanged.
    $t = Normalize-SpecWordLine $lines[$i]
    if ($t.Length -eq 0) { continue }
    $t = $t.Replace([char]0x2013, '-').Replace([char]0x2014, '-').ToUpperInvariant()
    switch ($t) {                                   # OCR remaps (DEVLOG 578 scout findings)
        'EMD-START'   { $t = 'END-START' }
        'I-OICONTROL' { $t = 'I-O-CONTROL' }
    }
    # OCR line-splits: a long word wraps as 'STEM-' + an '&NBSP;&NBSP;FRAGMENT' continuation line
    # (the FLOAT-NOT-A-NUMBER-QUIET/-SIGNALING pair, DEVLOG 585) — join stem + next non-empty line.
    # ⛔ THE CONTINUATION NEEDS THE SAME NORMALIZATION AS THE STEM (kb/Work PB250). The transcription
    # repair that made the list a real Markdown list also bulleted and code-fenced the CONTINUATION lines
    # ('- `&nbsp;&nbsp;QUIET`'), and this branch stripped only the entity - so the join produced
    # 'FLOAT-NOT-A-NUMBER-- `QUIET`', the word regex dropped it, and a re-run of this generator SILENTLY
    # un-reserved FLOAT-NOT-A-NUMBER-QUIET and -SIGNALING at 2023. Two normalizations, one shape: one place.
    if ($t.EndsWith('-')) {
        for ($j = $i + 1; $j -lt $end; $j++) {
            $cont = (Normalize-SpecWordLine $lines[$j]).ToUpperInvariant()
            if ($cont.Length -eq 0) { continue }
            $t = $t + $cont; $i = $j; break
        }
    }
    # ⛔ PAGE IS A §8.9 WORD (kb/Work PB655). This line used to drop PAGE with NOTE — an OCR-era guard against a
    # running page header, from before the list was a real Markdown list — and so un-reserved PAGE at 2023:
    # `- PAGE` sits between PACKED-DECIMAL and PAGE-COUNTER (`cite.py --check 8.9 "PAGE-COUNTER"`), and the
    # 2023 WRITE ADVANCING PAGE / RD PAGE LIMIT formats still spell it. Only the NOTE paragraph marker is excluded.
    if ($t -match '^[A-Z][A-Z0-9-]*[A-Z0-9]$' -and $t -ne 'NOTE') { [void]$iso2023.Add($t) }
}
[void]$iso2023.Add('METHOD')                        # OCR omission (DEVLOG 578)

# ---- 3. Parse VCR row 32: the 16 words newly reserved in 2023 (Annex E.2 item 25) ----
$vcrRow = (Get-Content -LiteralPath $vcrPath) | Where-Object { $_ -match '^\|\s*32\s*\|' } | Select-Object -First 1
if (-not $vcrRow) { throw 'VCR row 32 not found' }
if ($vcrRow -notmatch '\*\*New:\*\*\s*reserved:\s*([^.]+)\.') { throw 'VCR row 32 New:-reserved clause not parseable' }
$added2023 = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($m in [regex]::Matches($Matches[1], '[A-Z][A-Z0-9-]{2,}')) {
    if ($m.Value -ne 'B-SHIFT') { [void]$added2023.Add($m.Value) }   # exclude the family-glob remnant
}

# ---- 4. Sanity gates (fail loudly; never silently emit a wrong table) ----
if ($iso2023.Count -lt 400 -or $iso2023.Count -gt 430) { throw "2023 extraction $($iso2023.Count) outside 400..430" }
if ($gc85.Count -lt 320 -or $gc85.Count -gt 380)       { throw "gc85 $($gc85.Count) outside 320..380" }
# 2002/2014 exclude the starred context-sensitive entries (55/75 respectively — §8.10-class words are NOT
# reserved) and dedupe WORD=ALIAS spellings, so the real counts sit well below the raw line counts.
if ($gc2002.Count -lt 380 -or $gc2002.Count -gt 470)   { throw "gc2002 $($gc2002.Count) outside 380..470" }
if ($gc2014.Count -lt 370 -or $gc2014.Count -gt 470)   { throw "gc2014 $($gc2014.Count) outside 370..470" }
if ($added2023.Count -ne 16) { throw "VCR row 32 parse yielded $($added2023.Count) != 16 words" }
foreach ($w in $added2023) { if (-not $iso2023.Contains($w)) { throw "Annex-E addition '$w' missing from the 2023 extraction" } }
# ISO Annex E.2 item 25 is authoritative: ALL 16 added-2023 words were user-defined before 2023 — so their
# r2002/r2014 are FALSE even where the GnuCOBOL 2002/2014 lists disagree (they keep the communication trio
# reserved — a dialect-pragmatic curation; overridden below with provenance). Three of the 16 are 1985
# RE-reservations (the communication-module words incl. the END- scope terminator), discovered mechanically —
# the earlier recall-based classification knew only two (DEVLOG 585).
$reRes = @($added2023 | Where-Object { $gc85.Contains($_) })
if ($reRes.Count -ne 3) { throw "re-reserved set check: added2023 ∩ gc85 = $($reRes.Count), expected 3" }
$vcrVsGc = @($added2023 | Where-Object { $gc2014.Contains($_) -and -not $gc85.Contains($_) })
if ($vcrVsGc.Count -ne 0) { throw "cross-check failed: $($vcrVsGc.Count) non-85 Annex-E 2023 additions already in the 2014 source" }

# ---- 5. Classify every word in the union ----
$all = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($s in @($gc85, $gc2002, $gc2014, $iso2023)) { foreach ($w in $s) { [void]$all.Add($w) } }
$mediumBuckets = @{ isoOnly = 0; nonMonotone = 0 }
$rows = foreach ($w in $all) {
    $f85 = $gc85.Contains($w); $f02 = $gc2002.Contains($w); $f14 = $gc2014.Contains($w); $f23 = $iso2023.Contains($w)
    $conf = 'high'; $prov = $null
    if ($added2023.Contains($w)) {
        # ISO Annex E.2 item 25 overrides the 2002/2014 sources: user-defined before 2023, whatever GnuCOBOL says.
        $f02 = $false; $f14 = $false
        $prov = if ($f85) { '1985-reserved, unreserved 2002/2014, re-reserved 2023 (ISO Annex E.2 item 25 — overrides the GnuCOBOL 2002/2014 lists, which keep the communication trio)' }
                else      { 'added 2023 (ISO Annex E.2 item 25 = VCR row 32)' }
    }
    elseif ($f23 -and -not $f14 -and $f85) {
        # Reserved in 85 AND 2023, with Annex E recording NO 2023 (re-)addition. ISO E.3.1: "Such incompatible
        # additions are documented in E.2", and E.2 25) lists every word 2023 reserved that was user-defined
        # before (the $added2023 branch above, which already caught the three 1985 RE-reservations). A word
        # absent from that list was therefore NOT user-defined at 2014 — reservation did not flicker, so the
        # 2014-source absence (and, when the 2002 list is silent too, the 2002 one) is a curation gap:
        # interpolate both. kb/Work PB655: this is the REPORTS row, which sat 'medium' + "(inert)" while the
        # lexer token made it anything but inert — reserved-at-85/2023 but refused as a raw parse error at
        # 2002/2014 by a token no cobolWord admitted.
        $prov = if ($f02) { 'continuous since 1985; the 2014 flag INTERPOLATED (85+2002+2023 reserved, Annex E silent ⇒ the 2014 source list has a gap)' }
                else      { 'continuous since 1985; the 2002 AND 2014 flags INTERPOLATED (85+2023 reserved and ISO E.2 25) does not list it as a 2023 addition — E.3.1 documents every incompatible addition there ⇒ the GnuCOBOL 2002/2014 lists have a gap)' }
        $f02 = $true; $f14 = $true
    }
    elseif ($f23 -and -not $f14) {
        # In the ISO 2023 list, unknown to the 2014 source, NOT an Annex-E addition: source disagreement.
        $conf = 'medium'; $mediumBuckets.isoOnly++
        $prov = 'in ISO 2023 §8.9 but not the 2014 source list and not an Annex-E addition — needs review (inert)'
    }
    elseif ($f85 -and -not $f02 -and ($f14 -or $f23)) {
        # Gone in 2002 but back later without an Annex-E record: non-monotone surprise.
        $conf = 'medium'; $mediumBuckets.nonMonotone++
        $prov = 'non-monotone reservation across sources — needs review (inert)'
    }
    elseif ($f85 -and $f23) { $prov = 'continuous since 1985 (GnuCOBOL 85/2002/2014 lists; ISO 2023 §8.9)' }
    elseif ($f85 -and -not $f23) { $prov = '1985-reserved, removed post-85 (GnuCOBOL per-standard lists; absent from ISO 2023 §8.9)' }
    elseif ($f02) { $prov = 'added 2002 (GnuCOBOL 2002 list' + $(if ($f23) { '; ISO 2023 §8.9)' } else { '; dropped by 2023 §8.9)' }) }
    elseif ($f14) { $prov = 'added 2014 (GnuCOBOL 2014 list' + $(if ($f23) { '; ISO 2023 §8.9)' } else { '; dropped by 2023 §8.9)' }) }
    else { $prov = 'ISO 2023 §8.9' }
    [pscustomobject]@{ word = $w; r85 = $f85; r2002 = $f02; r2014 = $f14; r2023 = $f23; confidence = $conf; provenance = $prov }
}

# ---- 6. Emit the canonical JSON ----
$json = [ordered]@{
    _comment = 'GENERATED by scripts/gen-reserved-words.ps1 — do not hand-edit. Sources: ISO/IEC 1989:2023 §8.9 (specs/ISO_COBOL.md, authoritative for 2023), VCR row 32 (Annex E.2 item 25), GnuCOBOL per-standard word lists (85/2002/2014 flags; facts only). The drift test asserts this file equals the C# table. CONTENT-FILTER RULE: never print this file into a conversation.'
    words = $rows
}
Set-LfContent -LiteralPath $jsonOut -Value ($json | ConvertTo-Json -Depth 4)

# ---- 7. Emit the C# table ----
$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine('// <auto-generated>')
[void]$sb.AppendLine('// Generated by scripts/gen-reserved-words.ps1 — DO NOT EDIT; re-run the script.')
[void]$sb.AppendLine('// Sources: ISO/IEC 1989:2023 §8.9 (authoritative, 2023 flags), VCR row 32 (Annex E.2 item 25),')
[void]$sb.AppendLine('// GnuCOBOL per-standard word lists (85/2002/2014 flags; derived facts with provenance).')
[void]$sb.AppendLine('// ReservedWordsDriftTests asserts this table equals tests/version-matrix/reserved-words.json.')
[void]$sb.AppendLine('// </auto-generated>')
[void]$sb.AppendLine('namespace CobolNet.Editions;')   # must MATCH the committed table (kb/Work PB250: it said CobolNet.Validation, so a re-run broke the build)
[void]$sb.AppendLine('')
[void]$sb.AppendLine('public static partial class ReservedWords')
[void]$sb.AppendLine('{')
[void]$sb.AppendLine('    internal static readonly ReservedWordEntry[] Entries =')
[void]$sb.AppendLine('    [')
foreach ($r in $rows) {
    $b = @($r.r85, $r.r2002, $r.r2014, $r.r2023) | ForEach-Object { if ($_) { 'true' } else { 'false' } }
    $p = $r.provenance.Replace('"', '\"')
    [void]$sb.AppendLine("        new(""$($r.word)"", $($b[0]), $($b[1]), $($b[2]), $($b[3]), ""$($r.confidence)"", ""$p""),")
}
[void]$sb.AppendLine('    ];')
[void]$sb.AppendLine('}')
Set-LfContent -LiteralPath $csOut -Value $sb.ToString()

# ---- 7b. Extract the 2023 §8.10 context-sensitive word list and emit its table (kb/Work PB250) ----
$cstart = -1; $cend = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($cstart -lt 0 -and $lines[$i] -match '^#{2,}\s+8\.10\s+Context-sensitive words') { $cstart = $i; continue }
    if ($cstart -ge 0 -and $lines[$i] -match '^#{2,}\s+8\.11\s') { $cend = $i; break }
}
if ($cstart -lt 0 -or $cend -lt 0) { throw "spec §8.10 section not located (start=$cstart end=$cend)" }
# The section is a sequence of two-column Markdown tables (the printed page breaks split it); every data row
# is `| WORD | language construct or context |`. The repeated header rows are skipped by name.
$ctxRows = [ordered]@{}
for ($i = $cstart + 1; $i -lt $cend; $i++) {
    if ($lines[$i] -match '^\|\s*\*{0,2}([A-Za-z][A-Za-z0-9_-]*)\*{0,2}\s*\|\s*(.+?)\s*\|\s*$') {
        $w = $Matches[1].ToUpperInvariant()
        if ($w -eq 'CONTEXT-SENSITIVE') { continue }
        if (-not $ctxRows.Contains($w)) { $ctxRows[$w] = ($Matches[2] -replace '\*', '').Trim() }
    }
}
if ($ctxRows.Count -lt 50) { throw "§8.10 extraction returned only $($ctxRows.Count) words - the table shape changed" }
$ctxSorted = $ctxRows.Keys | Sort-Object
$ctxJson = [ordered]@{
    _comment = 'GENERATED by scripts/gen-reserved-words.ps1 - do not hand-edit. Source: ISO/IEC 1989:2023 §8.10 (specs/ISO_COBOL.md), the context-sensitive word table. Consumed by the >>COBOL-WORDS SR3/SR4 category validation (a 2023-only directive, so the 2023 table needs no per-edition flags). ContextSensitiveWordsDriftTests asserts this file, the C# table and the spec section all agree. CONTENT-FILTER RULE: never print this file into a conversation.'
    words = @($ctxSorted | ForEach-Object { [pscustomobject]@{ word = $_; context = $ctxRows[$_] } })
}
Set-LfContent -LiteralPath $ctxJsonOut -Value ($ctxJson | ConvertTo-Json -Depth 4)

$cb = [System.Text.StringBuilder]::new()
[void]$cb.AppendLine('// <auto-generated>')
[void]$cb.AppendLine('// Generated by scripts/gen-reserved-words.ps1 — DO NOT EDIT; re-run the script.')
[void]$cb.AppendLine('// Source: ISO/IEC 1989:2023 §8.10 Context-sensitive words (the whole table, verbatim).')
[void]$cb.AppendLine('// ContextSensitiveWordsDriftTests asserts this table equals tests/version-matrix/context-sensitive-words.json')
[void]$cb.AppendLine('// AND the spec section it came from.')
[void]$cb.AppendLine('// </auto-generated>')
[void]$cb.AppendLine('namespace CobolNet.Editions;')
[void]$cb.AppendLine('')
[void]$cb.AppendLine('public static partial class ContextSensitiveWords')
[void]$cb.AppendLine('{')
[void]$cb.AppendLine('    internal static readonly ContextSensitiveWordEntry[] Entries =')
[void]$cb.AppendLine('    [')
foreach ($w in $ctxSorted) {
    $c = $ctxRows[$w].Replace('\', '\\').Replace('"', '\"')
    [void]$cb.AppendLine("        new(""$w"", ""$c""),")
}
[void]$cb.AppendLine('    ];')
[void]$cb.AppendLine('}')
Set-LfContent -LiteralPath $ctxCsOut -Value $cb.ToString()

# ---- 8. Report COUNTS ONLY (content-filter rule) ----
$stats = [ordered]@{
    total = @($rows).Count; iso2023 = $iso2023.Count; gc85 = $gc85.Count; gc2002 = $gc2002.Count; gc2014 = $gc2014.Count
    at85 = @($rows | Where-Object r85).Count; at2002 = @($rows | Where-Object r2002).Count
    at2014 = @($rows | Where-Object r2014).Count; at2023 = @($rows | Where-Object r2023).Count
    removedPost85 = @($rows | Where-Object { $_.r85 -and -not $_.r2023 }).Count
    high = @($rows | Where-Object { $_.confidence -eq 'high' }).Count
    mediumIsoOnly = $mediumBuckets.isoOnly; mediumNonMonotone = $mediumBuckets.nonMonotone
    contextSensitive = $ctxRows.Count
}
Write-Output ("reserved-word tables generated: " + (($stats.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join ' '))
