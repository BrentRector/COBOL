# Copyright (c) 2026 Brent Rector. All rights reserved.
# Licensed under the Business Source License 1.1. See LICENSE file in the project root.
#
# gen-constructs.ps1 — generate the construct registry from THE canonical catalogue
# (rearchitecture PHASE 02, P2.5). tests/version-matrix/constructs.json is the SINGLE hand-authored source;
# this script renders it into two committed C# build-outputs, mirroring the gen-reserved-words.ps1 discipline
# (manual regen + committed .g.cs + a drift test — ConstructRegistryDriftTests — asserts they agree; there is no
# MSBuild build hook, so a stale .g.cs is caught by CI, not a silent wrong build).
#
# Input:
#   tests/version-matrix/constructs.json  — the canonical rows: id / display / introducedIn / removedIn / obsoleteIn /
#                                           diagnosticCode / citation / directiveWords (+ description / vcr / source /
#                                           status / expectDiagnostic used by the version-matrix tests, not the
#                                           registry). directiveWords (kb/Work PB725) lists the ISO §7.3 compiler-
#                                           directive words the row gates; CompilerDirectiveCatalog inverts them into
#                                           the word → row map the text-manipulation stage gates on, so a new
#                                           directive is ONE row here plus a regen — never a hand-kept name set.
# Outputs (ALL committed; ConstructRegistryDriftTests asserts they equal constructs.json):
#   src/Cobol.Net.Editions/ConstructRegistry.g.cs  — the ConstructRegistry.Entries list (a partial).
#   src/Cobol.Net.Editions/Constructs.g.cs         — one `public const string <PascalId> = "<id>";` per row.

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$jsonIn   = Join-Path $repo 'tests/version-matrix/constructs.json'
$regOut   = Join-Path $repo 'src/Cobol.Net.Editions/ConstructRegistry.g.cs'
$idsOut   = Join-Path $repo 'src/Cobol.Net.Editions/Constructs.g.cs'

