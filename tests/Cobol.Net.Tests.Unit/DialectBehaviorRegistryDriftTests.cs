// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Runtime;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The drift guard that bolts the BEHAVIOUR register (<see cref="DialectBehaviors"/>, the runtime's table of
/// named per-edition behaviour changes) to the ledger that owns the obligation
/// (<c>docs/VERSION_CHANGE_REFERENCE.md</c> Table 1, whose gating action for such a row is
/// <c>gate-behavior-by-dialect</c>) — kb/Work PB344.
///
/// <para>⛔ WHY A SECOND ANCHOR KIND EXISTS. A VCR row's <c>&lt;!-- gate:id --&gt;</c> anchor resolves to a
/// <c>tests/version-matrix/constructs.json</c> row, and that register answers "does this edition HAVE this
/// construct?" — every row of it carries a DIAGNOSTIC code, because its answer is a rejection. A behaviour
/// change has no diagnostic and cannot have one: the source is legal at every edition and only the ANSWER
/// differs, so it can never be a <c>constructs.json</c> row. Those rows sat at <c>&lt;!-- todo --&gt;</c> —
/// indistinguishable from "nobody has done the work" — while the compiler shipped the 2023 rule to
/// <c>--std 85/2002/2014</c>. The <c>&lt;!-- behavior:id --&gt;</c> anchor resolves HERE instead, and this test
/// holds the two registers together in BOTH directions so neither can rot: a member with no row, a row whose
/// anchor names no member, or a landed gate still marked todo fails the gate.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class DialectBehaviorRegistryDriftTests
{
    /// <summary>A VCR change-table row number — the same grammar <c>VcrDriftTests</c> parses (Tables 1–6 use
    /// <c>28</c>/<c>130e</c>; Table 7 the dotted <c>7.20a</c> form).</summary>
    private static readonly Regex RowNumRx = new(@"^\|\s*([0-9]+(?:\.[0-9]+)?[a-z]?)\s*\|", RegexOptions.Compiled);

    /// <summary>Every <c>behavior:</c> anchor on a change row, with that row's number. Only lines that START a
    /// real table row are read, so the legend's own example anchor is never parsed as a claim.</summary>
    private static readonly Regex AnchorRx = new(@"<!--\s*behavior:([^\s>]+)\s*-->", RegexOptions.Compiled);

    private static string[] Vcr() => File.ReadAllLines(TestRepo.Docs("VERSION_CHANGE_REFERENCE.md"));

    private static List<(string Row, string Id, string Line)> Anchors(string[] lines)
    {
        var res = new List<(string, string, string)>();
        foreach (string ln in lines)
        {
            var rm = RowNumRx.Match(ln);
            if (!rm.Success) continue;
            foreach (Match m in AnchorRx.Matches(ln))
                res.Add((rm.Groups[1].Value, m.Groups[1].Value, ln));
        }
        return res;
    }

    /// <summary>The table is indexed by the enum member's ordinal — the invariant
    /// <see cref="DialectBehaviors.Of"/> and <see cref="DialectBehaviors.IsActive"/> are an array read because
    /// of. The static constructor asserts it too; this states it where a reviewer looks for it, and adds the
    /// per-row shape (an id, a real edition, a VCR row, both citations).</summary>
    [Fact]
    public void EveryBehavior_HasItsOwnRow_WithACompleteShape()
    {
        var behaviors = Enum.GetValues<DialectBehavior>();
        Assert.Equal(behaviors.Length, DialectBehaviors.All.Length);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var b in behaviors)
        {
            var row = DialectBehaviors.Of(b);
            Assert.Equal(b, row.Behavior);
            Assert.True(ids.Add(row.Id), $"duplicate DialectBehaviors id '{row.Id}'");
            Assert.Matches("^[a-z0-9]+(-[a-z0-9]+)*$", row.Id);
            Assert.Contains(row.ChangedIn, new[] { 2002, 2014, 2023 });
            Assert.True(row.VcrRow > 0, $"{b}: no VERSION_CHANGE_REFERENCE row");
            Assert.False(string.IsNullOrWhiteSpace(row.SpecRef), $"{b}: no normative clause");
            Assert.False(string.IsNullOrWhiteSpace(row.AnnexRef), $"{b}: no Annex E item");
            Assert.False(string.IsNullOrWhiteSpace(row.Title), $"{b}: no title");
        }
    }

    /// <summary>⛔ FORWARD: every registry row is CLAIMED by the ledger row it names — the anchor sits on that
    /// row number, the row's gating action is the behaviour-gating one, and it no longer reads
    /// <c>&lt;!-- todo --&gt;</c> (a landed gate that still counts as outstanding work is exactly the rot this
    /// pair of registers exists to prevent; <c>scripts/session-probe.ps1</c> counts those markers).</summary>
    [Fact]
    public void EveryBehavior_IsAnchoredOnItsVersionChangeReferenceRow()
    {
        string[] lines = Vcr();
        var anchors = Anchors(lines);
        var bad = new List<string>();
        foreach (var row in DialectBehaviors.All.ToArray())
        {
            string want = row.VcrRow.ToString();
            var hit = anchors.FirstOrDefault(a => a.Row == want && a.Id == row.Id);
            if (hit.Line is null)
            {
                bad.Add($"{row.Behavior}: VERSION_CHANGE_REFERENCE row {want} carries no <!-- behavior:{row.Id} -->");
                continue;
            }
            if (hit.Line.Contains("<!-- todo -->", StringComparison.Ordinal))
                bad.Add($"{row.Behavior}: row {want} is gated in code but still marked <!-- todo -->");
            if (!hit.Line.Contains("gate-behavior-by-dialect", StringComparison.Ordinal))
                bad.Add($"{row.Behavior}: row {want}'s gating action is not gate-behavior-by-dialect");
        }
        Assert.True(bad.Count == 0, string.Join("; ", bad));
    }

    /// <summary>⛔ BACKWARD: no anchor names a behaviour that does not exist. A marker that resolves to nothing
    /// reads as coverage and is not coverage (feedback_a_dead_lookup_is_also_unverified).</summary>
    [Fact]
    public void EveryBehaviorAnchor_ResolvesToARegistryRow()
    {
        var known = DialectBehaviors.All.ToArray().Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        var bad = Anchors(Vcr()).Where(a => !known.Contains(a.Id))
            .Select(a => $"row {a.Row}: behavior:{a.Id}").ToList();
        Assert.True(bad.Count == 0,
            "VCR behavior anchor(s) naming no DialectBehavior: " + string.Join(", ", bad));
    }

    /// <summary>The ONE predicate's contract: a behaviour is active from the edition that introduced it upward
    /// and inactive below it, at every edition this compiler serves (85 / 2002 / 2014 / 2023).</summary>
    [Fact]
    public void IsActive_IsTheEditionWindowTheRowDeclares()
    {
        foreach (var row in DialectBehaviors.All.ToArray())
            foreach (int edition in new[] { 85, 2002, 2014, 2023 })
                Assert.Equal(edition >= row.ChangedIn, DialectBehaviors.IsActive(row.Behavior, edition));
    }
}
