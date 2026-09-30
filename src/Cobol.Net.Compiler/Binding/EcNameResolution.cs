// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions.Diagnostics;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Binding;

/// <summary>
/// ⛔ THE ONE FUNNEL FOR RESOLVING A WRITTEN EXCEPTION-NAME (kb/Work R05). Before it, the unknown-name
/// diagnostic (COBOLNET0711) and the introduction gate (COBOLNET0878) existed as FOUR verbatim copies each —
/// &gt;&gt;TURN (TurnState), the Format-3 USE builder (ProcedureTableBuilder), the exception-checking PERFORM's
/// WHEN list (EcBinder.ExceptionPerform), and RAISE / EXIT–GOBACK RAISING (EcBinder) — which is exactly the
/// shape under which R05's §15.33 width advisory would have been added to one arm of six
/// (feedback_two_arm_dispatch; feedback_one_rule_one_place: the extraction IS the fix).
///
/// <para>The procedure-division-header RAISING phrase (§14.2.2 SR7 — program, function and METHOD-ID headers
/// alike, through the ONE partition <c>RaisingPhrase.Partition</c>, kb/Work PB815) resolves through
/// <see cref="ExceptionCatalog.TryGet"/> itself because an unresolved word may legally be a class-name or an
/// interface-name there (SR8/SR9) — it calls <see cref="Advise"/> directly on the names it accepts. <c>EcNameResolutionDriftTests</c> derives the caller list from the source, so a seventh resolution
/// site cannot appear un-funneled and un-advised.</para>
/// </summary>
internal static class EcNameResolution
{
    /// <summary>Resolve a written exception-name: COBOLNET0711 when it is neither in the §14.6.13.1 catalog nor
    /// a valid EC-USER- name; optionally COBOLNET0710 when a level-1/-2 name stands where
    /// only level-3 is legal (the RAISE/RAISING contexts — checked BEFORE the introduction gate so the level
    /// error keeps priority for a level-2 name of a later family); COBOLNET0878 when the name's family postdates
    /// the targeted edition. Otherwise <see cref="Advise"/> decides: it carries the §15.33 width advisory AND the
    /// EC-SCREEN refusal (COBOLNET1707), and returns false when it refused.
    /// <para>⛔ <paramref name="level3"/> is the RAISING SITE, not a bool, because the rule it enforces has a
    /// DIFFERENT ordinal in each of the three statements that reach here (EXIT §14.9.14.3 SR3 · GOBACK
    /// §14.9.18.3 SR2 · RAISE §14.9.29.3 SR1) and this one message prints it — see <see cref="EcRaiseSite"/>
    /// (kb/Work PB388).</para></summary>
    public static bool TryResolve(EditionContext edition, string raw, string where, out EcInfo info,
        EcRaiseSite? level3 = null)
    {
        if (!ExceptionCatalog.TryGet(raw, out info))
        {
            edition.Error(DiagnosticCatalog.EcNameUnknown, $"{where}: '{raw}' is not an exception-name of "
                + "ISO/IEC 1989 §14.6.13.1 (and not a valid EC-USER- name; this implementation defines no EC-IMP- name, DOC-A.1-99)");
            return false;
        }
        // The EC-LOCALE family (Annex A.4.9 item 1) WAS refused here by name while the locale module was documented
        // non-support (kb/Work PB100) and is LEGAL again since kb/Work PB64 T1 adopted the module: EC-LOCALE-MISSING,
        // EC-LOCALE-INVALID-PTR and EC-LOCALE-INCOMPATIBLE now have live raise sites (SET LOCALE §14.9.39.4 GR24 / GR21;
        // the IS LOCALE sequence §8.8.4.2.11), so a program can >>TURN them on, name them in a USE declarative, or match
        // them in a WHEN phrase — as EC-ORDER-NOT-SUPPORTED has been since PB101 T7. Refusing a name whose condition
        // this compiler raises would make the condition unobservable.
        // ⚠ THE EC-SCREEN FAMILY IS THE SAME SHAPE IN THE OTHER DIRECTION and lives in Advise (kb/Work PB260):
        // Annex A.4.2 is still Not claimed, those four names have no raise site and no reader, and accepting
        // them made a >>TURN or RAISE compile against a facility that does not exist. When the screen module is
        // ever claimed, delete that branch exactly as PB64 T1 deleted this one — the two are one rule.
        if (level3 is { } site && info.Level != 3)
        {
            edition.Error("COBOLNET0710", $"{where}: exception-name '{info.Name}' is a level-{info.Level} "
                + $"name; only a LEVEL-3 exception-name may be raised ({site.Cite(site.Level3Rule)})");
            return false;
        }
        if (info.IntroducedIn > edition.DialectLevel)
        {
            edition.Error(DiagnosticCatalog.EcNameIntroducedLater, $"{where}: exception-name {info.Name} was "
                + $"introduced by ISO/IEC 1989:{info.IntroducedIn} — it requires --std {info.IntroducedIn} or "
                + $"later (targeting COBOL-{edition.DialectLevel})");
            return false;
        }
        return Advise(edition, info, where);
    }

