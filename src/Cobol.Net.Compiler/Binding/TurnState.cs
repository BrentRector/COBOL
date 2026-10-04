// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Runtime.Exceptions;
using CobolNet.Frontend.Preprocessor;

using CobolNet.Binding.Model;
using CobolNet.Runtime;

namespace CobolNet.Binding;

/// <summary>
/// The compile-time <c>&gt;&gt;TURN</c> resolution (ISO/IEC 1989:2023 §7.3.25; deep-dive D10): the source-ordered
/// directive events, folded per query to decide WHETHER an EC guard is emitted at a statement at all — checking
/// OFF (the §7.3.25.4 GR1 default) compiles to NOTHING, the zero-scaffolding invariant (SSOT §18.16). A query
/// names a LEVEL-3 exception-name; an event matches it per the hierarchy expansion rules: EC-ALL covers every
/// name except EC-I-O-WARNING (GR2), a level-2 name covers its level-3 children except EC-I-O-WARNING (GR3),
/// EC-I-O-WARNING toggles only explicitly (GR4); a file-scoped event applies only to that file (GR6/GR8). The
/// fold is over events with <c>Line &lt; statementLine</c> — strict for REAL directives, which occupy their own
/// line and apply to SUCCEEDING statements only (GR5) — plus the SYNTHETIC Inclusive events (the GR14 implicit
/// enable and handler floor), which also govern a statement starting on their own line (kb/Work R14); the LAST
/// matching event wins (GR6/GR8 — enabled/disabled "until" the next toggle).
/// </summary>
public sealed class TurnState
{
    /// <summary>One flattened (line, name, file) toggle in source order. <paramref name="Inclusive"/> marks a
    /// SYNTHETIC event that governs statements starting ON its own line (kb/Work R14): a REAL directive
    /// occupies its own line, so no statement can share one and the fold's strict <c>Line &lt;
    /// statementLine</c> is right for it — but the GR14 implicit enable is placed AT the PERFORM's line and
    /// imperative-statement-1 can legally START there (`PERFORM WITH LOCATION GO TO … WHEN …` on one line),
    /// and the GR14 handler floor is placed AT the first WHEN's line, where a handler statement can also
    /// start. Both synthetics must cover their own line or the same-line statement silently escapes checking
    /// — measured: the one-line form raised nothing while the two-line form raised and caught.
    /// <paramref name="RevokedAt"/> is the line of the <c>&gt;&gt;POP</c> that restored the checking state saved
    /// before this directive (§7.3.20.4 GR1/GR3; kb/Work PB941) — from that line on the event is as though it had
    /// never been written. <see cref="int.MaxValue"/> for a directive no POP undid, and for every synthetic.</summary>
    private readonly record struct Ev(int Line, string Ec, string? File, bool On, bool WithLocation,
        bool Inclusive = false, int RevokedAt = int.MaxValue)
    {
        /// <summary>Is this event written AFTER the statement starting on <paramref name="statementLine"/>? GR5 —
        /// a directive governs SUCCEEDING statements only; an Inclusive synthetic also governs its own line. The
        /// events are line-ordered, so a fold stops at the first such event.</summary>
        public bool After(int statementLine) => Line >= statementLine && !(Inclusive && Line == statementLine);

        /// <summary>Has a POP revoked this event before the statement starting on <paramref name="statementLine"/>
        /// (§7.3.20.4 GR1/GR3)? A revoked event is skipped, never a stopping point — later events still apply.</summary>
        public bool RevokedFor(int statementLine) => statementLine >= RevokedAt;
    }

    private readonly List<Ev> _events = [];

    /// <summary>The empty state — no TURN directives; every query is disabled (GR1 default).</summary>
    public static readonly TurnState Empty = new();

    private TurnState() { }

