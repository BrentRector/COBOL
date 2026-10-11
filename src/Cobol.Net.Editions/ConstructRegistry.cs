// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Editions;

/// <summary>
/// The construct registry + the ONE gating entry point (P2.5): every edition gate — validator override or
/// binder-side — routes through <see cref="Check"/>, which evaluates availability at the targeted
/// <see cref="EditionInfo"/> and reports the verdict to an <see cref="IDiagnosticSink"/> at the severity the
/// ONE <see cref="EditionSeverityPolicy"/> dictates (one policy, several emit sites; feedback_one_mechanism_per_job).
/// As of rearch PHASE 02 the registry lives BELOW the frontend, so both Frontend and Compiler consume it.
/// </summary>
public static partial class ConstructRegistry
{
    // Entries (the constructs.json rendering) is the generated partial half — ConstructRegistry.g.cs.
    private static Dictionary<string, ConstructDialectStatus>? _byId;

    /// <summary>Look up a registry entry by its constructs.json row id.</summary>
    public static ConstructDialectStatus? Find(string id) =>
        (_byId ??= Entries.ToDictionary(e => e.Id, StringComparer.Ordinal)).GetValueOrDefault(id);

    /// <summary>True when the construct <paramref name="id"/> is in force at <paramref name="edition"/> — the quiet
    /// twin of <see cref="Check"/> for a site that must BRANCH on the edition fact instead of reporting it (a binder
    /// route that is inert below the edition that introduced its declaration surface, kb/Work PB2589). It reads the
    /// same row <see cref="Check"/> reports from, so the branch and the diagnostic cannot name different editions.</summary>
    public static bool IsAvailable(EditionInfo edition, string id) =>
        (Find(id) ?? throw new ArgumentException($"unregistered construct id '{id}'", nameof(id)))
            .StatusAt(edition.Year) == ConstructAvailability.Available;

    /// <summary>
    /// THE gating entry point: evaluate <paramref name="id"/> at <paramref name="edition"/> and report the
    /// verdict to <paramref name="sink"/> at the severity <see cref="EditionSeverityPolicy"/> dictates —
    /// NotYetIntroduced ⇒ error on both axes (0900 band unless a single-edge pinned code); Removed ⇒ error
    /// strict / warning permissive; Obsolete ⇒ 0903 warning. <paramref name="where"/> localizes the diagnostic
    /// ("FD OUT-FILE", "paragraph P1", …). Layer-neutral: the frontend and the compiler (through the
    /// <c>EditionContext</c> adapter, which IS the sink) call this identical funnel. The text/codes are
    /// byte-identical to the pre-PHASE-02 direct <c>edition.Error/Removed/Warning</c> emission.
    /// </summary>
    public static void Check(EditionInfo edition, IDiagnosticSink sink, string id, string where)
    {
        var c = Find(id) ?? throw new ArgumentException($"unregistered construct id '{id}'", nameof(id));
        var verdict = c.StatusAt(edition.Year);
        if (verdict == ConstructAvailability.Available) return;
        var severity = EditionSeverityPolicy.For(verdict, edition);
        switch (verdict)
        {
            case ConstructAvailability.NotYetIntroduced:
                // Dual-obligation rows (an availability WINDOW: DiagnosticCode names the removal edge) use the
                // 0900 band for the introduction edge; single-edge rows keep their pinned code (pic-wide's 0802).
                sink.Report(new EditionDiagnostic(
                    c.RemovedIn is null ? c.DiagnosticCode : EditionCodes.Introduction, severity, c.Id,
                    $"{c.Display} requires COBOL-{c.IntroducedIn} (targeting COBOL-{edition.Year}) — {where} ({c.Citation})",
                    where, c.Citation));
                break;
            case ConstructAvailability.Removed:
                sink.Report(new EditionDiagnostic(c.DiagnosticCode, severity, c.Id,
                    $"{c.Display} was removed in COBOL-{c.RemovedIn} (targeting COBOL-{edition.Year}) — {where} ({c.Citation})",
                    where, c.Citation));
                break;
            case ConstructAvailability.Obsolete:
                sink.Report(new EditionDiagnostic(EditionCodes.ObsoleteFlag, severity, c.Id,
                    $"{c.Display} is obsolete as of COBOL-{c.ObsoleteIn} — {where} ({c.Citation})",
                    where, c.Citation));
                break;
        }
    }
}
