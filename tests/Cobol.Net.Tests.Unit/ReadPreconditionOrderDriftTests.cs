// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE READ PRECONDITIONS ARE WRITTEN DOWN ONCE, AND IN THE STANDARD'S OWN ORDER (kb/Work PB336).
/// </summary>
/// <remarks>
/// <para>
/// ISO §14.9.30.4 GR21's first sentence — "For a sequential READ statement, if the previous READ or START
/// statement for the file connector was unsuccessful, then the READ statement is unsuccessful and the I-O status
/// is set to '46'" — is what makes §9.1.13.4 item 1 c)'s '10' apply only to a sequential READ "attempted for the
/// FIRST TIME on a file described as optional and the physical file is not present". The absent-OPTIONAL arm is
/// itself an unsuccessful READ, so it arms GR21 for its own successor: test the arms in the other order and the
/// '46' can never be reached, which is exactly what happened. All three connectors carried a private copy of the
/// chain and all three had it backwards, so a per-connector patch looked sufficient twice over.
/// </para>
/// <para>
/// The fix was the extraction — <c>FileConnector.SequentialReadGuard</c> / <c>ReadOpenModeGuard</c> /
/// <c>RandomReadAbsentOptionalGuard</c> — and this gate is what keeps it true: a fourth organization, or an edit
/// that "just inlines the check here", re-opens the defect silently otherwise. The behaviour itself is pinned by
/// the corpus goldens <c>conformance-suite/cases/9.1.11-File-position-indicator/pb336_optional_absent_read_46.cob</c> and its 1985 twin, which
/// walk the two-READ shape on ALL THREE organizations in one program; this file pins the STRUCTURE, because the
/// uniformity across connectors is what made the defect reproduce three times.
/// </para>
/// <para>
/// ⛔ <see cref="TheOrderCheck_ActuallyFails_OnAConnectorWithTheGuardsSwapped"/> drives the same pure predicates
/// with sources built to break them AND with sources built to pass them
/// (<c>feedback_green_gates_arent_evidence</c>): a green structural gate that never looked at anything is
/// indistinguishable from one that works.
/// </para>
/// </remarks>
public sealed class ReadPreconditionOrderDriftTests
{
    /// <summary>The organizations whose sequential READ entry must go through the shared guard.</summary>
    private static readonly string[] Connectors =
        ["SequentialConnector.cs", "RelativeConnector.cs", "IndexedConnector.cs"];

    private static string ConnectorSource(string file) =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Runtime", "IO", file));

    // ── the pure predicates (driven by the real sources below, and by fabricated ones in the failure proof) ──

    /// <summary>True when <paramref name="source"/> tests GR21's '46' poison BEFORE the absent-OPTIONAL '10'
    /// arm, inside the shared sequential guard. Index order IS the rule here: the second arm assigns the poison
    /// the first arm reads, so whichever is written first wins for the life of the open mode.</summary>
    internal static bool PoisonTestedBeforeAbsentOptional(string source)
    {
        int guard = source.IndexOf("SequentialReadGuard()", StringComparison.Ordinal);
        if (guard < 0) return false;
        int body = source.IndexOf('{', guard);
        int end = source.IndexOf("RandomReadAbsentOptionalGuard", guard, StringComparison.Ordinal);
        if (body < 0 || end < 0) return false;
        string chain = source[body..end];
        int poison = chain.IndexOf("if (LastReadUnsuccessful)", StringComparison.Ordinal);
        int absent = chain.IndexOf("if (OptionalAbsent)", StringComparison.Ordinal);
        return poison >= 0 && absent >= 0 && poison < absent;
    }

    /// <summary>True when <paramref name="source"/> carries a LOCAL copy of the absent-OPTIONAL READ arm — an
    /// <c>OptionalAbsent</c> test that produces a READ status ('10' AtEnd or '23' RecordNotFound) rather than
    /// delegating. START's own absent-optional rule (§14.9.41 GR5, <c>StartFail()</c>) is a different rule of a
    /// different statement and is deliberately NOT matched.</summary>
    internal static bool HasLocalAbsentOptionalReadArm(string source) =>
        source.Split('\n').Any(line =>
            line.Contains("OptionalAbsent", StringComparison.Ordinal)
            && (line.Contains("FileStatusCode.AtEnd", StringComparison.Ordinal)
                || line.Contains("FileStatusCode.RecordNotFound", StringComparison.Ordinal)));

