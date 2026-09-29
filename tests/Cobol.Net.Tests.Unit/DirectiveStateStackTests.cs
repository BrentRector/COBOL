// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using CobolNet.Binding;
using CobolNet.Editions;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using Xunit;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB941 — <c>&gt;&gt;PUSH</c> (ISO §7.3.22) and <c>&gt;&gt;POP</c> (§7.3.20) SAVE AND RESTORE directive
/// state, through the ONE <see cref="DirectiveStateStack"/> every state-holding stage runs.
/// <list type="bullet">
/// <item>§7.3.22.4 GR1 — "If directive-name is specified, the state of the directive is saved."</item>
/// <item>§7.3.22.4 GR2 — "If ALL is specified, the state of all of the directives other than EVALUATE, IF, PAGE,
/// POP, or PUSH are saved."</item>
/// <item>§7.3.20.4 GR1 / GR3 — the named directive's, or every directive's, stored state is restored.</item>
/// <item>§7.3.20.4 GR2 — a named POP with nothing stored is unsuccessful, and "the implementor shall provide a
/// warning mechanism" (COBOLNET2297; Annex A.1 item 140, REQUIRED).</item>
/// </list>
/// The DRIFT half holds the accounting: every pushable directive is in <see cref="DirectiveStateRegistry"/>, every
/// directive-state member of <see cref="DirectiveResults"/> is claimed there, and every CARRIED directive has a
/// behavioural case below — so the next directive to gain state cannot escape PUSH/POP silently.
/// </summary>
public sealed class DirectiveStateStackTests
{
    // ── the behavioural case per carried directive — the drift test requires one for every carried row ──────

    /// <summary>One PUSH / change / POP case per carried row. Each asserts the state AFTER the POP is the state
    /// saved by the PUSH, and — the control that makes it able to fail — that the change was in force inside.</summary>
    public static readonly IReadOnlyDictionary<string, Action> Cases = new Dictionary<string, Action>
    {
        [Constructs.TurnDirective2002] = TurnIsRestored,
        [Constructs.RefModZeroLength2023] = RefModZeroLengthIsRestored,
        [Constructs.PropagateDirective2002] = PropagateIsRestored,
        [Constructs.Flag02Directive2014] = () => FlagIsRestored("FLAG-02", FlagOption.Flag02EcProgramExceptions),
        [Constructs.Flag14Directive2023] = () => FlagIsRestored("FLAG-14", FlagOption.Flag14ReadPrevious),
        [Constructs.CobolWordsDirective2023] = CobolWordsAreRestored,
        [Constructs.LeapSecondDirective2002] = LeapSecondIsRestored,
        [Constructs.DefineDirective2002] = DefineIsRestored,
        [Constructs.SourceFormatDirective2002] = SourceFormatIsRestored,
    };

    public static TheoryData<string> CarriedRows()
    {
        var data = new TheoryData<string>();
        foreach (var row in Cases.Keys) data.Add(row);
        return data;
    }

    [Theory]
    [MemberData(nameof(CarriedRows))]
    public void EachCarriedDirective_IsSavedByPushAndRestoredByPop(string row) => Cases[row]();

    private static void TurnIsRestored()
    {
        // line 3 ON, 4 PUSH ALL, 5 OFF, 6 (inside), 7 POP ALL, 8 (after)
        var d = Directives("""
                   >>TURN EC-SIZE CHECKING ON
                   >>PUSH ALL
                   >>TURN EC-SIZE CHECKING OFF
                       DISPLAY "IN"
                   >>POP ALL
                       DISPLAY "OUT"
            """);
        var turn = TurnState.Build(d.TurnEvents, new EditionContext(2023));
        Assert.False(turn.Enabled("EC-SIZE-OVERFLOW", null, Line(d, "IN")));
        Assert.True(turn.Enabled("EC-SIZE-OVERFLOW", null, Line(d, "OUT")));
    }

