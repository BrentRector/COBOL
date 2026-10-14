// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Editions.Diagnostics;

/// <summary>
/// Which of the standard's two "need not implement" annexes lists the language element a DECLINED-FACILITY
/// diagnostic refuses or warns about. This is the datum that DECIDES which clause licenses the posture, and until
/// kb/Work PB709 it existed only as free text inside <see cref="DiagnosticDescriptor.Title"/> and
/// <see cref="DiagnosticDescriptor.IsoSection"/> — where a §4.2.6 written at an Annex A.4 site is a REAL citation
/// quoting REAL text that answers a DIFFERENT question, so <c>audit_code_citations.py --check</c> passes and the
/// wrong clause propagates into the next comment, golden and DEVLOG paragraph that inherits it.
///
/// <para>⛔ THE TWO CLAUSES ARE NOT INTERCHANGEABLE, and the difference is normative rather than editorial.
/// ISO §4.2.6 (Annex A.3, <i>processor-dependent</i> language elements) carries TWO sentences nothing else in the
/// standard carries: "An implementation shall provide a warning mechanism at compile time to indicate use of
/// syntactically-detectable processor-dependent language elements not supported by that implementation", and
/// "it is not required to diagnose syntax errors within this unsupported syntax". ISO §4.2.7 (Annex A.4,
/// <i>optional</i> language elements) has NEITHER: it makes the element one an implementor "may, but need not,
/// implement", requires only that the claim be identified in user documentation, and Annex A.4.1 then admits the
/// syntax "only when support for that language element is claimed by the implementor". So an A.3 decline is
/// accept-and-WARN <i>because the standard mandates the warning</i>; an A.4 decline is refuse-by-name <i>because
/// accepting unclaimed syntax is itself the non-conformance</i> — and the permissive-inside/exact-at-the-edges
/// parse an A.3 decline gets for free is, at an A.4 site, a consequence of the element not being part of this
/// implementation at all rather than of any excusing sentence.</para>
///
/// <para>A facility CAN be listed in BOTH — commit and rollback is Annex A.3 items 6-7 <i>and</i> Annex A.4.3 —
/// so this is a flags enum and not a choice.</para>
/// </summary>
[Flags]
public enum DeclinedAnnex
{
    /// <summary>Not a declined-facility diagnostic — the overwhelming majority of the catalog.</summary>
    None = 0,
    /// <summary>Annex A.3, the processor-dependent language element list — licensed by ISO §4.2.6.</summary>
    A3 = 1,
    /// <summary>Annex A.4, the optional language element list — licensed by ISO §4.2.7 (with Annex A.4.1).</summary>
    A4 = 2,
}

/// <summary>
/// A first-class descriptor for one diagnostic (rearch PHASE 02, P2.10 — fixes P8: the compiler emitted codes
/// as bare string literals, with the <c>COBOLNET0899</c> catch-all spread across ~44 sites and codes reused
/// across unrelated rules, so nothing was documentable, suppressible, or drift-bound). A descriptor gives each
/// diagnostic a STABLE identity (<see cref="Id"/>) independent of its emitted <see cref="Code"/> number, a
/// documented ISO citation and severity, and a <see cref="SuppressKey"/> family — the raw material for
/// <c>docs/DIAGNOSTICS.md</c>, <c>--suppress</c>, and the drift test.
/// </summary>
/// <param name="Code">The emitted <c>COBOLNETnnnn</c> code. NOT unique across descriptors: several descriptors
/// deliberately share a code (the <c>COBOLNET0899</c> recognized-not-implemented family; the reused
/// <c>COBOLNET1533</c> strong-type rules) — the code is what the user sees, <see cref="Id"/> is the identity.</param>
/// <param name="Id">The STABLE, unique kebab-case slug — survives code renumbering; the addressable identity used
/// by <c>--suppress</c>, the version matrix, and snapshot review.</param>
/// <param name="Severity">Error | Warning. Uses <see cref="EditionSeverity"/> (the Editions-native two-value
/// severity) — every P2-catalogued diagnostic is one of those two. The eventual unification with the frontend's
/// three-value <c>CobolNet.Frontend.Diagnostics.DiagnosticSeverity</c> (which adds Info) is the P7 diagnostic
/// merge; deferred so P2 adds no third parallel severity type (feedback_one_mechanism_per_job).</param>
/// <param name="Title">A short human summary of what the diagnostic reports — the <c>docs/DIAGNOSTICS.md</c> row
/// text and the eventual <c>--explain</c> text. NOT the runtime message: the emitted message is composed at the
/// call site (many of these diagnostics interpolate item names / ISO §s that only the site has). Converting the
/// composed messages to descriptor-owned format strings is the broader P7 migration.</param>
/// <param name="IsoSection">The ISO/IEC 1989:2023 § (or roadmap) citation this diagnostic enforces.</param>
/// <param name="SuppressKey">The <c>--suppress</c> family key. <see langword="null"/> ⇒ falls back to
/// <see cref="Code"/> (so an edition-band code, unique per descriptor, is suppressed by its code; the shared
/// <c>COBOLNET0899</c> family sets this explicitly — e.g. <c>recognized-not-implemented</c> — so a developer can
/// mute all deferred-feature diagnostics as a group without also muting genuine validation errors on the same
/// code).</param>
/// <param name="PermissiveInert">The declined element is accepted under <c>--permissive</c> (a warning, not the
/// refusal): a clause binds to a no-op, a declined module's exception-name resolves and is raised only by a RAISE
/// statement — read by the one refuse rule <c>EditionContext.Refuses</c>, never tested at an emit site.</param>
/// <param name="Annex">For a DECLINED-FACILITY diagnostic, which annex lists the element (kb/Work PB709). See
/// <see cref="DeclinedAnnex"/>: it is what <see cref="PostureClause"/> derives the licensing § from, so the clause
/// cannot be written by hand at one site and contradicted at the next. <see cref="DeclinedAnnex.None"/> for every
/// diagnostic that is not a decline.</param>
public sealed record DiagnosticDescriptor(
    string Code,
    string Id,
    EditionSeverity Severity,
    string Title,
    string IsoSection,
    string? SuppressKey = null,
    bool PermissiveInert = false,
    DeclinedAnnex Annex = DeclinedAnnex.None)
{
    /// <summary>The effective <c>--suppress</c> family key: <see cref="SuppressKey"/> when set, else
    /// <see cref="Code"/>.</summary>
    public string ResolvedSuppressKey => SuppressKey ?? Code;

    /// <summary>The ISO clause that licenses this decline — DERIVED from <see cref="Annex"/> and never written by
    /// hand, which is the whole point of carrying the annex as data (kb/Work PB709). §4.2.6 for a processor-dependent
    /// element (Annex A.3), §4.2.7 for an optional one (Annex A.4), both where the facility is listed in both.
    /// <see langword="null"/> for a diagnostic that is not a decline.</summary>
    public string? PostureClause => Annex switch
    {
        DeclinedAnnex.A3 => "§4.2.6",
        DeclinedAnnex.A4 => "§4.2.7",
        DeclinedAnnex.A3 | DeclinedAnnex.A4 => "§4.2.6 / §4.2.7",
        _ => null,
    };
}
