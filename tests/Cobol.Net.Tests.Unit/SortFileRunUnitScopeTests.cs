// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>kb/Work PB1570 — the sort-merge file stores belong to the RUN UNIT that executes the SORT/MERGE
/// statement (ISO §14.6.1: "A run unit is an independent entity that may be executed without communicating with, or
/// being coordinated with, any other run unit except that it may communicate via messages with other run units,
/// process files, and set and test switches" — a sort-merge file is none of those). The store used to be one
/// process-wide static <c>Dictionary</c>, so a second run unit — concurrent, or following an abandoned statement —
/// shared and mutated the first one's sort files, and EC-SORT-MERGE-ACTIVE (§14.9.40.4 GR10 / GR13, §14.9.24.4 GR8:
/// the procedures of "an executing SORT/MERGE statement") fired in one run unit because of another's procedure
/// phase. The two interleavings below are ordered by events, never by time.</summary>
public sealed class SortFileRunUnitScopeTests
{
    private static readonly CobolSort.Key[] OneCharKey = [new(0, 2, false, CobolSort.KeyClass.Alphanumeric, default)];

    private static List<string> Drain(string name)
    {
        var got = new List<string>();
        while (CobolSort.Return(name, out var image)) got.Add(image);
        return got;
    }

    /// <summary>Two run units sort a file of the SAME name at once, and a procedure phase of one is open while the
    /// other begins a statement with EC-SORT-MERGE-ACTIVE checking on: the second statement neither raises -ACTIVE
    /// nor sees the first's records, and the first sorts only its own.</summary>
    [Fact]
    public async Task ConcurrentRunUnits_DoNotShareSortFilesOrProcedurePhases()
    {
        const string sd = "PB1570-CONCURRENT";
        using var aInInput = new ManualResetEventSlim();
        using var bDone = new ManualResetEventSlim();
        string? bRaised = null;
        List<string>? bSorted = null;

        var a = Task.Run(() => RunUnit.Run(unit =>
        {
            CobolSort.Init(sd);
            CobolSort.EnterProcedure(sd, output: false);         // run unit A is inside an input procedure
            CobolSort.ReleaseStatement(sd, "A2", 0, 2);
            aInInput.Set();
            bDone.Wait();
            CobolSort.ReleaseStatement(sd, "A1", 0, 2);
            CobolSort.Sort(sd, OneCharKey, duplicatesInOrder: true);
            Assert.Equal(["A1", "A2"], Drain(sd));                // never B's "B1"/"B2"
            CobolSort.Close(sd);
        }));
        var b = Task.Run(() => RunUnit.Run(unit =>
        {
            aInInput.Wait();
            ExceptionState.SortMergeActiveChecking = true;
            try
            {
                CobolSort.Init(sd);                               // would be -ACTIVE if A's phase were visible
                CobolSort.EnterProcedure(sd, output: false);
                CobolSort.ReleaseStatement(sd, "B2", 0, 2);
                CobolSort.ReleaseStatement(sd, "B1", 0, 2);
                CobolSort.Sort(sd, OneCharKey, duplicatesInOrder: true);
                bSorted = Drain(sd);
                CobolSort.Close(sd);
            }
            catch (CobolFatalException e) { bRaised = e.EcName; }
            finally { bDone.Set(); }
        }));
        await Task.WhenAll(a, b);

        Assert.Null(bRaised);
        Assert.Equal(["B1", "B2"], bSorted);
    }

    /// <summary>A statement abandoned inside its input procedure (the run unit ended before CLOSE) leaves nothing
    /// behind: the NEXT run unit's SORT is not within the range of any executing input procedure (§14.9.40.4 GR10) and its
    /// RELEASE is not allowed outside one (§14.9.32.4 GR1) exactly as in a fresh process.</summary>
    [Fact]
    public void AbandonedStatement_DoesNotOutliveItsRunUnit()
    {
        const string sd = "PB1570-ABANDONED";
        RunUnit.Run(unit =>
        {
            CobolSort.Init(sd);
            CobolSort.EnterProcedure(sd, output: false);
            CobolSort.ReleaseStatement(sd, "X", 0, 1);
            // run unit ends with the statement still executing
        });
        RunUnit.Run(unit =>
        {
            ExceptionState.SortMergeActiveChecking = true;
            ExceptionState.FlowReleaseChecking = true;
            CobolSort.Init("PB1570-OTHER");                       // no -ACTIVE: the first run unit's phase is gone
            CobolSort.Close("PB1570-OTHER");
            Assert.Equal("EC-FLOW-RELEASE",
                Assert.Throws<CobolFatalException>(() => CobolSort.ReleaseStatement(sd, "Y", 0, 1)).EcName);
            Assert.False(CobolSort.Return(sd, out _));           // an empty store: the first run unit's "X" is gone
        });
    }
}
