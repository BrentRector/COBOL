// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE DRIFT TEST for the SET diagnostics' ELIDED RECEIVERS (kb/Work PB388's last half).
/// <para>Every receiving brace in ISO §14.9.39.2 is printed <c>{ … } …</c>, so every SET diagnostic about a
/// receiver is about a LIST — and the binder has that list in hand at the moment it reports. Thirteen message
/// strings opened <c>SET …</c> anyway, and the diagnostic renderer transliterates U+2026 to ASCII, so what the
/// programmer actually read was <c>SET . TO SELF</c>: a statement nobody wrote, naming neither the operands
/// they wrote nor (the <c>SELF/SUPER</c> arms) the sender they wrote. Measured on this tree before the repair:
/// <c>SET P1 P2 TO SELF</c> over two <c>USAGE POINTER</c> items printed
/// <c>COBOLNET0869: SET … TO SELF/SUPER: …</c>.</para>
/// <para>⛔ THE REPAIR IS STRUCTURAL, SO THE TEST IS STRUCTURAL. Nine call sites had each written their own
/// <c>string.Join(' ', refs.Select(r =&gt; $"'{r.GetText()}'"))</c> while five siblings holding the same list
/// wrote <c>…</c> instead; the naming now has ONE home, <c>SetFormatSelection.Written</c>, which is what a new
/// arm reaches for. Both halves are asserted: no <b>message</b> may OPEN with an elided SET, and no file may
/// carry a second copy of the naming idiom. A form REFERENCE — "the receiving operand of <c>SET … TO ENTRY</c>
/// shall be USAGE PROGRAM-POINTER" — is the general format's own ellipsis and is deliberately untouched: it
/// appears mid-sentence, never at the head of the message, which is what the head-anchored pattern keys on.</para>
/// <para>Proven to FAIL before it was trusted: <see cref="ThePatternThisTestKeysOn_FiresOnThePB388Defect"/>
/// feeds the matcher the exact line the tree carried and asserts it is caught, and feeds it the repaired twin
/// and asserts it is not (feedback_green_gates_arent_evidence — a gate that has never been seen to fail is not
/// evidence). The behavioural half lives in the negative goldens
/// <c>pb388-set-pointer-self-sender</c> / <c>pb388-set-object-ref-self-outside-method</c>.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class SetDiagnosticNamesReceiversDriftTests
{
    /// <summary>The SET-family binders: every file that reports about a SET receiving operand.</summary>
    private static readonly string[] Binders =
    [
        Path.Combine("Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", "SetBinder.cs"),
        Path.Combine("Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", "OoBinder.cs"),
        Path.Combine("Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", "SetFormatSelection.cs"),
        Path.Combine("Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", "PtrBinder.cs"),
    ];

    /// <summary>A diagnostic message whose FIRST literal opens with the verb SET and then elides — the echo
    /// position. <c>ctx.Edition.Error(&lt;code&gt;, $"SET … TO …</c>. The ellipsis is matched in both the U+2026
    /// and the three-dot spellings, because the renderer collapses the first into the second.</summary>
    private static readonly Regex ElidedEcho = new(
        @"Error\(\s*[^,()]*(?:\([^()]*\))?[^,()]*,\s*\$?""SET (?:…|\.\.\.)",
        RegexOptions.Compiled);

    /// <summary>The receiver-naming idiom, which belongs in <c>SetFormatSelection.Written</c> and nowhere
    /// else. A second copy is how the family drifted apart in the first place: nine sites named the receivers
    /// and five, holding the same list, did not.</summary>
    private static readonly Regex NamingIdiom = new(
        @"string\.Join\(\s*'\s*'\s*,\s*\w+\.Select\(\s*\w+\s*=>\s*\$""'\{\w+\.GetText\(\)\}'""\s*\)\s*\)",
        RegexOptions.Compiled);

    [Fact]
    public void NoSetDiagnosticOpensWithAnElidedReceiverList()
    {
        var offenders = new List<string>();
        foreach (string rel in Binders)
        {
            string text = File.ReadAllText(TestRepo.Src(rel));
            foreach (Match m in ElidedEcho.Matches(text))
            {
                int line = text.Take(m.Index).Count(c => c == '\n') + 1;
                offenders.Add($"{rel}:{line}  {m.Value.Trim()}");
            }
        }
        Assert.True(offenders.Count == 0,
            "A SET diagnostic ECHOES the statement with its receiving operands elided. The receiving brace of "
            + "every SET general format is `{ … } …` (ISO §14.9.39.2) and the binder holds that list where it "
            + "reports, so the message names it through SetFormatSelection.Written — the renderer "
            + "transliterates U+2026 to ASCII and the user then reads `SET . TO …`, a statement nobody wrote "
            + "(kb/Work PB388):\n  " + string.Join("\n  ", offenders));
    }

    [Fact]
    public void TheReceiverNamingHasExactlyOneHome()
    {
        var offenders = new List<string>();
        foreach (string rel in Binders)
        {
            string text = File.ReadAllText(TestRepo.Src(rel));
            foreach (Match m in NamingIdiom.Matches(text))
            {
                // The ONE home is the helper's own body — the idiom has to be written down once somewhere.
                if (text[Math.Max(0, m.Index - 160)..m.Index].Contains("string Written(", StringComparison.Ordinal))
                    continue;
                int line = text.Take(m.Index).Count(c => c == '\n') + 1;
                offenders.Add($"{rel}:{line}");
            }
        }
        Assert.True(offenders.Count == 0,
            "The SET receiver-naming idiom has a second copy. It belongs to SetFormatSelection.Written and "
            + "nowhere else: nine hand-written copies is what let five sibling messages elide the same list "
            + "(kb/Work PB388). Sites: " + string.Join(", ", offenders));
    }

    /// <summary>⛔ THE GATE FIRES. The defect half is the line this tree carried verbatim; the repaired half is
    /// the line that replaced it, plus the FORM reference the check must leave alone.</summary>
    [Fact]
    public void ThePatternThisTestKeysOn_FiresOnThePB388Defect()
    {
        const string defect =
            "            ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,\n"
            + "                \"SET … TO SELF/SUPER: SELF and SUPER are object references, not data pointers \"";
        const string repaired =
            "            ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,\n"
            + "                $\"SET {SetFormatSelection.Written(targetRefs)} TO {senderText}: SELF and SUPER \"";
        const string formReference =
            "                ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,\n"
            + "                    $\"SET '{drefs[i].GetText()}': the receiving operand of SET … TO ENTRY \"";

        Assert.True(ElidedEcho.IsMatch(defect), "the elided-echo pattern no longer matches the PB388 defect");
        Assert.False(ElidedEcho.IsMatch(repaired), "the elided-echo pattern matches the REPAIRED message");
        Assert.False(ElidedEcho.IsMatch(formReference),
            "the elided-echo pattern matches a general-format REFERENCE, whose `…` is the standard's own");

        const string idiomDefect = "string.Join(' ', targetRefs.Select(t => $\"'{t.GetText()}'\"))";
        Assert.True(NamingIdiom.IsMatch(idiomDefect), "the naming-idiom pattern no longer matches a second copy");
        Assert.False(NamingIdiom.IsMatch("SetFormatSelection.Written(targetRefs)"),
            "the naming-idiom pattern matches the shared helper it exists to protect");
    }
}
