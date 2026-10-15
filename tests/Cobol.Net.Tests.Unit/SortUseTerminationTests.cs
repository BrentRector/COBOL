// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>kb/Work PB1177 — ISO §14.9.40.4 GR17: "If a USE procedure invoked while a format 1 SORT statement is active
/// does not complete normally, the SORT statement is terminated." The generated <c>__RunUse</c> hands every
/// declarative's action to <see cref="CobolSort.AfterUseProcedure"/>; these pin its answer per action, per scope and per
/// statement, independent of any compiled program (the end-to-end witness is
/// <c>conformance:2002/pb1177_sort_procedure_use_not_normal</c>).</summary>
public sealed class SortUseTerminationTests
{
    private const string Sd = "PB1177-SD";

    /// <summary>A RESUME AT NEXT STATEMENT, a not-normal fall-off and a RESUME AT procedure-name pc each terminate the
    /// SORT that registered the declarative's scope — in either procedure and between them (the implicit transfers
    /// run with no procedure phase) — and the signal carries the statement's file and the action to land.</summary>
    [Theory]
    [InlineData(DispatchResult.ResumeNext)]
    [InlineData(DispatchResult.NotNormal)]
    [InlineData(0)]
    [InlineData(42)]
    public void NotNormalCompletion_TerminatesTheRegisteringSort(int action) => RunUnit.Run(_ =>
    {
        object scope = new bool[2];
        CobolSort.Init(Sd, null, null, scope);
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

    /// <summary>A declarative that completed normally leaves the SORT alone: its action goes back to the raise site.</summary>
    [Fact]
    public void NormalCompletion_ReturnsTheAction() => RunUnit.Run(_ =>
    {
        object scope = new bool[1];
        CobolSort.Init(Sd, null, null, scope);
        CobolSort.EnterProcedure(Sd, output: false);
        Assert.Equal(DispatchResult.Normal, CobolSort.AfterUseProcedure(scope, DispatchResult.Normal));
        CobolSort.Close(Sd);
    });

    /// <summary>Only the registering scope's declaratives reach the rule: another program instance's (a CALLed
    /// program's own USE procedure), a MERGE's (its rules, §14.9.24.4, carry no such sentence, so it registers none) and a scope whose SORT
    /// has ended (no store, no active statement) all get their action back.</summary>
    [Fact]
    public void OtherScopes_MergesAndEndedSorts_AreNotTerminated() => RunUnit.Run(_ =>
    {
        object scope = new bool[1], callee = new bool[1];
        CobolSort.Init(Sd, null, null, scope);
        CobolSort.EnterProcedure(Sd, output: false);
        Assert.Equal(DispatchResult.ResumeNext, CobolSort.AfterUseProcedure(callee, DispatchResult.ResumeNext));
        CobolSort.Close(Sd);
        Assert.Equal(DispatchResult.ResumeNext, CobolSort.AfterUseProcedure(scope, DispatchResult.ResumeNext));

        CobolSort.Init(Sd);   // a MERGE's Init — no scope
        CobolSort.EnterProcedure(Sd, output: true);
        Assert.Equal(DispatchResult.NotNormal, CobolSort.AfterUseProcedure(scope, DispatchResult.NotNormal));
        CobolSort.Close(Sd);
    });
}