    /// <summary>Build the state from the frontend's directive events, validating each exception-name against
    /// the catalog (§7.3.25.3 SR2 — a name shall be a §14.6.13.1 name or a user-/implementor-defined one) and
    /// the per-name edition window (the 2023-only families — VERSION_CHANGE_REFERENCE rows 40/61 — are
    /// diagnosed at --std 2002/2014; the directive itself is 2002-gated upstream).</summary>
    public static TurnState Build(IReadOnlyList<TurnEvent>? events, EditionContext edition)
    {
        if (events is null || events.Count == 0) return Empty;
        var timeline = DirectiveTimeline<TurnEvent>.Of(events);   // the PUSH/POP history (kb/Work PB941)
        var s = new TurnState();
        for (int k = 0; k < timeline.Count; k++)
        {
            var ev = timeline[k];
            // Position every diagnostic of this directive at ITS line (the event's resultant-text line, which the
            // sink maps to the source file and line): a bad name in `>>TURN` was reported with no location at all
            // (kb/Work PB1531's sibling — COBOLNET0711 printed `error COBOLNET0711: >>TURN: …` with no file(line,col)).
            using var _ = edition.At(ev.Line, 0);
            foreach (var (ec, file) in ev.Names)
            {
                // Resolution + the 0711/0878/1636 diagnostics live in the ONE funnel (kb/Work R05).
                if (!EcNameResolution.TryResolve(edition, ec, ">>TURN", out var info)) continue;
                s._events.Add(new Ev(ev.Line, info.Name, file, ev.On, ev.WithLocation, RevokedAt: timeline.RevokedAt(k)));
            }
        }
        return s;
    }

    /// <summary>Return a NEW state = the base events with a synthetic ENABLING event at <paramref name="imp1Line"/>
    /// spliced in for each named exception that is NOT already enabled there — the GR14 implicit TURN of an
    /// exception-checking PERFORM over imperative-statement-1 (§14.9.28.4 GR14): "if checking … is NOT enabled for
    /// imperative-statement-1 by a TURN directive, an implicit TURN directive … is assumed BEFORE the first
    /// statement in imperative-statement-1" (WITH LOCATION iff the PERFORM specified LOCATION). Correctness of the
    /// fold demands the event list stay LINE-SORTED (Fold keeps the last match with <c>Line &lt; statementLine</c>),
    /// so the synthetic is placed at imp-1's line — AFTER every pre-PERFORM directive (a pre-PERFORM
    /// <c>&gt;&gt;TURN OFF</c> therefore LOSES to it, per GR14's "not enabled ⇒ assume on") and BEFORE any real
    /// <c>&gt;&gt;TURN OFF</c> inside imp-1 (which overrides it). The "not already enabled" guard means a real
    /// enable BEFORE the PERFORM keeps its own settings (GR22 — no implicit is assumed). This overlay is discarded
    /// after imp-1; imp-2..5 then bind under <see cref="WithAllDisabledFrom"/>, which is GR14's other half — NOT
    /// against the base state, as this comment used to claim.
    /// The synthetic is INCLUSIVE (kb/Work R14): imperative-statement-1 can start on the PERFORM's own line
    /// and must bind under the enable — measured, the one-line form silently escaped checking.
    /// (Known edge: WITH LOCATION precedence when a real enable precedes — a refinement for the interceptor
    /// wave.)</summary>
    public TurnState WithImplicitEnable(IEnumerable<(string Ec, string? File)> names, bool withLocation, int imp1Line)
    {
        var s = new TurnState();
        foreach (var e in _events) if (e.Line < imp1Line) s._events.Add(e);   // pre-imp-1 directives (line-sorted)
        foreach (var (ec, file) in names)
            if (!Enabled(ec, file, imp1Line))   // GR14 implicit ON only when not already enabled (else GR22 persists)
                s._events.Add(new Ev(imp1Line, ec, file, On: true, WithLocation: withLocation, Inclusive: true));
        foreach (var e in _events) if (e.Line >= imp1Line) s._events.Add(e);   // imp-1+ directives (line-sorted)
        return s;
    }

