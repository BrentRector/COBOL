// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY VERB BINDER STATES AN OPERAND'S ROLE WHEN IT RESOLVES IT (kb/Work PB881, CLAUDE.md rule 5).
/// <para>The receiving-operand prohibitions — ISO §13.18.15.3 SR2 ("Neither the data item described by the
/// subject of the entry nor any data item subordinate to the subject of the entry shall be specified as a
/// receiving data item"), §8.4.3.15.3 SR3, §8.4.3.14.3 SR2, §8.4.3.6.3 SR1, §13.10.4 GR1 — are rules about an
/// OPERAND ROLE, never about a statement, and they are written ONCE, in
/// <c>ExpressionBinder.ResolveReceiving</c>. The plain reference resolver (<c>ctx.Refs.Resolve</c>) answers only
/// "where does this live", so a verb binder that reached it for a receiving operand opted out of all of them in
/// silence: INSPECT REPLACING/CONVERTING, READ/RETURN INTO, MOVE/ADD/SUBTRACT CORRESPONDING's receiving group,
/// SEARCH VARYING, INVOKE RETURNING, and the SET pointer / object-reference receivers each rewrote a CONSTANT
/// RECORD the identical MOVE refuses.</para>
/// <para>The shape that keeps the NEXT verb from repeating it: a binder under <c>Verbs/</c> never calls the
/// resolver directly — it asks <c>host.Expr.ResolveSending</c> or <c>host.Expr.ResolveReceiving</c>, so the role
/// is a decision written at the site, and a new binder that forgets to make it fails here.</para>
/// </summary>
public sealed class ReceivingResolutionDriftTests
{
    private static readonly string VerbsDir =
        TestRepo.Src("Cobol.Net.Compiler", "Binding", "Procedure", "Verbs");

    private static readonly string ExpressionBinderPath =
        TestRepo.Src("Cobol.Net.Compiler", "Binding", "Procedure", "ExpressionBinder.cs");

    /// <summary>The file's code with every comment removed — prose naming the resolver is not a call to it.</summary>
    private static string CodeOf(string path)
    {
        string text = File.ReadAllText(path);
        text = Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline);
        return Regex.Replace(text, @"//[^\r\n]*", "");
    }

    [Fact]
    public void NoVerbBinder_ResolvesAnOperandWithoutStatingItsRole()
    {
        var offenders = Directory.EnumerateFiles(VerbsDir, "*.cs", SearchOption.AllDirectories)
            .SelectMany(f => CodeOf(f).Split('\n')
                .Select((line, i) => (file: Path.GetFileName(f), line: i + 1, text: line.Trim()))
                .Where(l => Regex.IsMatch(l.text, @"\bRefs\s*\.\s*Resolve\s*\(")))
            .Select(l => $"{l.file}:{l.line}: {l.text}")
            .ToList();
        Assert.True(offenders.Count == 0,
            "A verb binder resolves a data reference through ctx.Refs.Resolve, which screens nothing. Resolve it "
            + "through host.Expr.ResolveReceiving when the statement stores into it (every receiving-operand "
            + "prohibition — ISO §13.18.15.3 SR2 first among them — lives there) or host.Expr.ResolveSending when it "
            + "does not (kb/Work PB881):\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void TheReceivingChokepoint_AsksTheConstantRecordRule()
    {
        string code = CodeOf(ExpressionBinderPath);
        // kb/Work PB1418: the prohibitions became a table ResolveReceiving REPORTS and PermitsReceiving (§8.4.3.2.4
        // GR5's argument-manner test) only ASKS — so the chokepoint must still consult the place half of that table,
        // and the place half must still ask the constant-record rule.
        string resolve = MemberBody(code, "public Place? ResolveReceiving(");
        Assert.True(resolve.Contains("ReceivingPlaceBar(", System.StringComparison.Ordinal)
                && resolve.Contains("ReceivingFormBar(", System.StringComparison.Ordinal),
            "ResolveReceiving no longer consults both halves of the receiving-prohibition table (ReceivingFormBar / "
            + "ReceivingPlaceBar) — every verb's receiver passes through it.");
        Assert.True(MemberBody(code, "internal bool PermitsReceiving(").Contains("ReceivingPlaceBar(", System.StringComparison.Ordinal),
            "PermitsReceiving no longer asks the same prohibition table ResolveReceiving reports — the §8.4.3.2.4 GR5 "
            + "argument manner and the receiving chokepoint would disagree about which identifiers are receivable.");
        Assert.True(MemberBody(code, "private ReceivingBar? ReceivingPlaceBar(").Contains("ConstantStoreProhibition(", System.StringComparison.Ordinal),
            "ReceivingPlaceBar no longer asks DataBinder.ConstantStoreProhibition — ISO §13.18.15.3 SR2 is a rule over "
            + "every receiving data item and this is the one place every verb's receiver passes through.");
    }

    /// <summary>The code of the member whose declaration starts with <paramref name="signature"/>, up to the next
    /// member declared at class level.</summary>
    private static string MemberBody(string code, string signature)
    {
        int start = code.IndexOf(signature, System.StringComparison.Ordinal);
        Assert.True(start >= 0, $"ExpressionBinder no longer declares '{signature}'.");
        int next = Regex.Match(code[(start + 1)..], @"\n    (public|internal|private) ").Index;
        return next <= 0 ? code[start..] : code[start..(start + 1 + next)];
    }
}
