// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ ONE QUESTION, ONE ENTRY (kb/Work PB878). ISO §14.9.25.3's MOVE-validity question — SR2, SR6/SR7/SR8, SR9 and
/// SR10 (Table 16) — is asked by the written MOVE, by every implicit move <c>MoveBinder.BindMoveOf</c> binds, by
/// §14.9.20.3 SR4 (INITIALIZE REPLACING's hypothetical MOVE), by §14.7.6 rule 2 (the CORRESPONDING pairing filter)
/// and by §14.8.2.3.3 rule 2d (a BY CONTENT / BY VALUE argument). Each asker used to compose the rule readers by
/// hand, and two of them composed a SUBSET: INITIALIZE never asked SR9 (a variable-length-group REPLACING operand
/// compiled clean and aborted the run unit) and the INVOKE screen asked Table 16 alone (a BINARY-LONG argument at a
/// PIC X formal arrived as <c>0000</c>).
/// <para>The shape that makes the NEXT rule automatic: the per-rule readers are PRIVATE to <see cref="MoveTable16"/>
/// and every asker goes through <c>Validity</c> / <c>DataItemRefusal</c>. This class pins both halves.</para>
/// </summary>
public sealed class MoveTable16AskerDriftTests
{
    /// <summary>The per-rule readers a caller could compose by hand. None may be reachable from outside the class.</summary>
    [Theory]
    [InlineData("ShapeRefusal")]
    [InlineData("VariableLengthRefusal")]
    [InlineData("StrongGroupRefusal")]
    public void ThePerRuleReaders_AreNotCallableOutsideTheTable(string reader)
    {
        var methods = typeof(MoveTable16).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => m.Name == reader).ToList();
        Assert.NotEmpty(methods);
        Assert.All(methods, m => Assert.True(m.IsPrivate,
            $"MoveTable16.{reader} is {(m.IsPublic ? "public" : "internal")}. It answers ONE §14.9.25.3 rule; an asker "
            + "that calls it directly composes the validity question by hand, which is how INITIALIZE came to skip "
            + "SR9 and INVOKE to skip SR8/SR9 (kb/Work PB878). Ask MoveTable16.Validity / DataItemRefusal."));
    }

    /// <summary>
    /// The askers of TABLE 16 ALONE (<c>MoveTable16.Refusal</c>) outside the class, each with the reason the whole
    /// chain is not the question it asks. The list can only shrink: a new caller fails here and must either ask
    /// <c>Validity</c> or say why its sender has no data item and no shape for SR2/SR6–SR9 to read.
    /// </summary>
    private static readonly Dictionary<string, string> TableOnlyAskers = new(StringComparer.Ordinal)
    {
        ["MoveBinder.cs"] = "SR10's --permissive re-reading, asked AFTER Validity has already cleared SR2/SR8/SR9 — "
            + "the lenient reading is defined against Table 16's cells alone",
        ["OoConformance.cs"] = "a NONNUMERIC LITERAL argument (§14.8.2.3.3 rule 2d), asked once per sender category "
            + "the bound literal could be, and a BOOLEAN-EXPRESSION argument (ContentValueMismatch, kb/Work PB1113) — "
            + "neither has a data item for SR2/SR8/SR9",
        // ⛔ AcceptDisplayBinder is NOT here any more (kb/Work PB887): the §14.9.1.4 GR7–GR12 conceptual temporal
        // item used to be a bare Table16Operand "that is not a data item", so ACCEPT asked Table 16 alone. It is
        // now materialized as the data item GR7–GR12 describe, and ACCEPT asks Validity with it — see
        // TheAcceptTemporalTransfer_AsksTheWholeChain below.
    };

    /// <summary>ACCEPT format 2 asks the WHOLE §14.9.25.3 chain, with the materialized conceptual item as its
    /// sender, and then binds the transfer through <c>BindMoveOf</c> (kb/Work PB887) — never Table 16 alone again.</summary>
    [Fact]
    public void TheAcceptTemporalTransfer_AsksTheWholeChain()
    {
        string src = StripComments(File.ReadAllText(
            Path.Combine(TestRepo.Src("Cobol.Net.Compiler"), "Binding", "Procedure", "Verbs", "AcceptDisplayBinder.cs")));
        Assert.Matches(@"\bMoveTable16\s*\.\s*Validity\s*\(", src);
        Assert.Matches(@"\bBindMoveOf\s*\([^;]*ImplicitMovePhrase\s*\.\s*AcceptTemporal", src);
        Assert.DoesNotMatch(@"\bMoveTable16\s*\.\s*Refusal\s*\(", src);
    }

    [Fact]
    public void Table16Alone_IsAskedOnlyByTheKnownNonItemSenders()
    {
        var found = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs",
                     SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || Path.GetFileName(file) == "MoveTable16.cs") continue;
            int n = Regex.Matches(StripComments(File.ReadAllText(file)), @"\bMoveTable16\s*\.\s*Refusal\s*\(").Count;
            if (n > 0) found[Path.GetFileName(file)] = n;
        }
        var unexpected = found.Keys.Where(f => !TableOnlyAskers.ContainsKey(f)).ToList();
        Assert.True(unexpected.Count == 0,
            $"MoveTable16.Refusal (Table 16 ALONE) is called from {string.Join(", ", unexpected)}. §14.9.25.3 SR10 "
            + "applies only \"for all other cases not described in Syntax rules 8 and 9\", so an asker holding a "
            + "data-item or bound-operand sender asks MoveTable16.Validity — the whole chain (kb/Work PB878).");
        var stale = TableOnlyAskers.Keys.Where(f => !found.ContainsKey(f)).ToList();
        Assert.True(stale.Count == 0,
            $"TableOnlyAskers names {string.Join(", ", stale)}, which no longer call MoveTable16.Refusal — remove "
            + "the entry so the list stays a statement of fact.");
    }

    private static string StripComments(string s) =>
        Regex.Replace(Regex.Replace(s, @"/\*.*?\*/", " ", RegexOptions.Singleline), @"//[^\n]*", " ");
}