    /// <summary>One declined module's exception-name family: the name PREFIX it owns, plus the three strings the
    /// diagnostic needs. A record rather than three parallel arrays, and a TABLE rather than an if-chain,
    /// because the population is open — every A.4 module this compiler declines owns one (CLAUDE.md rule 5:
    /// prefer the shape that makes the NEXT case automatic).</summary>
    private readonly record struct DeclinedEcFamily(string Prefix, DiagnosticDescriptor Descriptor,
                                                   string Facility, string Annex, string Documentation);

    /// <summary>⛔ THE DECLINED-MODULE EXCEPTION-NAME TABLE. Ordered by prefix length so a longer, more specific
    /// prefix cannot be shadowed by a shorter one.
    /// <para>WHY REFUSING THE NAME IS THE CONFORMING ANSWER, not over-strictness: §14.6.13.1.1 says "The
    /// implementor is not required to raise any exception conditions for level-3 exception-names that are
    /// associated with optional language elements … that the implementor has not implemented" — and that is
    /// exactly the problem. These names had catalog entries and ZERO setting sites, so
    /// `&gt;&gt;TURN EC-VALIDATE CHECKING ON`, `RAISE EC-VALIDATE-CONTENT` and a `USE AFTER EXCEPTION CONDITION
    /// EC-FLOW-COMMIT` declarative all compiled CLEAN against a facility that does not exist — a silent promise
    /// the run unit can never keep. A.4.1 makes the module's exception conditions optional WITH the module, so
    /// declining the module declines its names.</para>
    /// <para>⛔ EC-SCREEN IS A ROW HERE, and that is the whole point of the table shape. The A.4.2 screen
    /// wave (kb/Work PB260) landed its own predicate for the same job an hour before this one merged —
    /// two mechanisms for "a declined module's exception-names", which is the anti-pattern
    /// (feedback_one_mechanism_per_job). Folded in as one row, carrying its OWN descriptor: the screen
    /// module is refused under COBOLNET1707 (its statement surface's code, so the negative witnesses keep
    /// naming the construct) while the A.4.14 and A.4.3 families are refused under COBOLNET1710. The
    /// descriptor also carries whether <c>--permissive</c> moves the refusal, so the row is the only place
    /// a module's posture is written down.
    /// <para>⚠ EC-MCS-* still has the SAME zero-setting-site shape and is NOT here: asynchronous messaging
    /// is Annex A.3 item 4, whose §4.2.6 licence is accept-and-WARN (COBOLNET1578), so refusing its names
    /// would be the wrong posture, not merely a missing row. It is registered work. EC-LOCALE-* was in this
    /// table until kb/Work PB64 adopted the locale module whole, at which point its row was DELETED rather
    /// than special-cased — the table's other direction working correctly.</para></para></summary>
    private static readonly DeclinedEcFamily[] DeclinedEcFamilies =
    [
        // Annex A.4.2 item 10 — the EC-SCREEN conditions in the RAISING phrases of EXIT and GOBACK, the
        // RAISING phrase of the procedure division header, the USE statement, the WHEN phrase of PERFORM,
        // RAISE and the TURN directive. Covers the level-2 EC-SCREEN and all four level-3 names. Its
        // descriptor is the SCREEN STATEMENT code (1707), not 1710: the module owns a code per division
        // surface and this is its procedure surface (kb/Work PB260).
        new("EC-SCREEN", DiagnosticCatalog.ScreenStatementUnsupported, "the screen handling module",
            "ISO Annex A.4.2 item 10; §14.9.1 / §14.9.11 / §14.9.39", "docs/CONFORMANCE.md §4 item 4"),
        // Annex A.4.14 item 10 — "EC-VALIDATE exception conditions in the RAISING phrase of the EXIT and GOBACK
        // statements, the RAISING phrase of the procedure division header, the USE statement, the WHEN phrase of
        // the PERFORM statement, the RAISE statement, and the TURN compiler directive", i.e. every site this
        // funnel serves. Covers the level-2 EC-VALIDATE and all five level-3 names (§14.6.13.1.6 Table 13, whose
        // NOTE records the whole family as obsolete).
        new("EC-VALIDATE", DiagnosticCatalog.DeclinedModuleExceptionName, "the VALIDATE facility", "ISO Annex A.4.14 item 10; §14.9.50",
            "docs/CONFORMANCE.md §4 item 3"),
        // Annex A.4.3 item 3 — the three commit-and-rollback conditions, all COBOL-2023 additions (Annex E.3.2
        // item 2). NOT a bare "EC-FLOW" prefix: EC-FLOW is a live level-2 family whose OTHER level-3 names
        // (EC-FLOW-GLOBAL-EXIT, EC-FLOW-RELEASE, EC-FLOW-RETURN, EC-FLOW-USE, …) belong to facilities this
        // compiler DOES implement — declining them would reject legal source (feedback_spec_fidelity: check
        // the siblings).
        new("EC-FLOW-APPLY-COMMIT", DiagnosticCatalog.DeclinedModuleExceptionName, "the commit and rollback facility", "ISO Annex A.4.3 item 3; §12.4.6.3",
            "docs/CONFORMANCE.md §4 item 2"),
        new("EC-FLOW-COMMIT", DiagnosticCatalog.DeclinedModuleExceptionName, "the commit and rollback facility", "ISO Annex A.4.3 item 3; §14.9.7",
            "docs/CONFORMANCE.md §4 item 2"),
        new("EC-FLOW-ROLLBACK", DiagnosticCatalog.DeclinedModuleExceptionName, "the commit and rollback facility", "ISO Annex A.4.3 item 3; §14.9.36",
            "docs/CONFORMANCE.md §4 item 2"),
    ];

