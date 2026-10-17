// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>kb/Work PB1177 — ISO §14.9.40.4 GR17: "If a USE procedure invoked while a format 1 SORT statement is active
/// does not complete normally, the SORT statement is terminated." Every format 1 SORT registers itself
/// (<see cref="CobolSort.RegisterFormat1Sort"/>) and the generated <c>__RunUse</c> of every EC-model element hands each
/// declarative's action to <see cref="CobolSort.AfterUseProcedure"/>; these pin its answer per action, per scope and per
/// statement, independent of any compiled program (the end-to-end witnesses are
/// <c>conformance:cases/14.9.40-SORT/pb1177_sort_procedure_use_not_normal</c> and <c>conformance:cases/14.9.40-SORT/pb1177_sort_called_use_not_normal</c>).</summary>
public sealed class SortUseTerminationTests
{
    private const string Sd = "PB1177-SD";

    /// <summary>A RESUME AT NEXT STATEMENT, a not-normal fall-off and a RESUME AT procedure-name pc of the SORT's OWN
    /// declarative each terminate it — in either procedure and between them (the implicit transfers run with no
    /// procedure phase) — and the signal carries the statement's file and the action to land.</summary>
    [Theory]
    [InlineData(DispatchResult.ResumeNext)]
    [InlineData(DispatchResult.NotNormal)]
    [InlineData(0)]
    [InlineData(42)]
    public void OwnDeclarative_NotNormal_TerminatesTheSortWithItsAction(int action) => RunUnit.Run(_ =>
    {
        object scope = new bool[2];
        CobolSort.Init(Sd);
        CobolSort.RegisterFormat1Sort(Sd, scope);
        try
        {
            var implicitPhase = Assert.Throws<SortTerminationSignal>(() => CobolSort.AfterUseProcedure(scope, action));
            Assert.Equal((Sd, action), (implicitPhase.File, implicitPhase.Action));
            CobolSort.EnterProcedure(Sd, output: false);
            Assert.Throws<SortTerminationSignal>(() => CobolSort.AfterUseProcedure(scope, action));
            CobolSort.EnterProcedure(Sd, output: true);
            Assert.Throws<SortTerminationSignal>(() => CobolSort.AfterUseProcedure(scope, action));
        }
        finally { CobolSort.Close(Sd); }
    });

    /// <summary>ANOTHER element's USE procedure — a CALLed program's, an INVOKEd method's, a containing program's GLOBAL
    /// one — that does not complete normally terminates the active SORT too (the rule names every USE procedure invoked
    /// while the SORT is active, and GR10/GR13 put the CALLed element in the procedure's range), whether or not the
    /// SORT's own scope has declaratives. It lands as a not-normal completion at the statement's end: a RESUME AT
    /// procedure-name of that element names a procedure of an activation the termination has ended.</summary>
    [Theory]
    [InlineData(DispatchResult.ResumeNext, true)]
    [InlineData(DispatchResult.NotNormal, true)]
    [InlineData(42, true)]
    [InlineData(42, false)]
    public void AnotherElementsDeclarative_NotNormal_TerminatesTheSortAtItsEnd(int action, bool sortHasDeclaratives) =>
        RunUnit.Run(_ =>
        {
            object callee = new bool[1];
            CobolSort.Init(Sd);
            CobolSort.RegisterFormat1Sort(Sd, sortHasDeclaratives ? new bool[1] : null);
            CobolSort.EnterProcedure(Sd, output: false);
            try
            {
                var signal = Assert.Throws<SortTerminationSignal>(() => CobolSort.AfterUseProcedure(callee, action));
                Assert.Equal((Sd, DispatchResult.NotNormal), (signal.File, signal.Action));
            }
            finally { CobolSort.Close(Sd); }
        });

    /// <summary>A declarative that completed normally leaves the SORT alone, its own or another element's: the action
    /// goes back to the raise site.</summary>
    [Fact]
    public void NormalCompletion_ReturnsTheAction() => RunUnit.Run(_ =>
    {
        object scope = new bool[1], callee = new bool[1];
        CobolSort.Init(Sd);
        CobolSort.RegisterFormat1Sort(Sd, scope);
        CobolSort.EnterProcedure(Sd, output: false);
        Assert.Equal(DispatchResult.Normal, CobolSort.AfterUseProcedure(scope, DispatchResult.Normal));
        Assert.Equal(DispatchResult.Normal, CobolSort.AfterUseProcedure(callee, DispatchResult.Normal));
        CobolSort.Close(Sd);
    });