function To-Pascal([string]$id) {
    (($id -split '-') | ForEach-Object {
        if ($_.Length -eq 0) { '' } else { $_.Substring(0,1).ToUpper() + $_.Substring(1) }
    }) -join ''
}
function Esc([string]$s) { $s.Replace('\','\\').Replace('"','\"') }
function NullOrInt($v) { if ($null -eq $v) { 'null' } else { [string][int]$v } }

$rows = (Get-Content -Raw -LiteralPath $jsonIn | ConvertFrom-Json).constructs
if ($rows.Count -lt 1) { throw "constructs.json parsed to 0 rows" }

# --- validate: unique ids, unique PascalIds, required fields present ---
$seenId = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$seenPascal = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($r in $rows) {
    foreach ($f in 'id','display','diagnosticCode','citation','introducedIn') {
        if ($null -eq $r.$f) { throw "row '$($r.id)' is missing required field '$f'" }
    }
    if (-not $seenId.Add($r.id)) { throw "duplicate construct id '$($r.id)'" }
    if (-not $seenPascal.Add((To-Pascal $r.id))) { throw "PascalId collision for '$($r.id)'" }
    # ⛔ A DIRECTIVE ROW CARRIES ITS OPERAND SYNTAX (kb/Work PB794). "Which words may follow the directive word"
    # is the same kind of fact as "from which edition may the word appear", so it lives on the same row — and the
    # partition is TOTAL by construction: words (a closed set, checked centrally), text (content unchecked, and
    # the citation says by whose decision), or stage (a named downstream stage parses it). The state this check
    # forbids is the one that shipped: a directive nobody checked and nothing recorded as unchecked.
    if ($null -ne $r.directiveWords -and @($r.directiveWords).Count -gt 0) {
        if ($null -eq $r.directiveOperand) { throw "directive row '$($r.id)' has directiveWords but no directiveOperand — see DirectiveOperandSyntax (kb/Work PB794)" }
        if ($r.directiveOperand.form -notin 'words','text','stage') { throw "row '$($r.id)': directiveOperand.form '$($r.directiveOperand.form)' is not words/text/stage" }
        if ([string]::IsNullOrWhiteSpace($r.directiveOperand.citation)) { throw "row '$($r.id)': directiveOperand needs a citation — an unchecked operand is a cited decision, never a silence" }
        if ($r.directiveOperand.form -eq 'stage' -and [string]::IsNullOrWhiteSpace($r.directiveOperand.owner)) { throw "row '$($r.id)': a stage-owned operand needs an owner" }
        if ($r.directiveOperand.form -eq 'words' -and @($r.directiveOperand.choice).Count -eq 0 -and -not $r.directiveOperand.directiveName -and -not $r.directiveOperand.userWord) { throw "row '$($r.id)': a words operand needs a choice, a directiveName or a userWord alternative" }
    }
    elseif ($null -ne $r.directiveOperand) { throw "row '$($r.id)' carries directiveOperand but no directiveWords" }
    # A directive's PLACEMENT rule (kb/Work PB1377/PB1378/PB1065) — where §7.3 says it may not be written — is data on
    # the same row, so the stage that judges the rule shape is written once and a new directive with the same
    # restriction is one field here.
    if ($null -ne $r.directivePlacement) {
        if ($null -eq $r.directiveWords -or @($r.directiveWords).Count -eq 0) { throw "row '$($r.id)' carries directivePlacement but no directiveWords" }
        if ($r.directivePlacement.rule -notin 'outsideCompilationUnits','beforeFirstCompilationUnit','betweenClauses') { throw "row '$($r.id)': directivePlacement.rule '$($r.directivePlacement.rule)' is not outsideCompilationUnits/beforeFirstCompilationUnit/betweenClauses" }
        if ([string]::IsNullOrWhiteSpace($r.directivePlacement.citation) -or [string]::IsNullOrWhiteSpace($r.directivePlacement.text)) { throw "row '$($r.id)': directivePlacement needs a citation and the rule's text" }
    }
}

$hdr = @"
// <auto-generated>
// Generated by scripts/gen-constructs.ps1 from tests/version-matrix/constructs.json — DO NOT EDIT; re-run the script.
// tests/version-matrix/constructs.json is THE canonical version-matrix catalogue; ConstructRegistryDriftTests
// asserts this output equals it (a stale file is a CI failure, not a silent wrong build).
// </auto-generated>
"@

# ---- 1. ConstructRegistry.g.cs (the Entries partial) ----
$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine($hdr)
[void]$sb.AppendLine('namespace CobolNet.Editions;')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('public static partial class ConstructRegistry')
[void]$sb.AppendLine('{')
[void]$sb.AppendLine('    /// <summary>The in-code rendering of constructs.json (generated). Pending rows (not yet')
[void]$sb.AppendLine('    /// implemented) are REGISTERED — their edition metadata is frozen here.</summary>')
[void]$sb.AppendLine('    public static readonly IReadOnlyList<ConstructDialectStatus> Entries =')
[void]$sb.AppendLine('    [')
foreach ($r in $rows) {
    $line = '        new("{0}", "{1}", {2}, {3}, {4}, "{5}", "{6}")' -f `
        (Esc $r.id), (Esc $r.display), ([int]$r.introducedIn), (NullOrInt $r.removedIn), (NullOrInt $r.obsoleteIn), `
        (Esc $r.diagnosticCode), (Esc $r.citation)
    # The ISO §7.3 directive words this row gates (kb/Work PB725) — an object initializer only on the rows
    # that carry them, so the 200-odd non-directive rows keep their one-line rendering.
    if ($null -ne $r.directiveWords -and @($r.directiveWords).Count -gt 0) {
        $words = (@($r.directiveWords) | ForEach-Object { '"' + (Esc $_) + '"' }) -join ', '
        $init = 'DirectiveWords = [{0}]' -f $words
        # The OPERAND column (kb/Work PB794) — which words may FOLLOW the directive word, on the same row that
        # already says from which edition the word may appear at all. Every directive row carries one (the
        # validation above refuses a row that does not), so a new directive cannot be silently unchecked.
        $init += ', DirectiveOperand = new() { Form = DirectiveOperandForm.' + (To-Pascal $r.directiveOperand.form)
        if ($null -ne $r.directiveOperand.optionalWords -and @($r.directiveOperand.optionalWords).Count -gt 0) {
            $init += ', OptionalWords = [{0}]' -f ((@($r.directiveOperand.optionalWords) | ForEach-Object { '"' + (Esc $_) + '"' }) -join ', ')
        }
        if ($null -ne $r.directiveOperand.choice -and @($r.directiveOperand.choice).Count -gt 0) {
            $init += ', Choice = [{0}]' -f ((@($r.directiveOperand.choice) | ForEach-Object { '"' + (Esc $_) + '"' }) -join ', ')
        }
        if ($r.directiveOperand.choiceOmissible) { $init += ', ChoiceOmissible = true' }
        if ($r.directiveOperand.directiveName)   { $init += ', DirectiveName = true' }
        if ($null -ne $r.directiveOperand.excludedDirectives -and @($r.directiveOperand.excludedDirectives).Count -gt 0) {
            $init += ', ExcludedDirectives = [{0}]' -f ((@($r.directiveOperand.excludedDirectives) | ForEach-Object { '"' + (Esc $_) + '"' }) -join ', ')
        }
        if ($null -ne $r.directiveOperand.noOperandWords -and @($r.directiveOperand.noOperandWords).Count -gt 0) {
            $init += ', NoOperandWords = [{0}]' -f ((@($r.directiveOperand.noOperandWords) | ForEach-Object { '"' + (Esc $_) + '"' }) -join ', ')
        }
        if ($r.directiveOperand.userWord)        { $init += ', UserWord = true' }
        if ($r.directiveOperand.operandRequired) { $init += ', OperandRequired = true' }
        if (-not [string]::IsNullOrEmpty($r.directiveOperand.owner)) { $init += ', Owner = "{0}"' -f (Esc $r.directiveOperand.owner) }
        $init += ', Citation = "{0}" }}' -f (Esc $r.directiveOperand.citation)
        if ($null -ne $r.directivePlacement) {
            $rule = $r.directivePlacement.rule
            $init += ', Placement = new(DirectivePlacementRule.{0}, "{1}", "{2}")' -f ($rule.Substring(0,1).ToUpper() + $rule.Substring(1)), (Esc $r.directivePlacement.citation), (Esc $r.directivePlacement.text)
        }
        $line += (' {{ {0} }}' -f $init)
    }
    [void]$sb.AppendLine($line + ',')
}
[void]$sb.AppendLine('    ];')
[void]$sb.AppendLine('}')
Set-Content -LiteralPath $regOut -Value ($sb.ToString().TrimEnd() + "`n") -NoNewline -Encoding utf8

# ---- 2. Constants.g.cs (compile-checked ids) ----
$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine($hdr)
[void]$sb.AppendLine('namespace CobolNet.Editions;')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('/// <summary>Compile-checked constructs.json row ids (P2.6): use <c>Constructs.X</c> at every')
[void]$sb.AppendLine('/// <see cref="ConstructRegistry.Check"/> call site so an unregistered id is a COMPILE error.</summary>')
[void]$sb.AppendLine('public static class Constructs')
[void]$sb.AppendLine('{')
foreach ($r in $rows) {
    [void]$sb.AppendLine(('    public const string {0} = "{1}";' -f (To-Pascal $r.id), (Esc $r.id)))
}
[void]$sb.AppendLine('}')
Set-Content -LiteralPath $idsOut -Value ($sb.ToString().TrimEnd() + "`n") -NoNewline -Encoding utf8

# (The former section 3 — Gating/GateId.cs — was DELETED at Exec Step E: the P2.7 forward-stamping
# consumer was abandoned (DEVLOG 679) and the enum had ZERO production consumers; the registry rows +
# Constructs consts are the live outputs.)

Write-Host ("gen-constructs: {0} rows -> Entries; {0} ids" -f $rows.Count)
