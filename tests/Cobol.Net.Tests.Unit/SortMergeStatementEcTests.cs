// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.CodeGen;
using CobolNet.Runtime.Exceptions;
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>kb/Work PB1036 — the EC-SORT-MERGE statement conditions at their <see cref="CobolSort"/> raise sites,
/// the runtime half the golden <c>2002/pb1036_sort_merge_statement_ec</c> cannot isolate: which condition wins when
/// two are detected by one statement (docs/CONFORMANCE.md §3 D-SMA), that a raise leaves the statement's action
/// undone, and that checking off keeps the pre-PB1036 behaviour byte for byte. Each test uses its own sort-merge
/// file name — the store is keyed by name — and restores every flag it sets.</summary>
[Collection("process-globals")]   // CobolSort's file-name store is one static Dictionary: a class that Init/Close-es a sort file while another's RETURN enumerates it throws "Collection was modified" (wave 1021 gate)
public sealed class SortMergeStatementEcTests
{
    private static readonly CobolSort.Key[] OneCharKey = [new(0, 1, false, CobolSort.KeyClass.Alphanumeric, default)];

    private static string RaisedBy(Action action) => Assert.Throws<CobolFatalException>(action).EcName;

    private static List<string> Drain(string name)
    {
        var got = new List<string>();
        while (CobolSort.Return(name, out var image)) got.Add(image);
        return got;
    }

    /// <summary>A RETURN in an executing input procedure is detected by §14.9.40.4 GR10 (-ACTIVE) and by
    /// §14.9.34.4 GR1 (EC-FLOW-RETURN, "any other time"): -ACTIVE is raised when it is enabled, EC-FLOW-RETURN
    /// only when -ACTIVE is not.</summary>
    [Fact]
    public void Return_InAnInputProcedure_RaisesActive_BeforeFlowReturn()
    {
        const string sd = "PB1036-UNIT-RET";
        CobolSort.Init(sd);
        CobolSort.EnterProcedure(sd, output: false);
        try
        {
            ExceptionState.FlowReturnChecking = true;
            ExceptionState.SortMergeActiveChecking = true;
            Assert.Equal("EC-SORT-MERGE-ACTIVE", RaisedBy(() => CobolSort.ReturnStatement(sd, out _)));
            ExceptionState.SortMergeActiveChecking = false;
            Assert.Equal("EC-FLOW-RETURN", RaisedBy(() => CobolSort.ReturnStatement(sd, out _)));
        }
        finally
        {
            ExceptionState.FlowReturnChecking = false;
            ExceptionState.SortMergeActiveChecking = false;
            CobolSort.Close(sd);
        }
    }

    /// <summary>The RELEASE twin: §14.9.40.4 GR13 (-ACTIVE) before §14.9.32.4 GR1 (EC-FLOW-RELEASE), and the raise
    /// releases nothing.</summary>
    [Fact]
    public void Release_InAnOutputProcedure_RaisesActive_BeforeFlowRelease_AndReleasesNothing()
    {
        const string sd = "PB1036-UNIT-REL";
        CobolSort.Init(sd);
        CobolSort.Sort(sd, OneCharKey, duplicatesInOrder: false);
        CobolSort.EnterProcedure(sd, output: true);
        try
        {
            ExceptionState.FlowReleaseChecking = true;
            ExceptionState.SortMergeActiveChecking = true;
            Assert.Equal("EC-SORT-MERGE-ACTIVE", RaisedBy(() => CobolSort.ReleaseStatement(sd, "X", 0, 1)));
            ExceptionState.SortMergeActiveChecking = false;
            Assert.Equal("EC-FLOW-RELEASE", RaisedBy(() => CobolSort.ReleaseStatement(sd, "X", 0, 1)));
            CobolSort.Rewind(sd);
            Assert.Empty(Drain(sd));
        }
        finally
        {
            ExceptionState.FlowReleaseChecking = false;
            ExceptionState.SortMergeActiveChecking = false;
            CobolSort.Close(sd);
        }
    }

