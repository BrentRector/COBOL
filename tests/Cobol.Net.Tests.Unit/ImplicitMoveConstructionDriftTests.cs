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
/// ⛔ A BOUND NODE BUILT AFTER BINDING IS NEVER CHECKED BY A BIND PASS. That sentence is the whole of kb/Work
/// PB348: the implicit MOVE of every <c>… FROM</c> / <c>… INTO</c> phrase was constructed in the EMITTER —
/// <c>move.Emit(new BoundMove(from, [rl.Record]))</c> — downstream of every bind-time MOVE screen AND of the
/// storage facts the emitter's own output consumes.
///
/// <para><b>Measured cost, not a theory.</b> ISO §14.9.32.4 GR4 makes <c>RELEASE record-name-1 FROM x</c>
/// exactly <c>MOVE x TO record-name-1</c> followed by the same RELEASE without FROM. It was not:
/// <c>RELEASE SRT-REC FROM WS-NUM</c> (a PIC 9(3) sender into a PIC A(8) record) compiled clean where the
/// identical explicit MOVE drew COBOLNET0819; and <c>RELEASE SRT-NUM FROM QUOTE</c> ABORTED THE RUN with an
/// unhandled NotImplementedCobolFeatureException before the record was ever released, because
/// <c>MarkImageForced</c> — a fact <c>StorageFormPass</c> consumes — is collected by the binder and the
/// emitter-built move never passed through it. Same source, same SD, opposite result.</para>
///
/// <para><b>The invariant.</b> Every <c>BoundMove</c> the I-O and sort emitters render was built by
/// <c>MoveBinder</c>, so it carries the §14.9.25.3 syntax rules and the collected storage facts. Adding an I-O
/// verb with a FROM or INTO phrase and synthesizing its move at emission fails HERE, at the shape, rather than
/// silently in a program nobody has written yet.</para>
///
/// <para><b>What this can and cannot see.</b> It reads comment-stripped source, so a construction NAMED in
/// prose (this subject is discussed in several of these files) is not mistaken for one performed in code. It
/// cannot see a bound node built by a helper the emitter calls in another assembly.</para>
///
/// <para><b>And the non-phrase implicit moves (kb/Work PB880).</b> Three more were emitter-built after PB348 —
/// MOVE CORRESPONDING's per-pair moves, INITIALIZE's §14.9.20.4 GR4 stores and GOBACK RETURNING's move — and this
/// class used to NAME them in a dictionary so their count could only go down. A green test that enumerates known
/// holes is a decision, not a gate: <c>INITIALIZE G REPLACING NUMERIC DATA BY SPACE</c> aborted the run unit
/// behind it. All three are bound now, and the gate below says the number is ZERO.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ImplicitMoveConstructionDriftTests
{
    private const string Node = "BoundMove";

    /// <summary>The emitters that render a <c>… FROM</c> or <c>… INTO</c> phrase. Every one of them constructed
    /// a <c>BoundMove</c> before PB348; none of them may again.</summary>
    public static TheoryData<string> PhraseEmitters() => new()
    {
        "SequentialIoEmitter.cs",   // WRITE/REWRITE … FROM, READ … INTO — sequential + line-sequential
        "KeyedIoEmitter.cs",        // WRITE/REWRITE … FROM, READ … INTO — relative + indexed
        "SortEmitter.cs",           // RELEASE … FROM, RETURN … INTO
    };

    [Theory]
    [MemberData(nameof(PhraseEmitters))]
    public void NoFromOrIntoEmitter_ConstructsItsOwnBoundMove(string emitterFile)
    {
        string src = StripComments(File.ReadAllText(
            TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", emitterFile)));
        Assert.False(Regex.IsMatch(src, $@"\bnew\s+{Node}\s*\("),
            $"{emitterFile} constructs a {Node}. The implicit MOVE of a FROM / INTO phrase is BOUND — "
            + "MoveBinder.BindFromPhrase / BindIntoPhrase — so that the §14.9.25.3 syntax rules and the "
            + "MarkImageForced / MarkRefModStoreImage storage facts apply to it. A move built here escapes both, "
            + "which is how RELEASE … FROM QUOTE came to abort the run unit (kb/Work PB348).");
    }

    /// <summary>⛔ NO BoundMove IS CONSTRUCTED ANYWHERE IN CodeGen (kb/Work PB880). Every implicit move — each
    /// FROM / INTO phrase (PB348), INITIALIZE's §14.9.20.4 GR4 stores, MOVE CORRESPONDING's §14.9.25.4 GR11
    /// per-pair moves and GOBACK RETURNING's move — is built by <c>MoveBinder.BindMoveOf</c> and carried on the
    /// bound node the emitter walks. A move built after binding is checked by no bind pass and carries none of the
    /// storage facts codegen consumes; the three this test used to enumerate as "known" each aborted the run unit
    /// on legal source for exactly that reason.</summary>
    [Fact]
    public void NoEmitterConstructsABoundMove()
    {
        var found = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(
                     TestRepo.Src("Cobol.Net.Compiler", "CodeGen"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                continue;
            int n = Regex.Matches(StripComments(File.ReadAllText(file)), $@"\bnew\s+{Node}\s*\(").Count;
            if (n > 0) found[Path.GetFileName(file)] = n;
        }

        Assert.True(found.Count == 0,
            $"CodeGen constructs a {Node} in {string.Join(", ", found.Select(kv => $"{kv.Key}×{kv.Value}"))}. A move "
            + "built after binding is checked by no bind pass and carries none of the storage facts codegen consumes "
            + "(kb/Work PB348, PB880). Bind it — MoveBinder.BindMoveOf is the one entry, with an ImplicitMovePhrase "
            + "naming the statement — and carry the BoundMove on the bound node.");
    }

    // ── kb/Work PB337: the INTO phrase's OWN syntax rules ride the same one funnel ────────────────────────────

    /// <summary>
    /// ⛔ THE <c>… INTO</c> RECEIVER SCREEN EXISTS IN EXACTLY ONE PLACE, AND IT IS THE PLACE EVERY INTO ARM MUST
    /// GO THROUGH. ISO §14.9.30.3 SR1/SR2 (READ) and §14.9.34.3 SR2/SR3 (RETURN) had NO implementation at all
    /// before PB337 — both READ binders and <c>SortBinder.BindReturn</c> resolved identifier-1 and inspected
    /// nothing — and the reason all four rules could go missing together is that each verb was expected to check
    /// for itself. They are checked in <c>MoveBinder.BindIntoPhrase</c> instead, which every INTO arm already
    /// calls to get its implicit move, so a fourth INTO-bearing verb inherits the rules by construction.
    /// <para>This asserts the SHAPE that makes that true: the screen is called once, from the phrase binder. A
    /// second call site would mean a verb checking for itself again; a call from anywhere else would mean the
    /// funnel had been bypassed.</para>
    /// </summary>
    [Fact]
    public void TheIntoReceiverScreen_IsCalledOnlyFromThePhraseBinder()
    {
        var callers = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (string file in CompilerSources())
        {
            // The declaration lives in StatementValidation; every other mention is a call.
            if (Path.GetFileName(file) == "StatementValidation.cs") continue;
            int n = Regex.Matches(StripComments(File.ReadAllText(file)), @"\bCheckIntoReceiver\s*\(").Count;
            if (n > 0) callers[Path.GetFileName(file)] = n;
        }

        Assert.True(callers.Count == 1 && callers.TryGetValue("MoveBinder.cs", out int calls) && calls == 1,
            "StatementValidation.CheckIntoReceiver must be called exactly once, from MoveBinder.BindIntoPhrase "
            + $"— found [{string.Join(", ", callers.Select(kv => $"{kv.Key}×{kv.Value}"))}]. The INTO phrase's "
            + "admissibility rules (ISO §14.9.30.3 SR1/SR2, §14.9.34.3 SR2/SR3) are checked at the ONE place "
            + "the sequential READ, the keyed READ and the sort RETURN all funnel through, so the next "
            + "INTO-bearing verb cannot forget them (kb/Work PB337).");
    }

    /// <summary>The other half of the same invariant: an INTO phrase's rules row may be handed ONLY to
    /// <c>BindIntoPhrase</c>. Passing <c>IntoPhraseRules.X.Phrase</c> straight to <c>BindMoveOf</c> would build a
    /// correctly-labelled implicit move that skipped the receiver screen entirely — the MOVE rules would apply
    /// and the READ/RETURN rules would not, which is precisely the state PB337 found.</summary>
    [Fact]
    public void AnIntoPhraseRulesRow_ReachesOnlyBindIntoPhrase()
    {
        var offenders = new List<string>();
        foreach (string file in CompilerSources())
        {
            if (Path.GetFileName(file) == "BoundTree.cs") continue;   // where the rows are DECLARED
            string[] lines = StripComments(File.ReadAllText(file)).Split('\n');
            for (int i = 0; i < lines.Length; i++)
                if (Regex.IsMatch(lines[i], @"\bIntoPhraseRules\s*\.\s*(Read|Return)\b")
                    && !lines[i].Contains("BindIntoPhrase", StringComparison.Ordinal))
                    offenders.Add($"{Path.GetFileName(file)}:{i + 1}: {lines[i].Trim()}");
        }

        Assert.True(offenders.Count == 0,
            "An IntoPhraseRules row is referenced away from a BindIntoPhrase call:\n  "
            + string.Join("\n  ", offenders)
            + "\nThe row carries the verb's §14.9.30.3 / §14.9.34.3 syntax rules, and BindIntoPhrase is what "
            + "applies them. Routing the phrase to BindMoveOf directly binds the move without its own verb's "
            + "rules (kb/Work PB337).");
    }

    /// <summary>Every compiler source, obj/ excluded — the corpus both PB337 shape tests read.</summary>
    private static IEnumerable<string> CompilerSources() =>
        Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                                    StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                                    StringComparison.Ordinal));

    /// <summary>Drop line and block comments so a construction NAMED in prose is never mistaken for one
    /// performed in code — several of these files discuss this very subject at length.</summary>
    private static string StripComments(string s) =>
        Regex.Replace(Regex.Replace(s, @"/\*.*?\*/", " ", RegexOptions.Singleline), @"//[^\n]*", " ");
}