    /// <summary>Return a NEW state = the base events with a synthetic <c>EC-ALL OFF</c> floor spliced in at
    /// <paramref name="handlerLine"/> — the GR14 implicit disable that governs an exception-checking PERFORM's
    /// HANDLER bodies (§14.9.28.4 GR14): "An implicit PUSH ALL followed by TURN OFF ALL is assumed AT THE END OF
    /// imperative-statement-1. Immediately preceding the END PERFORM phrase, there is an implicit POP ALL …".
    /// imp-2/3/4 (WHEN / OTHER / COMMON) and imp-5 (FINALLY) all execute between those two points, so NO exception
    /// checking is in effect inside them and §14.6.13.1.1 applies — "if checking for an exception that occurs is
    /// not enabled, no exception condition is raised".
    /// <para>⚠ GR21 is NOT the governing rule here, though the binder used to cite it: it says only that an
    /// exception raised in imp-2..5 "will not cause transfer of control to any of these imperative-statements"
    /// — re-entry, not the checking state. GR22 governs what survives AFTER the PERFORM. GR14 controls during.</para>
    /// <para><c>EcAll</c> matches every name in <c>NameMatches</c>/<c>Fold</c>, so the floor shadows every prior
    /// enable; real handler-local <c>&gt;&gt;TURN</c> directives are appended AFTER it and therefore still win,
    /// which is what keeps the fold's line-sorted invariant meaningful rather than flattening the state.</para></summary>
    public TurnState WithAllDisabledFrom(int handlerLine)
    {
        var s = new TurnState();
        foreach (var e in _events) if (e.Line < handlerLine) s._events.Add(e);
        // Inclusive: a handler statement can START on the first WHEN's own line and must bind under the floor
        // (kb/Work R14 — the same same-line rule as the GR14 enable, in the disabling direction).
        s._events.Add(new Ev(handlerLine, ExceptionCatalog.EcAll, null, On: false, WithLocation: false, Inclusive: true));
        foreach (var e in _events) if (e.Line >= handlerLine) s._events.Add(e);
        return s;
    }

    /// <summary>The 1-based lines of every <c>&gt;&gt;TURN</c> directive that NAMES one of <paramref name="ecNames"/>
    /// (matched on the canonical exception-name, ON or OFF alike) — the FLAG-02 b EC-PROGRAM-EXCEPTIONS detector
    /// flags such a directive when its source element calls a function or invokes a method (§7.3.14.4 GR4 b). A
    /// directive naming several of the set contributes its line once per name; the caller de-duplicates.</summary>
    public IEnumerable<int> DirectiveLinesNaming(IReadOnlySet<string> ecNames)
        => _events.Where(e => ecNames.Contains(e.Ec)).Select(e => e.Line);

    /// <summary>True when ANY enabling event exists — the group-level EC-machinery gate (a compilation group
    /// with no enabling TURN, no F3, no RAISE/RESUME emits byte-identical code to a pre-EC build).</summary>
    public bool AnyEnabled => _events.Any(e => e.On);

    /// <summary>True when any enabling event ANYWHERE in the group covers level-3 <paramref name="level3"/> —
    /// the group-level gate for machinery that must exist wherever the condition COULD pair (the EC-EXTERNAL
    /// descriptor registrations: an element whose own mask is zero must still register its descriptions when a
    /// LATER element in the group can check against them, §14.8.4 — TURN events are line-scoped across the
    /// whole group text).</summary>
    public bool AnyEnabledFor(string level3) => _events.Any(e => e.On && NameMatches(e.Ec, level3));

    /// <summary>Is checking for level-3 name <paramref name="level3"/> (for <paramref name="file"/>, when the
    /// name is EC-I-O-scoped) enabled at the statement starting on <paramref name="statementLine"/>?</summary>
    public bool Enabled(string level3, string? file, int statementLine) =>
        Fold(level3, file, statementLine) is { On: true };

    /// <summary>⛔ THE ACTIVATED ELEMENT'S HALF OF AN "ENABLED IN BOTH" GATE — asked at its PROCEDURE DIVISION HEADER
    /// (kb/Work PB1381). The parameter-conformance conditions §14.9.4.4 GR3 d) (EC-PROGRAM-ARG-MISMATCH) and
    /// §14.9.23.4 GR7 c) (EC-OO-UNIVERSAL) are set "if checking for it is enabled in both the activated program
    /// [method] and activating runtime element", and a TURN reaches the activated side through its header: §7.3.25.4
    /// GR6 / GR8 enable or disable checking "for the procedure division statements and procedure division headers
    /// that follow in the compilation group". So the fold point is the header's line — NOT the Environment division
    /// line §14.8.4.1 fixes for the EC-EXTERNAL conditions alone (a TURN between the DATA DIVISION and the header
    /// used to be ignored), and NOT the METHOD-ID line. <paramref name="header"/> is the element's
    /// <c>procedureDivision</c>; <paramref name="absentLine"/> answers for an element that has none.</summary>
    public bool EnabledAtHeader(string level3, Antlr4.Runtime.ParserRuleContext? header, int absentLine) =>
        Enabled(level3, null, header?.Start.Line ?? absentLine);

