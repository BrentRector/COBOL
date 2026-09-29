// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Editions;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using Xunit;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1004 — ISO §14.9.28.4 GR14: "An implicit PUSH ALL followed by TURN OFF ALL is assumed at the end of
/// imperative-statement-1. Immediately preceding the END PERFORM phrase, there is an implicit POP ALL …". The two
/// implicit ops go through the ONE directive-state stack with the written ones, so EVERY line-scoped directive
/// state (§7.3.22.4 GR2: "the state of all of the directives other than EVALUATE, IF, PAGE, POP, or PUSH") written
/// in a handler is gone after END-PERFORM, and one written in imperative-statement-1 — before the PUSH — survives.
/// </summary>
public sealed class ExceptionPerformDirectiveScopeTests
{
    [Fact]
    public void RefModZeroLength_WrittenInAWhenPhrase_IsRestoredAtEndPerform()
    {
        var d = Bound("""
                       PERFORM
                           DISPLAY "IMP1"
                       WHEN EC-SIZE
                   >>REF-MOD-ZERO-LENGTH ON
                           DISPLAY "HANDLER"
                       END-PERFORM
                       DISPLAY "AFTER"
            """);
        var s = RefModZeroLengthState.Build(d.RefModZeroLengthEvents);
        Assert.True(s.IsOnAt(Line("HANDLER")));        // the control: the directive is in force in the handler
        Assert.False(s.IsOnAt(Line("AFTER")));         // GR14's implicit POP ALL restored the omitted state
        Assert.True(s.IsUnspecifiedAt(Line("AFTER"))); // … "not specified", the FLAG-14 i tri-state
    }

    [Fact]
    public void Directive_WrittenInImperativeStatement1_PrecedesThePushAndSurvives()
    {
        var d = Bound("""
                       PERFORM
                   >>REF-MOD-ZERO-LENGTH ON
                           DISPLAY "IMP1"
                       WHEN EC-SIZE
                           DISPLAY "HANDLER"
                       END-PERFORM
                       DISPLAY "AFTER"
            """);
        Assert.True(RefModZeroLengthState.Build(d.RefModZeroLengthEvents).IsOnAt(Line("AFTER")));
    }

    [Theory]
    [InlineData("FLAG-02", FlagOption.Flag02EcProgramExceptions)]
    [InlineData("FLAG-14", FlagOption.Flag14ReadPrevious)]
    public void Flag_WrittenInAFinallyPhrase_IsRestoredAtEndPerform(string directive, FlagOption option)
    {
        string word = FlagOptions.Info(option).Word;
        var d = Bound($"""
                       PERFORM
                           DISPLAY "IMP1"
                       WHEN EC-SIZE
                           DISPLAY "HANDLER"
                       FINALLY
                   >>{directive} {word} ON
                           DISPLAY "FINAL"
                       END-PERFORM
                       DISPLAY "AFTER"
            """);
        var s = FlagState.Build(d.FlagEvents);
        Assert.True(s.IsOnAt(Line("FINAL"), option));
        Assert.False(s.IsOnAt(Line("AFTER"), option));
    }

    [Fact]
    public void Turn_WrittenInAWhenPhrase_IsRestoredAtEndPerform()
    {
        var d = Bound("""
                       PERFORM
                           DISPLAY "IMP1"
                       WHEN EC-SIZE
                   >>TURN EC-SIZE CHECKING ON
                           DISPLAY "HANDLER"
                       END-PERFORM
                       DISPLAY "AFTER"
            """);
        var turn = TurnState.Build(d.TurnEvents, new EditionContext(2023));
        Assert.True(turn.Enabled("EC-SIZE-OVERFLOW", null, Line("HANDLER")));
        Assert.False(turn.Enabled("EC-SIZE-OVERFLOW", null, Line("AFTER")));
    }

    [Fact]
    public void NestedPerforms_EachPopRestoresItsOwnPush()
    {
        // The inner PERFORM is the outer one's HANDLER body: its handler's directive is revoked at the INNER
        // END-PERFORM; the outer handler's directive, written after it, lives until the OUTER END-PERFORM.
        var d = Bound("""
                       PERFORM
                           DISPLAY "IMP1"
                       WHEN EC-SIZE
                           PERFORM
                               DISPLAY "INNER-IMP1"
                           WHEN EC-SIZE
                   >>FLAG-14 READ-PREVIOUS ON
                               DISPLAY "INNER-HANDLER"
                           END-PERFORM
                           DISPLAY "BETWEEN"
                   >>REF-MOD-ZERO-LENGTH ON
                           DISPLAY "OUTER-HANDLER"
                       END-PERFORM
                       DISPLAY "AFTER"
            """);
        var flag = FlagState.Build(d.FlagEvents);
        var rm = RefModZeroLengthState.Build(d.RefModZeroLengthEvents);
        Assert.True(flag.IsOnAt(Line("INNER-HANDLER"), FlagOption.Flag14ReadPrevious));
        Assert.False(flag.IsOnAt(Line("BETWEEN"), FlagOption.Flag14ReadPrevious));
        Assert.True(rm.IsOnAt(Line("OUTER-HANDLER")));
        Assert.False(rm.IsOnAt(Line("AFTER")));
    }