    /// <summary>§13.18.43.4 GR14 b): a size outside integer-2..integer-3 raises -RELEASE and "the execution of the
    /// RELEASE statement is unsuccessful". With checking off the record releases at the DEPENDING ON size exactly
    /// as the lenient reference modification <c>(1:size)</c> sliced it before PB1036.</summary>
    [Fact]
    public void ReleaseStatement_SizeOutsideTheRange_IsUnsuccessful_AndLenientWhenUnchecked()
    {
        const string sd = "PB1036-UNIT-SIZE";
        CobolSort.Init(sd);
        CobolSort.EnterProcedure(sd, output: false);
        try
        {
            ExceptionState.SortMergeReleaseChecking = true;
            Assert.Equal("EC-SORT-MERGE-RELEASE", RaisedBy(() => CobolSort.ReleaseStatement(sd, "AAAAA", 2, 5, size: 9)));
            Assert.Equal("EC-SORT-MERGE-RELEASE", RaisedBy(() => CobolSort.ReleaseStatement(sd, "AAAAA", 2, 5, size: 1)));
            CobolSort.ReleaseStatement(sd, "BBBBB", 2, 5, size: 3);          // in range: released at 3
            ExceptionState.SortMergeReleaseChecking = false;
            CobolSort.ReleaseStatement(sd, "CCCCC", 2, 5, size: 7);          // unchecked: space-extended to 7
            CobolSort.ReleaseStatement(sd, "DDDDD", 2, 5, size: -1);         // unchecked: a negative size is the whole area
            CobolSort.Sort(sd, OneCharKey, duplicatesInOrder: false);
            Assert.Equal(["BBB", "CCCCC  ", "DDDDD"], Drain(sd));
        }
        finally
        {
            ExceptionState.SortMergeReleaseChecking = false;
            CobolSort.Close(sd);
        }
    }

    /// <summary>§14.9.40.4 GR12 b): the implicit USING release tests the size the record had when READ — larger
    /// than the largest record, or (variable-length SD) smaller than the smallest.</summary>
    [Fact]
    public void ImplicitRelease_TestsTheReadSize()
    {
        const string sd = "PB1036-UNIT-IMPL";
        CobolSort.Init(sd);
        try
        {
            ExceptionState.SortMergeReleaseChecking = true;
            Assert.Equal("EC-SORT-MERGE-RELEASE", RaisedBy(() => CobolSort.Release(sd, "ABC", readSize: 5, min: 0, max: 3)));
            Assert.Equal("EC-SORT-MERGE-RELEASE", RaisedBy(() => CobolSort.Release(sd, "A", readSize: 1, min: 2, max: 3)));
            CobolSort.Release(sd, "ABC", readSize: 3, min: 0, max: 3);
            ExceptionState.SortMergeReleaseChecking = false;
            CobolSort.Release(sd, "XYZ", readSize: 5, min: 0, max: 3);        // unchecked: accepted as shaped
            CobolSort.Sort(sd, OneCharKey, duplicatesInOrder: false);
            Assert.Equal(["ABC", "XYZ"], Drain(sd));
        }
        finally
        {
            ExceptionState.SortMergeReleaseChecking = false;
            CobolSort.Close(sd);
        }
    }

    /// <summary>§14.9.40.4 GR10: a SORT/MERGE executed while a procedure of an executing statement runs raises
    /// -ACTIVE BEFORE it touches a store, so an enclosing statement on the SAME file keeps its records.</summary>
    [Fact]
    public void Init_WhileAProcedureRuns_RaisesActive_AndLeavesTheEnclosingStoreIntact()
    {
        const string sd = "PB1036-UNIT-ACT";
        CobolSort.Init(sd);
        CobolSort.EnterProcedure(sd, output: false);
        CobolSort.ReleaseStatement(sd, "Q", 0, 1);
        try
        {
            ExceptionState.SortMergeActiveChecking = true;
            Assert.Equal("EC-SORT-MERGE-ACTIVE", RaisedBy(() => CobolSort.Init(sd)));
            ExceptionState.SortMergeActiveChecking = false;
            CobolSort.Sort(sd, OneCharKey, duplicatesInOrder: false);
            Assert.Equal(["Q"], Drain(sd));
        }
        finally
        {
            ExceptionState.SortMergeActiveChecking = false;
            CobolSort.Close(sd);
        }
    }

    /// <summary>§14.9.24.4 GR6: a USING stream out of KEY order raises -SEQUENCE; the scan runs only while the
    /// condition is enabled, and an ordered input raises nothing.</summary>
    [Fact]
    public void Merge_AnOutOfOrderStream_RaisesSequence_OnlyWhenChecked()
    {
        const string sd = "PB1036-UNIT-SEQ";
        static void Load(string name, params string[][] streams)
        {
            CobolSort.Init(name);
            foreach (var stream in streams)
            {
                CobolSort.NextInput(name);
                foreach (var r in stream) CobolSort.Release(name, r, r.Length, 0, 1);
            }
        }
        try
        {
            ExceptionState.SortMergeSequenceChecking = true;
            Load(sd, ["A", "C"], ["B"]);
            CobolSort.Merge(sd, OneCharKey);                                   // ordered: no raise
            Assert.Equal(["A", "B", "C"], Drain(sd));
            Load(sd, ["A", "C"], ["D", "B"]);
            Assert.Equal("EC-SORT-MERGE-SEQUENCE", RaisedBy(() => CobolSort.Merge(sd, OneCharKey)));
            ExceptionState.SortMergeSequenceChecking = false;
            Load(sd, ["A", "C"], ["D", "B"]);
            CobolSort.Merge(sd, OneCharKey);                                   // unchecked: undefined, no raise
            Assert.Equal(4, Drain(sd).Count);
        }
        finally
        {
            ExceptionState.SortMergeSequenceChecking = false;
            CobolSort.Close(sd);
        }
    }