    /// <summary>Did the event that ENABLED <paramref name="level3"/> at this statement carry WITH LOCATION
    /// (§7.3.25.4 GR7 — the EXCEPTION-LOCATION/-STATEMENT information capture)?</summary>
    public bool WithLocation(string level3, string? file, int statementLine) =>
        Fold(level3, file, statementLine) is { On: true, WithLocation: true };

    private Ev? Fold(string level3, string? file, int statementLine)
    {
        Ev? last = null;
        foreach (var e in _events)
        {
            // Events are in line order; GR5 — a directive governs SUCCEEDING statements only. A synthetic
            // Inclusive event (the GR14 implicit enable / handler floor) also governs a statement starting
            // ON its own line — see the Ev remark (kb/Work R14). A POP-revoked event is skipped (kb/Work PB941).
            if (e.After(statementLine)) break;
            if (e.RevokedFor(statementLine) || !NameMatches(e.Ec, level3)) continue;
            if (e.File is not null && (file is null || !CobolNames.Same(e.File, file)))
                continue;   // a file-scoped event applies only to that file (GR6/GR8)
            last = e;
        }
        return last;
    }

    /// <summary>Does directive name <paramref name="evName"/> cover level-3 <paramref name="level3"/>?
    /// The §7.3.25.4 GR2/GR3/GR4 expansion, owned by <see cref="ExceptionCatalog.DirectiveCovers"/> — the
    /// RUNTIME fold (<see cref="EcCheckingProfile"/>, which answers the same question about a name chosen at
    /// run time) walks the identical rule, and a second copy here is how the two would drift (kb/Work PB408).
    /// </summary>
    private static bool NameMatches(string evName, string level3) =>
        ExceptionCatalog.DirectiveCovers(evName, level3);

    /// <summary>The §7.3.25 checking state that governs the statement starting on <paramref name="statementLine"/>,
    /// as a RUNTIME-queryable profile — the carrier for §14.9.18.4 GR1 b), whose enablement question is asked in
    /// the ACTIVATING runtime element about an exception-name a callee may not choose until run time
    /// (<c>GOBACK … RAISING LAST EXCEPTION</c>). It is the same event prefix <see cref="Fold"/> walks, so
    /// <c>profile.Enabled(n)</c> and <c>Enabled(n, null, statementLine)</c> agree for every level-3 <c>n</c> —
    /// asserted by <c>ExceptionCheckingProfileDriftTests</c>.
    /// <para>FILE-SCOPED events are excluded, matching the <c>file: null</c> fold: an activating statement
    /// references no file, and §7.3.25.4 GR6/GR8 scope a FILE-phrase directive to the statements that do.</para>
    /// </summary>
    public EcCheckingProfile ProfileAt(int statementLine)
    {
        List<(string Ec, bool On)>? evs = null;
        foreach (var e in _events)
        {
            if (e.After(statementLine)) break;
            if (e.File is not null || e.RevokedFor(statementLine)) continue;   // a POP-revoked event is gone (PB941)
            // Only the LAST event naming a given exception-name can ever be a query's last match, so an earlier
            // one with the same name is dead weight in the emitted literal. Dropping it preserves the fold
            // exactly (the relative order of the surviving, differently-named events is untouched) and bounds
            // the literal by the count of DISTINCT names the element mentions rather than by directive count.
            evs ??= [];
            evs.RemoveAll(x => CobolNames.Same(x.Ec, e.Ec));
            evs.Add((e.Ec, e.On));
        }
        return evs is null ? EcCheckingProfile.None : EcCheckingProfile.FromEvents(evs);
    }
}
