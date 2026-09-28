// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CobolNet.Binding.Bound;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A PHRASE THAT IS PARSED, BOUND AND EDITION-GATED AND THEN NEVER READ BY ITS EMITTER IS INVISIBLE TO EVERY
/// OTHER GATE. The bound node carries the field, the grammar accepts the source, the version pass rejects it
/// below its edition — so the construct looks supported from every side except the one that matters, and the
/// program silently runs as though the phrase had not been written.
/// <para>Three rows of the traceability inventory were open for exactly this shape at once: READ … ADVANCING ON
/// LOCK on the keyed organizations (kb/Work PB340 — <c>BoundKeyedRead.AdvancingOnLock</c> appeared NOWHERE in
/// <c>KeyedIoEmitter</c>), OPEN … WITH NO REWIND on the sharing arm (PB317) and the sequential READ direction
/// (PB334). One absent invariant, three statements. This is that invariant: every phrase property a bound I-O
/// node declares is READ by the emitter that renders it.</para>
/// <para><b>What this test can and cannot see.</b> It matches <c>identifier.Property</c> on the identifiers the
/// emitter declares as parameters of the node's own type, in the emitter's comment-stripped source — so a
/// property no one touched fails, which is the drift that keeps recurring. It cannot see a property that is
/// READ but then discarded, and it cannot see a phrase the BINDER never stored (PB334's absent direction field
/// is beyond any reflection over the node). Those need their own goldens; this one closes the "carried all the
/// way to emission and then dropped" class.</para>
/// </summary>
public sealed class BoundIoPhraseConsumptionDriftTests
{
    /// <summary>Each bound I-O node and the emitter that renders it. Adding a node here is free; adding a
    /// PROPERTY to one of these nodes without wiring it fails the test that names the node.</summary>
    public static TheoryData<string, string> Nodes() => new()
    {
        { nameof(BoundRead), "SequentialIoEmitter.cs" },
        { nameof(BoundWrite), "SequentialIoEmitter.cs" },
        { nameof(BoundRewrite), "SequentialIoEmitter.cs" },
        { nameof(BoundKeyedRead), "KeyedIoEmitter.cs" },
        { nameof(BoundKeyedWrite), "KeyedIoEmitter.cs" },
        { nameof(BoundKeyedRewrite), "KeyedIoEmitter.cs" },
        { nameof(BoundKeyedDelete), "KeyedIoEmitter.cs" },
        { nameof(BoundKeyedStart), "KeyedIoEmitter.cs" },
    };

    /// <summary>Properties whose consumption is deliberately elsewhere, each with the reason. An entry here is
    /// an adjudication, not a formality.</summary>
    private static readonly Dictionary<string, string> Exempt = new(StringComparer.Ordinal);

    [Theory]
    [MemberData(nameof(Nodes))]
    public void EveryPhrasePropertyOfABoundIoNode_IsReadByItsEmitter(string nodeName, string emitterFile)
    {
        Type node = typeof(BoundRead).Assembly.GetTypes().Single(t => t.Name == nodeName);
        string source = StripComments(File.ReadAllText(
            TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", emitterFile)));

        // The identifiers the emitter binds to this node type — a parameter or a local declared with the type.
        var receivers = Regex.Matches(source, $@"\b{Regex.Escape(nodeName)}\??\s+(?<id>[a-z]\w*)\b")
            .Select(m => m.Groups["id"].Value).Distinct(StringComparer.Ordinal).ToList();
        Assert.True(receivers.Count > 0,
            $"{emitterFile} declares no parameter of type {nodeName}: the node/emitter pairing in this test is "
            + "stale, or the verb moved to another emitter.");

        var missing = node.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            // The BoundStatement base's own members are not phrases of this statement: OperandEvaluation (kb/Work
            // PB1432) is read for EVERY node by StatementEmitter.EmitStatement, never by a verb's emitter.
            .Where(p => p.DeclaringType != typeof(BoundStatement))
            .Select(p => p.Name)
            .Where(p => !Exempt.ContainsKey($"{nodeName}.{p}"))
            .Where(p => !receivers.Any(r => Regex.IsMatch(source, $@"\b{Regex.Escape(r)}\s*[.?]\s*{Regex.Escape(p)}\b")))
            .ToList();

        Assert.True(missing.Count == 0,
            $"{nodeName} declares {missing.Count} phrase propert{(missing.Count == 1 ? "y" : "ies")} that "
            + $"{emitterFile} never reads: {string.Join(", ", missing)}. A phrase bound and then dropped at "
            + "emission runs as though it had not been written (kb/Work PB340). Wire it, or add it to Exempt "
            + "with the reason its consumption is elsewhere.");
    }

    /// <summary>Line and block comments removed, so a property merely NAMED in a comment (this subject appears
    /// in several) is not mistaken for one the code reads.</summary>
    private static string StripComments(string text) =>
        Regex.Replace(Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline), @"//[^\r\n]*", "");
}
