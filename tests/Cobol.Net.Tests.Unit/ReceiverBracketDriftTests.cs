// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A RECEIVING OBJECT PROPERTY IS ACCESSED WHEN ITS STATEMENT REACHES IT — ONE CLAIM, ONE PLACEMENT (kb/Work PB2078).
/// ISO §14.7.7 4) b) stores an arithmetic statement's result in each receiver "in the left-to-right order" with "Item
/// identification for the receiving data items … done as each data item is accessed"; §14.9.25.4 GR1 says the same of
/// MOVE. A property is its data item's stand-in (§8.4.3.9.4), so its GET and SET go around ITS access and store. The
/// pair of facts that makes that automatic for the next arithmetic statement is structural, and this pins it:
/// <list type="number">
///   <item>a binder that builds a receiver list the emitter loops over CLAIMS its property receivers
///     (<c>OoClaimInterleavedReceiver</c>) — arithmetic receivers in the ONE <c>ExpressionBinder.ReceiverOf</c>, never a
///     bare <c>new Receiver(</c>;</item>
///   <item>an emitter that loops over claimed receivers stores each through <c>ReceiverBracketEmitter.Receive</c> — every
///     <c>ArithmeticEmitter.GuardedStore</c> call names the receiver place it stores, and <c>MoveEmitter.Emit</c> routes
///     each target through <c>Receive</c>;</item>
///   <item>nobody else claims: a claim from a binder whose emitter never calls <c>Receive</c> would fail the compilation
///     of a legal program (<c>ReceiverBracketEmitter.Emit</c> throws on an unplaced bracket).</item>
/// </list>
/// </summary>
public sealed class ReceiverBracketDriftTests
{
    private static string Binding(params string[] path) => TestRepo.Src(["Cobol.Net.Compiler", "Binding", .. path]);
    private static string Verbs(string file) => TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", file);

    [Fact]
    public void ArithmeticReceivers_AreBuiltBy_TheOneClaimingConstructor()
    {
        string src = File.ReadAllText(Binding("Procedure", "ExpressionBinder.cs"));
        var constructions = Regex.Matches(src, @"new Receiver\(").Count;
        Assert.True(constructions == 1,
            $"ExpressionBinder builds a Receiver in {constructions} places; ReceiverOf is the ONE (it claims the receiver for "
            + "per-receiver property accessors, kb/Work PB2078) — route the new construction through it");
        int home = src.IndexOf("private Receiver ReceiverOf(", StringComparison.Ordinal);
        Assert.True(home >= 0 && src.IndexOf("new Receiver(", home, StringComparison.Ordinal) is var at && at > home && at < home + 400,
            "the one `new Receiver(` is not inside ReceiverOf");
        Assert.Contains("OoClaimInterleavedReceiver(resolved)", src[home..]);
    }

    [Fact]
    public void EveryClaim_IsMadeBy_ABinderWhoseEmitterPlacesTheBracket()
    {
        // The claim sites: the arithmetic receivers (ReceiverOf), the DIVIDE REMAINDER receiver, BindMoveOf (the written
        // MOVE and every implicit move), STRING INTO and INSPECT identifier-1.
        var claimants = new List<string>();
        foreach (var file in Directory.EnumerateFiles(Binding(), "*.cs", SearchOption.AllDirectories))
            if (File.ReadAllText(file).Contains("OoClaimInterleavedReceiver(") && Path.GetFileName(file) != "DataBinder.Oo.cs")
                claimants.Add(Path.GetFileName(file));
        claimants.Sort(StringComparer.Ordinal);
        Assert.Equal(["ArithmeticBinder.cs", "ExpressionBinder.cs", "InspectBinder.cs", "MoveBinder.cs", "StringUnstringBinder.cs"],
            claimants);
    }

    [Fact]
    public void TheOneMoveConstructor_ClaimsItsTargets_SoEveryImplicitMoveGetsTheBracket()
    {
        // BindMoveOf is the one constructor of a MOVE (the written statement and every implicit move a phrase defines), so the
        // claim lives there and not in the written MOVE's binder: a phrase added tomorrow inherits it by construction.
        string src = File.ReadAllText(Binding("Procedure", "Verbs", "MoveBinder.cs"));
        Assert.Single(Regex.Matches(src, @"OoClaimInterleavedReceiver\("));
        int ctor = src.IndexOf("public BoundMove BindMoveOf(", StringComparison.Ordinal);
        int claim = src.IndexOf("OoClaimInterleavedReceiver(", StringComparison.Ordinal);
        Assert.True(ctor >= 0 && claim > ctor && claim < src.IndexOf("new BoundMove(", ctor, StringComparison.Ordinal),
            "the one claim is not inside BindMoveOf before the BoundMove is built");
    }

    [Fact]
    public void StringAndInspectEmitters_PlaceTheirReceiver_ThroughTheBracketFunnel()
    {
        Assert.Contains("brackets.Receive(s.Into, null, () =>", File.ReadAllText(Verbs("StringEmitter.cs")));
        Assert.Contains("brackets.Receive(receiver, null, () => EmitInspect(statement));", File.ReadAllText(Verbs("InspectEmitter.cs")));
    }

    [Fact]
    public void ArithmeticEmitter_StoresEveryReceiver_ThroughTheBracketFunnel()
    {
        string[] lines = File.ReadAllLines(Verbs("ArithmeticEmitter.cs"));
        var bare = new List<string>();
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.StartsWith("//") || line.StartsWith("///") || !line.Contains("GuardedStore(")) continue;
            if (line.Contains("private void GuardedStore(")) continue;
            if (!Regex.IsMatch(line, @"GuardedStore\((ise|true|inSizeError), [A-Za-z.]+, \(\) =>"))
                bare.Add($"ArithmeticEmitter.cs:{i + 1}: {line}");
        }
        Assert.True(bare.Count == 0,
            "A GuardedStore call does not name the receiver it stores, so a property receiver's GET/SET would be dropped "
            + "(kb/Work PB2078):\n" + string.Join("\n", bare));
        string src = string.Join('\n', lines);
        Assert.Contains("brackets.Receive(receiver,", src);
        Assert.Contains("brackets.Receive(d.Remainder,", src);
    }

    [Fact]
    public void MoveEmitter_RoutesEachTarget_ThroughTheBracketFunnel()
    {
        string src = File.ReadAllText(Verbs("MoveEmitter.cs"));
        Assert.Contains("brackets.Receive(m.Targets[at], null, () => EmitReceiver(m, at));", src);
    }
}