    private static void RefModZeroLengthIsRestored()
    {
        var d = Directives("""
                   >>PUSH REF-MOD-ZERO-LENGTH
                   >>REF-MOD-ZERO-LENGTH ON
                       DISPLAY "IN"
                   >>POP REF-MOD-ZERO-LENGTH
                       DISPLAY "OUT"
            """);
        var s = RefModZeroLengthState.Build(d.RefModZeroLengthEvents);
        Assert.True(s.IsOnAt(Line(d, "IN")));
        Assert.False(s.IsOnAt(Line(d, "OUT")));
        Assert.True(s.IsUnspecifiedAt(Line(d, "OUT")));   // back to "not specified", the FLAG-14 i tri-state
    }

    /// <summary>kb/Work PB1119 — the PROPAGATE state a PUSH saves is the ON/OFF fold the binder asks at each
    /// source element's first line (§7.3.22.4 GR1, §7.3.20.4 GR1).</summary>
    private static void PropagateIsRestored()
    {
        var d = Directives("""
                   >>PROPAGATE ON
                   >>PUSH PROPAGATE
                   >>PROPAGATE OFF
                       DISPLAY "IN"
                   >>POP PROPAGATE
                       DISPLAY "OUT"
            """);
        var s = PropagateState.Build(d.PropagateEvents);
        Assert.False(s.IsOnAt(Line(d, "IN")));
        Assert.True(s.IsOnAt(Line(d, "OUT")));
    }

    private static void FlagIsRestored(string directive, FlagOption option)
    {
        string word = FlagOptions.Info(option).Word;
        var d = Directives($"""
                   >>PUSH {directive}
                   >>{directive} {word} ON
                       DISPLAY "IN"
                   >>POP {directive}
                       DISPLAY "OUT"
            """);
        var s = FlagState.Build(d.FlagEvents);
        Assert.True(s.IsOnAt(Line(d, "IN"), option));
        Assert.False(s.IsOnAt(Line(d, "OUT"), option));
    }

    private static void CobolWordsAreRestored()
    {
        var pushed = Directives("""
                   DISPLAY "X"
            """, prefix: """
                   >>PUSH COBOL-WORDS
                   >>COBOL-WORDS RESERVE "ZQXWORD"
                   >>POP COBOL-WORDS
            """);
        Assert.True(pushed.CobolWordsMap.IsEmpty);
        var control = Directives("""
                   DISPLAY "X"
            """, prefix: """
                   >>COBOL-WORDS RESERVE "ZQXWORD"
            """);
        Assert.False(control.CobolWordsMap.IsEmpty);
    }

    private static void LeapSecondIsRestored()
    {
        Assert.False(Directives("""
                   DISPLAY "X"
            """, prefix: """
                   >>PUSH LEAP-SECOND
                   >>LEAP-SECOND ON
                   >>POP LEAP-SECOND
            """).LeapSecondOn);
        Assert.True(Directives("""
                   DISPLAY "X"
            """, prefix: """
                   >>PUSH LEAP-SECOND
                   >>LEAP-SECOND ON
            """).LeapSecondOn);
    }

    private static void DefineIsRestored()
    {
        string text = ConditionalCompilationProcessor.Process("""
            >>DEFINE V AS 1
            >>PUSH DEFINE
            >>DEFINE V AS 2 OVERRIDE
            >>IF V = 2
            INSIDE-SAW-2
            >>END-IF
            >>POP DEFINE
            >>IF V = 1
            AFTER-SAW-1
            >>END-IF
            """, CnFrontend.LeftDirectives, new DiagnosticBag(), "t.cob", 2023);
        Assert.Contains("INSIDE-SAW-2", text);
        Assert.Contains("AFTER-SAW-1", text);
    }

    private static void SourceFormatIsRestored()
    {
        // FIXED, PUSH SOURCE, FREE, POP SOURCE: the last line carries a sequence number in columns 1-6, which the
        // fixed-form reading strips and a free-form reading keeps.
        string text = ReferenceFormatProcessor.NormalizeToFreeForm(
            "       >>SOURCE FIXED\n"
            + "       >>PUSH SOURCE\n"
            + "       >>SOURCE FREE\n"
            + "FREE-LINE\n"
            + "       >>POP SOURCE\n"
            + "000100     FIXED-LINE\n");
        Assert.Contains("FREE-LINE", text);
        Assert.Contains("FIXED-LINE", text);
        Assert.DoesNotContain("000100", text);
        Assert.Contains(">>POP SOURCE", text);   // the POP line survives for DirectiveSiteProcessor
    }