    // ── the gates ────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>§14.9.30.4 GR21 before §9.1.13.4 item 1 c), in the one place the pair is written down.</summary>
    [Fact]
    public void SharedSequentialGuard_TestsGr21Poison_BeforeTheAbsentOptionalArm()
    {
        string shared = ConnectorSource("FileConnector.cs");
        Assert.True(PoisonTestedBeforeAbsentOptional(shared),
            "FileConnector.SequentialReadGuard must test LastReadUnsuccessful (§14.9.30.4 GR21 → '46') BEFORE "
            + "OptionalAbsent (§9.1.13.4 item 1 c) → '10'). Written the other way the '10' arm arms a poison "
            + "nothing can ever reach, and every READ after the first on an absent OPTIONAL file re-reports '10'.");
    }

    /// <summary>No connector re-implements the READ preconditions locally — one rule, one place.</summary>
    [Fact]
    public void EveryConnectorSequentialRead_DelegatesToTheSharedGuard()
    {
        foreach (string file in Connectors)
        {
            string src = ConnectorSource(file);
            Assert.True(src.Contains("SequentialReadGuard()", StringComparison.Ordinal),
                $"{file}'s sequential READ must call FileConnector.SequentialReadGuard() — a private copy of the "
                + "'47'/'46'/'10' chain is how kb/Work PB336 came to exist in three connectors at once.");
            Assert.False(HasLocalAbsentOptionalReadArm(src),
                $"{file} carries a local OptionalAbsent → READ-status arm. §9.1.13.4 item 1 c) and §9.1.13.5 "
                + "item 3 b) belong to FileConnector.SequentialReadGuard / RandomReadAbsentOptionalGuard.");
        }
    }

    /// <summary>The shared guard is the only place the two absent-OPTIONAL READ statuses are produced.</summary>
    [Fact]
    public void TheAbsentOptionalReadArms_LiveOnlyInFileConnector()
    {
        string shared = ConnectorSource("FileConnector.cs");
        Assert.True(HasLocalAbsentOptionalReadArm(shared),
            "FileConnector must OWN the absent-OPTIONAL READ arms — if this fails the rule moved and the sweep "
            + "above is measuring nothing (feedback_measure_the_selectors_complement).");
    }

    /// <summary>⛔ The failure proof: both predicates must DISCRIMINATE, not merely return a constant.</summary>
    [Fact]
    public void TheOrderCheck_ActuallyFails_OnAConnectorWithTheGuardsSwapped()
    {
        const string good = """
            protected string? SequentialReadGuard()
            {
                if (ReadOpenModeGuard() is { } notOpen) return notOpen;
                if (LastReadUnsuccessful) return FileStatusCode.NoValidNextRecord;
                if (OptionalAbsent) { LastReadUnsuccessful = true; return FileStatusCode.AtEnd; }
                return null;
            }
            protected string? RandomReadAbsentOptionalGuard() => null;
            """;
        // The PB336 defect, verbatim in shape: the '10' arm ahead of the '46' guard it arms.
        string swapped = """
            protected string? SequentialReadGuard()
            {
                if (ReadOpenModeGuard() is { } notOpen) return notOpen;
                if (OptionalAbsent) { LastReadUnsuccessful = true; return FileStatusCode.AtEnd; }
                if (LastReadUnsuccessful) return FileStatusCode.NoValidNextRecord;
                return null;
            }
            protected string? RandomReadAbsentOptionalGuard() => null;
            """;
        Assert.True(PoisonTestedBeforeAbsentOptional(good));
        Assert.False(PoisonTestedBeforeAbsentOptional(swapped));
        Assert.False(PoisonTestedBeforeAbsentOptional("no guard here at all"));

        // …and the local-copy sweep must see a re-inlined arm while ignoring START's own absent-optional rule.
        Assert.True(HasLocalAbsentOptionalReadArm(
            "if (OptionalAbsent) { LastReadUnsuccessful = true; return Status = FileStatusCode.AtEnd; }"));
        Assert.True(HasLocalAbsentOptionalReadArm(
            "if (OptionalAbsent) { LastReadUnsuccessful = true; return Status = FileStatusCode.RecordNotFound; }"));
        Assert.False(HasLocalAbsentOptionalReadArm("if (OptionalAbsent) return StartFail();   // '23' §14.9.41 GR5"));
        Assert.False(HasLocalAbsentOptionalReadArm("if (SequentialReadGuard() is { } pre) return Status = pre;"));
    }
}
