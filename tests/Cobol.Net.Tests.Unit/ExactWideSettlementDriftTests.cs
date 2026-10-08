// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ AN EXACT WIDE INTERMEDIATE (<c>NumX.Wide</c>, kb/Work PB1900) NEVER LEAVES <c>NumericRenderer</c> UNSETTLED, AND THE LIST OF
/// CONSUMERS THAT TAKE IT EXACT IS CLOSED.
/// <para>A nested native product past the Int128 carrier is carried as a <c>CobolWide</c> so a cancellation
/// (<c>A * B - C * D</c> over 21-digit operands) is exact. The form is NOT one of the <c>NumXCarrier</c>s the ~40 consumers
/// of a rendered <c>NumX</c> dispatch on (<c>x.Dec</c> / <c>x.Real</c> / <c>x.U</c>): a consumer handed one would read its
/// <c>CobolWide</c> text as an <c>Int128</c>. So every public entry of the renderer SETTLES it
/// (<c>NumericRenderer.Settle</c>: into the receiver's own scale and mode at the final transfer, else into the SDIDI), and a
/// consumer may take it exact only by passing <c>keepWide: true</c> — which it then owes a settlement of its own. This class
/// pins both halves: the entries all settle, and the opt-in sites are the two that do (a several-receiver COMPUTE's one
/// initial evaluation and a relation). A third site is a design decision (add its settlement here), never a silent edit.</para>
/// </summary>
public sealed class ExactWideSettlementDriftTests
{
    private static readonly string[] OptInSites = ["ArithmeticEmitter.cs", "ConditionRenderer.cs"];

    private static string Renderer() =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Emit", "NumericRenderer.cs"));

    private static string BodyOf(string src, string signaturePrefix)
    {
        int at = src.IndexOf(signaturePrefix, StringComparison.Ordinal);
        Assert.True(at >= 0, $"NumericRenderer no longer declares '{signaturePrefix}'");
        int open = src.IndexOf('{', at);
        int depth = 0, i = open;
        for (; i < src.Length; i++)
        {
            if (src[i] == '{') depth++;
            else if (src[i] == '}' && --depth == 0) break;
        }
        return src[open..(i + 1)];
    }

    /// <summary>Every public entry that hands a rendered value to a caller settles it unless the caller asked to keep it.</summary>
    [Fact]
    public void EveryPublicRenderEntry_SettlesTheWideForm()
    {
        string src = Renderer();
        foreach (string entry in new[] { "public NumX Render(BoundExpr e", "public NumX AsNum(BoundOperand op" })
            Assert.Contains("keepWide ? x : Settle(x, rcv, outermost)", BodyOf(src, entry));
        Assert.Contains("return Settle(acc, rcv, outermost: false);", BodyOf(src, "public NumX Fold("));
        Assert.Contains("Settle(CombineCore(a, op, b), rcv, outermost)", BodyOf(src, "public NumX Combine("));
    }

    /// <summary>The consumers that opt in to the exact form are exactly the two that settle it themselves.</summary>
    [Fact]
    public void OnlyTheArithmeticStatementsAndTheRelation_KeepTheWideForm()
    {
        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler", "CodeGen"), "*.cs", SearchOption.AllDirectories))
        {
            string name = Path.GetFileName(file);
            if (name == "NumericRenderer.cs") continue;                       // the declaration itself
            string text = File.ReadAllText(file);
            if (Regex.IsMatch(text, @"keepWide\s*:\s*true") && !OptInSites.Contains(name)) offenders.Add(name);
        }
        Assert.True(offenders.Count == 0,
            "These files ask for an UNSETTLED exact wide intermediate (keepWide: true) without being a registered settling site "
            + "(ExactWideSettlementDriftTests.OptInSites): " + string.Join(", ", offenders));
    }

    /// <summary>The opt-in sites really do settle: the guard above would pass vacuously if they stopped.</summary>
    [Fact]
    public void TheOptInSites_SettleWhatTheyKeep()
    {
        string arithmetic = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", "ArithmeticEmitter.cs"));
        Assert.Contains("keepWide: true", arithmetic);
        Assert.Contains("num.Settle(v, RcvFor(r, ise), outermost: true)", arithmetic);
        string condition = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Emit", "ConditionRenderer.cs"));
        Assert.Contains("keepWide: true", condition);
        Assert.Contains("NumericRenderer.CompareWide(l, rr)", condition);
        Assert.Contains("NumericRenderer.LowerWide(l)", condition);
    }

    /// <summary>The wide form is not a carrier: no consumer may dispatch on it, so <c>NumX.Carrier</c> refuses it.</summary>
    [Fact]
    public void TheWideForm_IsNotACarrier()
    {
        string core = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Emit", "EmitCore.cs"));
        Assert.Matches(@"Carrier\s*=>\s*Wide\s*\?\s*throw", core);
        // The enum is internal to the compiler, so its members are read from source.
        Match members = Regex.Match(core, @"enum\s+NumXCarrier\s*\{(?<m>[^}]*)\}");
        Assert.True(members.Success, "NumXCarrier is no longer declared in EmitCore.cs");
        Assert.DoesNotContain("Wide", members.Groups["m"].Value.Replace("UnsignedWide", ""));
    }
}