    /// <summary>⛔ DRIFT: every Table 13 EC-SORT-MERGE-* name the implementation raises itself has a FLAGGED fatal
    /// gate row, so <c>&gt;&gt;TURN … CHECKING ON</c> arms a raise site — the family sat unwired behind a green
    /// suite until PB1036 (the general registry is kb/Work PB1037). -IMP is the implementor's own name, which
    /// WiseOwl COBOL never raises (docs/CONFORMANCE.md §7, DOC-A.1-100). A new EC-SORT-MERGE-* row in the catalog
    /// fails here until it is wired, and <c>ExceptionRaiseHelperDriftTests</c> then pins its helper to the flag.</summary>
    [Fact]
    public void EverySortMergeCondition_HasAFlaggedFatalGate()
    {
        var gates = EcEmitter.FatalAmbientGates.ToDictionary(g => g.Ec, g => g.Flag);
        var family = ExceptionCatalog.Level3Rows
            .Where(r => ExceptionCatalog.UnderLevel2(r.Name, "EC-SORT-MERGE") && r.Name != "EC-SORT-MERGE-IMP")
            .Select(r => r.Name).ToList();
        Assert.Equal(5, family.Count);   // ACTIVE, FILE-OPEN, RELEASE, RETURN, SEQUENCE — Table 13's five
        foreach (var name in family)
            Assert.True(gates.TryGetValue(name, out var flag) && flag is not null, $"{name} has no flagged fatal gate row");
    }

    /// <summary>§14.6.13.1.3 2) keys on the statement that EXECUTED: a condition a SORT/MERGE statement entry raises
    /// carries <see cref="CobolFatalException.RaisedBySortMerge"/> — the mark the SORT/MERGE guard disposes of by
    /// the verb — and the same name raised by the RELEASE statement does not; only SORT and MERGE guards consult it.</summary>
    [Fact]
    public void TheStatementsOwnRaise_IsMarked_TheReleaseStatementsIsNot()
    {
        const string sd = "PB1036-UNIT-MARK";
        CobolSort.Init(sd);
        CobolSort.Sort(sd, OneCharKey, duplicatesInOrder: false);
        CobolSort.EnterProcedure(sd, output: true);
        try
        {
            ExceptionState.SortMergeActiveChecking = true;
            Assert.True(Assert.Throws<CobolFatalException>(() => CobolSort.Init("PB1036-UNIT-MARK-2")).RaisedBySortMerge);
            Assert.False(Assert.Throws<CobolFatalException>(() => CobolSort.ReleaseStatement(sd, "X", 0, 1)).RaisedBySortMerge);
        }
        finally
        {
            ExceptionState.SortMergeActiveChecking = false;
            CobolSort.Close(sd);
        }
        static BoundStatement Node(Type t) => (BoundStatement)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(t);
        Assert.True(EcEmitter.VerbDisposes(Node(typeof(BoundSort))));
        Assert.True(EcEmitter.VerbDisposes(Node(typeof(BoundMerge))));
        Assert.False(EcEmitter.VerbDisposes(Node(typeof(BoundRelease))));
    }

    /// <summary>§14.9.40.4 GR9 / §14.9.24.4 GR7, GR12 raise -FILE-OPEN only for a file "in an open mode": a
    /// connector that is not open — here one never even registered — raises nothing although checking is on. The
    /// raising arm needs a live connector and is witnessed end to end by the golden's cases A and B.</summary>
    [Fact]
    public void FileNotOpen_AConnectorThatIsNotOpen_RaisesNothing()
    {
        try
        {
            ExceptionState.SortMergeFileOpenChecking = true;
            CobolSort.FileNotOpen("PB1036-UNIT-SD", "PB1036-UNIT-NEVER-REGISTERED");
        }
        finally { ExceptionState.SortMergeFileOpenChecking = false; }
    }
}
