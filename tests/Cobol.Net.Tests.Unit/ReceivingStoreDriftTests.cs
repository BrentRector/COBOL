// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB871 — the ELEMENTARY CHARACTER RECEIVING STORE lives in ONE place, <c>CodeGen/Verbs/ReceivingStore.cs</c>.
/// ISO §8.5.1.10.4 is written over "a receiving operand", not over MOVE: every verb that transfers "according to the
/// rules for the MOVE statement" owes a dynamic-length receiver its whole new value. The defect was that the
/// dynamic arm existed only inside <c>MoveEmitter</c>, and UNSTRING and ACCEPT each built the fixed-width arm
/// themselves — at the PICTURE's one position (§13.18.19.3 SR1). This pins the structure, not a case: an emitter
/// under <c>CodeGen/Verbs</c> that builds either store directly is a new private copy of the rule.
/// </summary>
public sealed class ReceivingStoreDriftTests
{
    /// <summary>The one file allowed to spell the two stores. Deliberately NO other entry: <c>OoEmitter</c>'s BY
    /// CONTENT boolean argument copy was the last private copy, and it is "the MOVE store for the formal's
    /// category" by its own comment (§14.8.2.3.3 rule 2d), so it calls the home too.</summary>
    private static readonly HashSet<string> Allowed = new() { "ReceivingStore.cs" };

    [Fact]
    public void NoVerbEmitter_BuildsTheElementaryCharacterStoreItself()
    {
        var dir = TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs");
        var rx = new Regex(@"RuntimeApi\.(StrStoreAligned|DynStore)\(");
        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(dir, "*.cs"))
        {
            string name = Path.GetFileName(file);
            if (Allowed.Contains(name)) continue;
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
                if (rx.IsMatch(lines[i]) && !lines[i].TrimStart().StartsWith("//"))
                    offenders.Add($"{name}:{i + 1}: {lines[i].Trim()}");
        }
        Assert.True(offenders.Count == 0,
            "An emitter builds the elementary character receiving store itself instead of calling "
            + "ReceivingStore.Characters — a dynamic-length receiver would take the PICTURE's width there "
            + "(ISO §8.5.1.10.4; kb/Work PB871):\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void TheOneHome_StillCarriesBothArms()
    {
        // Make the scan above able to FAIL: if the home stops spelling the two stores, the allow-list entry is
        // stale and the drift test would pass vacuously over a rule that moved somewhere unscanned.
        string home = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", "ReceivingStore.cs"));
        Assert.Contains("RuntimeApi.DynStore(", home);
        Assert.Contains("RuntimeApi.StrStoreAligned(", home);
    }

    /// <summary>kb/Work PB1013 — ACCEPT format 1 sizes its device transfer by the RECEIVER's size at execution
    /// (ISO §14.9.1.4 GR3/GR4 "the size of the receiving data item"; an ANY LENGTH receiver's is its argument's,
    /// §13.18.2.4 GR1 b)), which is <c>ReceivingStore.CharacterPositions</c>. Every elementary arm used to spell the
    /// DECLARED width itself (<c>DisplayTextWidth</c>, <c>pic.Length</c>, <c>DynamicReceivingSize</c>), so an ANY
    /// LENGTH receiver took one character. This fails if any device read in <c>EmitAcceptDevice</c> other than the
    /// reference-modified slice sizes itself — the group arm included, whose size is the group's character
    /// positions (a bit / national group's as-if positions; kb/Work PB1653).</summary>
    [Fact]
    public void AcceptDevice_SizesEveryElementaryTransferByTheOneReceiverSize()
    {
        string src = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", "AcceptDisplayEmitter.cs"));
        int start = src.IndexOf("private void EmitAcceptDevice(", StringComparison.Ordinal);
        Assert.True(start >= 0, "EmitAcceptDevice not found — the drift test must follow the method");
        int end = src.IndexOf("/// <summary>", start, StringComparison.Ordinal);
        string body = end < 0 ? src[start..] : src[start..end];
        Assert.Contains("ReceivingStore.CharacterPositions(target)", body);
        var offenders = new List<string>();
        foreach (var line in body.Split('\n'))
        {
            if (line.TrimStart().StartsWith("//")) continue;
            foreach (Match m in Regex.Matches(line, @"AcceptSource\.Device(?:Boolean)?\(([^)]*)\)"))
                if (m.Groups[1].Value is not ("{size}" or "{len}"))
                    offenders.Add(line.Trim());
        }
        Assert.True(offenders.Count == 0,
            "An ACCEPT device arm sizes its transfer from something other than the receiver's run-time size "
            + "(ReceivingStore.CharacterPositions — ISO §14.9.1.4 GR3; kb/Work PB1013):\n" + string.Join("\n", offenders));
    }
}
