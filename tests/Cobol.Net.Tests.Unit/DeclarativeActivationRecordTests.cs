// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1122 Task B — the DECLARATIVE ACTIVATION RECORD (ISO §14.6.13.1.2 1): a declarative does not complete
/// normally when "a RESUME … that is specified in this … program is executed, or a fatal exception occurs within the
/// scope of the declarative". The emitted <c>__RunUse</c> opens a record with <c>EnterDeclarative</c>, asks
/// <c>DeclarativeNotNormal</c> when the range falls off its end, and closes it in a <c>finally</c>; the fatal raise
/// funnel (<c>Set(…, fatal: true)</c>) and a RESUME that §14.9.33.4 GR1 made a CONTINUE
/// (<c>MarkDeclarativesNotNormal</c>) are the two marks. These drive the engine directly, because the golden
/// <c>2002/pb1122_declarative_fatal_in_scope_merge</c> can only show the consumer's end of it.
/// </summary>
public sealed class DeclarativeActivationRecordTests
{
    private static readonly object OwnerA = new();
    private static readonly object OwnerB = new();

    [Fact]
    public void FatalRaiseWithinTheScope_MarksTheOpenDeclarative()
    {
        var e = new ExceptionEngine();
        int tok = e.EnterDeclarative(OwnerA);
        Assert.False(e.DeclarativeNotNormal(tok));
        e.Set("EC-BOUND-SUBSCRIPT", fatal: true);
        Assert.True(e.DeclarativeNotNormal(tok));
    }

    [Fact]
    public void NonfatalRaise_DoesNotMark()
    {
        var e = new ExceptionEngine();
        int tok = e.EnterDeclarative(OwnerA);
        e.Set("EC-DATA-TRUNCATION", fatal: false);
        Assert.False(e.DeclarativeNotNormal(tok));
    }

    /// <summary>"Within the scope of the declarative" is dynamic: a fatal raised while a NESTED declarative runs is
    /// within the outer one's scope too, and it stays marked after the inner one leaves.</summary>
    [Fact]
    public void FatalRaisedInANestedDeclarative_MarksEveryOpenOne_AndSurvivesTheInnerLeaving()
    {
        var e = new ExceptionEngine();
        int outer = e.EnterDeclarative(OwnerA);
        int inner = e.EnterDeclarative(OwnerA);
        e.Set("EC-BOUND-SUBSCRIPT", fatal: true);
        Assert.True(e.DeclarativeNotNormal(inner));
        e.LeaveDeclarative(inner);
        Assert.True(e.DeclarativeNotNormal(outer));
        Assert.False(e.DeclarativeNotNormal(inner));   // a closed record answers false
    }

    /// <summary>The raise that SELECTS a declarative happens before it opens, so it must not mark its own record.</summary>
    [Fact]
    public void FatalRaisedBeforeTheDeclarativeOpens_DoesNotMarkIt()
    {
        var e = new ExceptionEngine();
        e.Set("EC-BOUND-SUBSCRIPT", fatal: true);
        int tok = e.EnterDeclarative(OwnerA);
        Assert.False(e.DeclarativeNotNormal(tok));
    }

    /// <summary>§14.6.13.1.2 1)'s RESUME is one "specified in this … program": a RESUME that GR1 made a CONTINUE marks
    /// only the open declaratives of ITS program instance, never another program's in the call chain.</summary>
    [Fact]
    public void ResumeContinue_MarksOnlyItsOwnPrograms()
    {
        var e = new ExceptionEngine();
        int a = e.EnterDeclarative(OwnerA);
        int b = e.EnterDeclarative(OwnerB);
        e.MarkDeclarativesNotNormal(OwnerA);
        Assert.True(e.DeclarativeNotNormal(a));
        Assert.False(e.DeclarativeNotNormal(b));
    }

    /// <summary>The −1 token of an invoker that is not running a declarative (an exception-checking PERFORM's WHEN
    /// handler shares <c>__RunUse</c>) opens, marks and closes nothing.</summary>
    [Fact]
    public void ANonDeclarativeToken_IsInert()
    {
        var e = new ExceptionEngine();
        int tok = e.EnterDeclarative(OwnerA);
        e.LeaveDeclarative(-1);
        Assert.False(e.DeclarativeNotNormal(-1));
        e.Set("EC-BOUND-SUBSCRIPT", fatal: true);
        Assert.False(e.DeclarativeNotNormal(-1));
        Assert.True(e.DeclarativeNotNormal(tok));   // and it did not close the real record
    }

    [Fact]
    public void LeavingAnOuterRecord_ClosesAnyRecordAboveIt()
    {
        var e = new ExceptionEngine();
        int outer = e.EnterDeclarative(OwnerA);
        int inner = e.EnterDeclarative(OwnerA);
        e.LeaveDeclarative(outer);
        e.Set("EC-BOUND-SUBSCRIPT", fatal: true);
        Assert.False(e.DeclarativeNotNormal(inner));
        Assert.False(e.DeclarativeNotNormal(outer));
    }

    /// <summary>A rule keyed on NORMAL completion tells <c>NotNormal</c> from <c>Normal</c>; every other consumer
    /// lets the statement finish and treats a handled warning alike (§14.9.49.4 GR13, GR7 b)/c), GR12 b)/c)).</summary>
    [Theory]
    [InlineData(DispatchResult.Normal, true)]
    [InlineData(DispatchResult.NotNormal, true)]
    [InlineData(DispatchResult.NoHandler, true)]
    [InlineData(DispatchResult.HandledNonfatal, false)]
    [InlineData(DispatchResult.ResumeNext, false)]
    [InlineData(5, false)]
    public void FinishesStatement_IsTrueUnlessAResumeRedirected(int result, bool finishes) =>
        Assert.Equal(finishes, DispatchResult.FinishesStatement(result));

    [Fact]
    public void ANotNormalHandler_OnASuccessfulStatement_IsAHandledWarning() =>
        Assert.Equal(DispatchResult.HandledNonfatal, DispatchResult.ForHandledWarning(DispatchResult.NotNormal));
}