    [Fact]
    public void ExplicitPushPop_AroundThePerform_StillPair()
    {
        // The implicit ops are balanced inside the statement, so a written PUSH before it and POP after it still
        // pair with each other (§7.3.20.4 GR1): the ON written between them is revoked at the written POP.
        var d = Bound("""
                   >>PUSH ALL
                   >>REF-MOD-ZERO-LENGTH ON
                       PERFORM
                           DISPLAY "IMP1"
                       WHEN EC-SIZE
                           DISPLAY "HANDLER"
                       END-PERFORM
                       DISPLAY "INSIDE"
                   >>POP ALL
                       DISPLAY "AFTER"
            """);
        var s = RefModZeroLengthState.Build(d.RefModZeroLengthEvents);
        Assert.True(s.IsOnAt(Line("INSIDE")));
        Assert.False(s.IsOnAt(Line("AFTER")));
    }

    [Fact]
    public void FormatTwoPerform_HasNoImplicitScope()
    {
        // GR14 is a rule of the exception-checking format only; an inline Format-2 PERFORM brackets nothing.
        var d = Bound("""
                       PERFORM 1 TIMES
                   >>REF-MOD-ZERO-LENGTH ON
                           DISPLAY "BODY"
                       END-PERFORM
                       DISPLAY "AFTER"
            """);
        Assert.True(RefModZeroLengthState.Build(d.RefModZeroLengthEvents).IsOnAt(Line("AFTER")));
    }

    /// <summary>⛔ DRIFT: every <see cref="DirectiveTimeline{T}"/> member of <see cref="DirectiveResults"/> is
    /// replayed by <see cref="DirectiveResults.WithStackOps"/> — a new line-scoped directive state that the method
    /// forgot would outlive every exception-checking PERFORM's handlers, the PB1004 defect again.</summary>
    [Fact]
    public void WithStackOps_ReplaysEveryTimelineMember()
    {
        var d = Bound("""
                   >>TURN EC-SIZE CHECKING ON
                   >>REF-MOD-ZERO-LENGTH ON
                   >>FLAG-14 READ-PREVIOUS ON
                   >>PROPAGATE ON
                       DISPLAY "X"
            """);
        int pop = Line("X");
        var replayed = d.WithStackOps([
            new DirectiveStackOp(1, DirectiveStackKind.Push, null),
            new DirectiveStackOp(pop, DirectiveStackKind.Pop, null)]);
        var members = typeof(DirectiveResults).GetProperties()
            .Where(p => p.PropertyType.IsGenericType
                && p.PropertyType.GetGenericTypeDefinition() == typeof(DirectiveTimeline<>)).ToList();
        Assert.NotEmpty(members);
        Assert.True(d.HasLineScopedEvents());
        foreach (var member in members)
        {
            object timeline = member.GetValue(replayed)!;
            var revokedAt = member.PropertyType.GetMethod(nameof(DirectiveTimeline<int>.RevokedAt))!;
            Assert.True(((System.Collections.IEnumerable)timeline).Cast<object>().Any(),
                $"the drift fixture writes no {member.Name} event — add one");
            Assert.Equal(pop, (int)revokedAt.Invoke(timeline, [0])!);

            // … and the binder's walk guard sees an event held by THIS member alone.
            var ctor = typeof(DirectiveResults).GetConstructors().Single(c => c.GetParameters().Length > 1);
            var only = (DirectiveResults)ctor.Invoke([.. ctor.GetParameters().Select(p =>
                p.Name == member.Name ? member.GetValue(d)
                    : typeof(DirectiveResults).GetProperty(p.Name!)!.GetValue(DirectiveResults.None))]);
            Assert.True(only.HasLineScopedEvents(), $"HasLineScopedEvents misses {member.Name}");
        }
    }

    // ── fixture ─────────────────────────────────────────────────────────────────────────────────────────────

    [ThreadStatic] private static string[]? _lines;

    private static int Line(string marker) =>
        Array.FindIndex(_lines!, l => l.Contains($"DISPLAY \"{marker}\"", StringComparison.Ordinal)) + 1 is var n and > 0
            ? n : throw new InvalidOperationException($"no DISPLAY \"{marker}\"");

    /// <summary>Parse <paramref name="body"/> as a 2023 procedure division and return its directive results — with
    /// the front end's GR14 implicit ops already applied, exactly what <c>BinderDriver.Bind</c> folds.</summary>
    private static DirectiveResults Bound(string body)
    {
        string source = """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB1004T.
                   PROCEDURE DIVISION.
            """ + "\n" + body + "\n" + "           STOP RUN.\n";
        string path = Path.Combine(Path.GetTempPath(), $"pb1004_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, source);
        try
        {
            var diags = new DiagnosticBag();
            var frontend = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 };
            var tree = frontend.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            Assert.NotNull(tree);
            _lines = File.ReadAllLines(path);
            return frontend.Directives;
        }
        finally { try { File.Delete(path); } catch (IOException) { /* best-effort */ } }
    }
}
