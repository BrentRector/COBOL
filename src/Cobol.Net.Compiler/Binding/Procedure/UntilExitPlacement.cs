// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding.Procedure;

/// <summary>
/// The OUT-OF-LINE half of ISO §14.9.28.3 SR8 — "The UNTIL EXIT phrase shall not be specified in a PERFORM
/// statement with or under a PERFORM statement with the VARYING phrase or either the TEST BEFORE or TEST AFTER
/// phrase" (kb/Work PB434).
///
/// <para>The "with" arm and the LEXICAL "under" arm are decided where the PERFORM binds
/// (<c>ControlFlowBinder.CheckUntilExitPlacement</c>, asking <see cref="EnclosingContext.UnderVaryingOrTestPerform"/>).
/// What no bind position can see is a PERFORM UNTIL EXIT written in a paragraph that a VARYING or TEST PERFORM
/// elsewhere performs: §14.9.28.4 GR1 puts that paragraph in the PERFORM's range, and whether any such PERFORM
/// exists is known only once every procedure of the source element is bound. So the binder RECORDS here — each
/// UNTIL EXIT statement no frame refused (its paragraph pc and position), and the ranges each VARYING or TEST
/// PERFORM executes — and <see cref="Report"/> answers the question once, over <see cref="ProcedureReach"/>.</para>
/// </summary>
internal sealed class UntilExitPlacement
{
    private readonly List<(int Pc, DiagnosticCursor At)> _sites = [];
    private readonly List<(PcRange Range, int Line)> _barring = [];

    /// <summary>Record a PERFORM statement specifying UNTIL EXIT, bound in paragraph <paramref name="pc"/>, that
    /// neither carries VARYING / TEST itself nor lies lexically under a PERFORM that does.</summary>
    public void AddSite(int pc, DiagnosticCursor at) => _sites.Add((pc, at));

    /// <summary>Record procedure ranges executed by a PERFORM written with the VARYING phrase or a TEST phrase at
    /// source line <paramref name="line"/>: an out-of-line PERFORM's own specified set, or every procedure an
    /// inline PERFORM's imperative-statement-1 transfers to and returns from.</summary>
    public void AddBarringRanges(IEnumerable<PcRange> ranges, int line)
    {
        foreach (var range in ranges) _barring.Add((range, line));
    }

    /// <summary>Refuse every recorded UNTIL EXIT statement whose paragraph lies in the range of a recorded VARYING
    /// or TEST PERFORM — called once per source element, after its procedures (and their exception-checking
    /// handler paragraphs) are bound, because <paramref name="paragraphs"/> is the pc space the ranges index.</summary>
    public void Report(IReadOnlyList<BoundParagraph> paragraphs, EditionContext edition)
    {
        if (_sites.Count == 0 || _barring.Count == 0) return;
        var under = ProcedureReach.Closure(paragraphs, _barring);
        foreach (var (pc, at) in _sites)
        {
            if (!under.TryGetValue(pc, out int line)) continue;
            using var _ = edition.At(at);
            edition.Error(DiagnosticCatalog.PerformUntilExitPlacement,
                $"the UNTIL EXIT phrase is specified in a PERFORM statement under the PERFORM statement at line {line}, "
                + "which has the VARYING phrase or a TEST BEFORE / TEST AFTER phrase: this statement's paragraph is "
                + "in that PERFORM's range (ISO §14.9.28.4 GR1), and \"The UNTIL EXIT phrase shall not be specified in "
                + "a PERFORM statement with or under a PERFORM statement with the VARYING phrase or either the TEST "
                + "BEFORE or TEST AFTER phrase\" (ISO §14.9.28.3 SR8)");
        }
    }
}