    // ── the unsuccessful POP (§7.3.20.4 GR2) ──────────────────────────────────────────────────────────────

    [Fact]
    public void ANamedPopWithNothingStored_Warns_AndPopAllDoesNot()
    {
        var bag = new DiagnosticBag();
        DirectiveSiteProcessor.Process("""
            >>POP TURN
            >>POP ALL
            >>PUSH ALL
            >>POP TURN
            >>POP TURN
            >>POP LISTING
            >>POP LISTING
            """, bag, "t.cob");
        // line 1: nothing stored; line 4: PUSH ALL stored TURN, so it succeeds; line 5: already restored;
        // line 6: PUSH ALL stored LISTING (a directive with no state still has a save to remove); line 7: gone.
        var warned = bag.Diagnostics.Where(x => x.Code == "COBOLNET2297").Select(x => x.Location.Line + 1).ToList();
        Assert.Equal([1, 5, 7], warned);
        Assert.All(bag.Diagnostics.Where(x => x.Code == "COBOLNET2297"),
            x => Assert.Equal(DiagnosticSeverity.Warning, x.Severity));
        Assert.Contains("§7.3.20.4 GR2", bag.Diagnostics.First(x => x.Code == "COBOLNET2297").Message);
    }

    [Fact]
    public void PopRestoresTheMostRecentSave_Nested()
    {
        var d = Directives("""
                   >>TURN EC-SIZE CHECKING ON
                   >>PUSH TURN
                   >>TURN EC-SIZE CHECKING OFF
                   >>PUSH ALL
                   >>TURN EC-SIZE CHECKING ON
                   >>POP TURN
                       DISPLAY "MID"
                   >>POP ALL
                       DISPLAY "OUT"
            """);
        var turn = TurnState.Build(d.TurnEvents, new EditionContext(2023));
        Assert.False(turn.Enabled("EC-SIZE-OVERFLOW", null, Line(d, "MID")));
        Assert.True(turn.Enabled("EC-SIZE-OVERFLOW", null, Line(d, "OUT")));
    }

    [Fact]
    public void APush_LeavesTheSavedStateInForce()
    {
        // §7.3.22.4 GR3 — "The effects of the directive being pushed remain active."
        var d = Directives("""
                   >>TURN EC-SIZE CHECKING ON
                   >>PUSH ALL
                       DISPLAY "IN"
                   >>POP ALL
            """);
        Assert.True(TurnState.Build(d.TurnEvents, new EditionContext(2023)).Enabled("EC-SIZE-OVERFLOW", null, Line(d, "IN")));
        string text = ConditionalCompilationProcessor.Process("""
            >>DEFINE V AS 1
            >>DEFINE W AS 2
            >>PUSH DEFINE
            >>IF V = 1 AND W = 2
            BOTH-STILL-DEFINED
            >>END-IF
            """, CnFrontend.LeftDirectives, new DiagnosticBag(), "t.cob", 2023);
        Assert.Contains("BOTH-STILL-DEFINED", text);   // every instance of DEFINE stays in force (GR3)
    }

    [Fact]
    public void ANamedPush_SavesOnlyThatDirective()
    {
        // PUSH TURN then POP ALL: FLAG-14 was never saved, so its ON survives; TURN is restored to OFF.
        var d = Directives("""
                   >>PUSH TURN
                   >>TURN EC-SIZE CHECKING ON
                   >>FLAG-14 READ-PREVIOUS ON
                   >>POP ALL
                       DISPLAY "OUT"
            """);
        Assert.False(TurnState.Build(d.TurnEvents, new EditionContext(2023)).Enabled("EC-SIZE-OVERFLOW", null, Line(d, "OUT")));
        Assert.True(FlagState.Build(d.FlagEvents).IsOnAt(Line(d, "OUT"), FlagOption.Flag14ReadPrevious));
    }