    /// <summary>The declined module owning <paramref name="name"/>, or null. Prefix-matched so a family's
    /// level-2 name and every level-3 name under it resolve to one row — including the open EC-IMP suffixes
    /// (§14.6.13.1.1) a program may write for a declined module.</summary>
    private static DeclinedEcFamily? DeclinedModuleOf(string name)
    {
        foreach (var f in DeclinedEcFamilies)
            if (name.StartsWith(f.Prefix, StringComparison.OrdinalIgnoreCase)
                && (name.Length == f.Prefix.Length || name[f.Prefix.Length] == '-'))
                return f;
        return null;
    }

    /// <summary>The §15.33 width advisory (COBOLNET1636, Warning — legal source stays legal): §15.33.3 r1 fixes
    /// FUNCTION EXCEPTION-STATUS's value at 31 characters while COBOL-2023 words run to 63 (§8.3.2.1) and the
    /// §14.6.13.1.1 open-family suffixes are unbounded, so a level-3 name of 32..63 characters is LEGAL and
    /// indistinguishable from anything sharing its first 31 characters through that ONE function — checking,
    /// declarative selection and WHEN matching all use the full name (ExceptionState.LastName). The r1 width is
    /// implemented as written (EcFunctions.Status); this makes the collision visible at compile time instead of
    /// a documented-nowhere truncation (Phase-B F6). Below 2023 the word itself cannot reach here (the 31-char
    /// COBOL-2002 word limit rejects it first), so no edition guard is needed. One advisory per spelling —
    /// the same name legitimately appears at TURN + RAISE + USE in one program.</summary>
    /// <returns><see langword="false"/> when the name was REFUSED (today: the EC-SCREEN family) — every caller
    /// must treat that as an unresolved name.</returns>
    public static bool Advise(EditionContext edition, EcInfo info, string where = "exception-name")
    {
        // ⛔ EVERY DECLINED MODULE'S EXCEPTION-NAME REFUSAL LIVES HERE, NOT AT THE SIX WRITING SITES (each
        // module's Annex item names all six: the RAISING phrases of EXIT and GOBACK, the RAISING phrase of
        // the procedure division header, the USE statement, the WHEN phrase of PERFORM, RAISE, and the TURN
        // directive). Putting it in the resolution
        // funnel is what makes it cover all of them at once — and the header RAISING partition that calls Advise
        // DIRECTLY (RaisingPhrase.Partition — once two arms, kb/Work PB815) is exactly the arm a per-site check would have missed
        // (feedback_two_arm_dispatch). This is the shape the EC-LOCALE family had while A.4.9 was declined
        // (kb/Work PB100), removed when PB64 T1 claimed that module and gave its names real raise sites.
        if (DeclinedModuleOf(info.Name) is { } declined)
        {
            edition.Declined(declined.Descriptor,
                $"the exception-name {info.Name} of {declined.Facility} ({declined.Annex}), written in "
                + $"{where} — no statement in this implementation can set that condition, so checking for "
                + "it, declaring a handler for it or matching it would be unreachable, and "
                + $"{declined.Documentation} records the decline. §14.6.13.1.1 licenses raising nothing for "
                + "such a name, but not ACCEPTING it");
            return false;
        }
        if (info.Level != 3 || info.Name.Length <= 31) return true;
        if (edition.Warnings.Any(w =>
                w.Contains(DiagnosticCatalog.EcNameWiderThanStatus.Code) && w.Contains(info.Name))) return true;
        edition.Warning(DiagnosticCatalog.EcNameWiderThanStatus,
            $"exception-name {info.Name} is {info.Name.Length} characters long; FUNCTION EXCEPTION-STATUS "
            + "returns a 31-character value (ISO §15.33.3 r1), so this name and any other sharing its first 31 "
            + "characters are indistinguishable through that function (checking and declarative selection use "
            + "the full name)");
        return true;
    }
}