    /// <summary>With no format 1 SORT active the rule has nothing to terminate: a SORT that has ended (no store) and a
    /// MERGE (its rules, §14.9.24.4, carry no such sentence, so it registers none) both give the action back. A store
    /// reused by the next statement on the file starts unregistered (<c>Init</c> clears the registration).</summary>
    [Fact]
    public void EndedSortsAndMerges_AreNotTerminated() => RunUnit.Run(_ =>
    {
        object scope = new bool[1];
        CobolSort.Init(Sd);
        CobolSort.RegisterFormat1Sort(Sd, scope);
        CobolSort.Close(Sd);
        Assert.Equal(DispatchResult.ResumeNext, CobolSort.AfterUseProcedure(scope, DispatchResult.ResumeNext));

        CobolSort.Init(Sd);
        CobolSort.RegisterFormat1Sort(Sd, scope);
        CobolSort.Init(Sd);   // the next statement on the same file, a MERGE: it registers nothing, and Init cleared the last
        CobolSort.EnterProcedure(Sd, output: true);
        Assert.Equal(DispatchResult.NotNormal, CobolSort.AfterUseProcedure(scope, DispatchResult.NotNormal));
        CobolSort.Close(Sd);
    });

    /// <summary>An EXIT PROGRAM or GOBACK in a USE procedure leaves it by the element's return signal, which the
    /// generated <c>__RunUse</c> notes with <see cref="CobolSort.UseProcedureExited"/> (a declining filter, always
    /// false). Another element's: the active SORT is terminated once that activation has returned, exactly once, as a
    /// not-normal completion. The SORT's own: its return leaves the SORT statement through its <c>finally</c>, so
    /// nothing is recorded. A SORT that ends first takes its pending termination with it.</summary>
    [Fact]
    public void ExitFromAUseProcedure_TerminatesAnotherElementsSortAtTheActivationsReturn() => RunUnit.Run(_ =>
    {
        object scope = new bool[1], callee = new bool[1];
        CobolSort.Init(Sd);
        CobolSort.RegisterFormat1Sort(Sd, scope);
        CobolSort.EnterProcedure(Sd, output: false);
        try
        {
            Assert.False(CobolSort.UseProcedureExited(scope));
            CobolSort.AfterActivationReturn();   // the SORT's own: nothing pending

            Assert.False(CobolSort.UseProcedureExited(callee));
            var signal = Assert.Throws<SortTerminationSignal>(CobolSort.AfterActivationReturn);
            Assert.Equal((Sd, DispatchResult.NotNormal), (signal.File, signal.Action));
            CobolSort.AfterActivationReturn();   // taken once

            Assert.False(CobolSort.UseProcedureExited(callee));
        }
        finally { CobolSort.Close(Sd); }
        CobolSort.AfterActivationReturn();   // the SORT ended: its pending termination went with it
        Assert.False(CobolSort.UseProcedureExited(callee));   // and with no SORT active nothing is recorded
        CobolSort.AfterActivationReturn();
    });

    /// <summary>A SORT executed within another's procedure (EC-SORT-MERGE-ACTIVE, undefined when unchecked — GR10/GR13):
    /// the INNERMOST active SORT is the one terminated, and once it has ended the outer one is active again.</summary>
    [Fact]
    public void NestedSorts_TheInnermostIsTerminated() => RunUnit.Run(_ =>
    {
        const string inner = "PB1177-INNER";
        object scope = new bool[1];
        CobolSort.Init(Sd);
        CobolSort.RegisterFormat1Sort(Sd, scope);
        CobolSort.Init(inner);
        CobolSort.RegisterFormat1Sort(inner, scope);
        Assert.Equal(inner, Assert.Throws<SortTerminationSignal>(() => CobolSort.AfterUseProcedure(scope, DispatchResult.NotNormal)).File);
        CobolSort.Close(inner);
        Assert.Equal(Sd, Assert.Throws<SortTerminationSignal>(() => CobolSort.AfterUseProcedure(scope, DispatchResult.NotNormal)).File);
        CobolSort.Close(Sd);
    });
}