    [Fact]
    public void APushPopInAnOmittedBranch_ActsOnNothing()
    {
        string text = ConditionalCompilationProcessor.Process("""
            >>DEFINE V AS 1
            >>IF V = 2
            >>PUSH DEFINE
            >>END-IF
            >>DEFINE V AS 3 OVERRIDE
            >>POP DEFINE
            >>IF V = 3
            STILL-3
            >>END-IF
            """, CnFrontend.LeftDirectives, new DiagnosticBag(), "t.cob", 2023);
        Assert.Contains("STILL-3", text);
    }

    // ── drift: the accounting that keeps the NEXT stateful directive covered ──────────────────────────────

    [Fact]
    public void TheRegistry_AccountsForEveryPushableDirective_AndNothingElse()
    {
        Assert.Equal(CompilerDirectiveCatalog.PushableRows.Order(StringComparer.Ordinal),
            DirectiveStateRegistry.Entries.Select(e => e.Row).Order(StringComparer.Ordinal));
        Assert.All(DirectiveStateRegistry.Entries, e => Assert.False(string.IsNullOrWhiteSpace(e.Reason)));
    }

    [Fact]
    public void ThePushableSet_IsTheCatalogMinusGr2sFive()
    {
        // §7.3.22.4 GR2 names five: EVALUATE, IF, PAGE, POP, PUSH. Each names a directive ROW (EVALUATE's row also
        // owns WHEN / END-EVALUATE), so the pushed set is every other row.
        var five = new[] { "EVALUATE", "IF", "PAGE", "POP", "PUSH" }
            .Select(w => CompilerDirectiveCatalog.Find(w)!.Id).ToHashSet();
        var all = CompilerDirectiveCatalog.Words.Select(w => CompilerDirectiveCatalog.Find(w)!.Id).ToHashSet();
        Assert.Equal(all.Except(five).Order(StringComparer.Ordinal), CompilerDirectiveCatalog.PushableRows);
    }

    [Fact]
    public void EveryCarrier_IsARealStage_AndEveryCarriedRowHasABehaviouralCase()
    {
        var frontend = typeof(DirectiveStateStack).Assembly;
        foreach (var e in DirectiveStateRegistry.Entries)
            foreach (var carrier in e.Carriers)
                Assert.True(frontend.GetTypes().Any(t => t.Name == carrier),
                    $"{e.Row}: carrier '{carrier}' names no Frontend type");
        Assert.Equal(DirectiveStateRegistry.Entries.Where(e => e.IsCarried).Select(e => e.Row).Order(StringComparer.Ordinal),
            Cases.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void EveryDirectiveResultsMember_IsClaimedByTheRegistry()
    {
        // A directive whose state reaches the binder does so through a DirectiveResults member (kb/Work PB65). A
        // new member no entry claims is a new directive state PUSH/POP does not cover.
        var claimed = DirectiveStateRegistry.Entries.SelectMany(e => e.Products)
            .Concat(DirectiveStateRegistry.NotState).ToHashSet();
        foreach (var p in typeof(DirectiveResults).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            if (p.Name != "EqualityContract")
                Assert.True(claimed.Contains(p.Name), $"DirectiveResults.{p.Name} is claimed by no registry entry");
    }

    // ── helpers ──────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Run the real frontend over a 2023 program whose procedure division is <paramref name="body"/>,
    /// with <paramref name="prefix"/> (directives) before the IDENTIFICATION DIVISION.</summary>
    private static DirectiveResults Directives(string body, string prefix = "")
    {
        string source = (prefix.Length == 0 ? "" : prefix + "\n") + """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB941T.
                   PROCEDURE DIVISION.
            """ + "\n" + body + "\n" + "           STOP RUN.\n";
        string path = Path.Combine(Path.GetTempPath(), $"pb941_{Guid.NewGuid():N}.cob");
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

    [ThreadStatic] private static string[]? _lines;

    /// <summary>The 1-based line of the DISPLAY of <paramref name="marker"/> in the last source.</summary>
    private static int Line(DirectiveResults _, string marker) =>
        Array.FindIndex(_lines!, l => l.Contains($"DISPLAY \"{marker}\"")) + 1;
}
