// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// WHERE each pushable directive's state lives — the accounting ISO §7.3.22.4 GR2 demands ("the state of all of
/// the directives other than EVALUATE, IF, PAGE, POP, or PUSH are saved"), kept as data so that it is checked
/// rather than remembered (kb/Work PB941).
///
/// <para>One entry per <see cref="CompilerDirectiveCatalog.PushableRows"/> row. A CARRIED entry names the stage
/// types that hold the state (each runs a <see cref="DirectiveStateStack"/> and registers a carrier for the row)
/// and the <see cref="DirectiveResults"/> members the state reaches the binder through; an uncarried entry states,
/// with its citation, why this compiler holds no state for a PUSH to save. <c>DirectiveStateRegistryDriftTests</c>
/// fails when a pushable row is missing here, when a carrier names a type that does not exist, when a
/// <see cref="DirectiveResults"/> member is claimed by no entry, and when a carried row has no PUSH/POP
/// behavioural case — so the next directive to gain state cannot silently escape PUSH/POP the way every one of
/// them did before PB941.</para>
/// </summary>
public static class DirectiveStateRegistry
{
    /// <summary>One pushable directive's disposition.</summary>
    /// <param name="Row">The <c>constructs.json</c> row id.</param>
    /// <param name="Carriers">The stage types holding the state; empty when none does.</param>
    /// <param name="Products">The <see cref="DirectiveResults"/> members carrying the state onward.</param>
    /// <param name="Reason">Where the state lives, or — for an uncarried row — why there is none, with the rule.</param>
    public sealed record Entry(string Row, IReadOnlyList<string> Carriers, IReadOnlyList<string> Products, string Reason)
    {
        /// <summary>True when some stage holds this directive's state.</summary>
        public bool IsCarried => Carriers.Count > 0;
    }

    /// <summary>Every pushable directive, carried or not.</summary>
    public static IReadOnlyList<Entry> Entries { get; } =
    [
        new(Constructs.SourceFormatDirective2002, [nameof(ReferenceFormatProcessor)], [],
            "The reference format of the following text (§7.3.24.3 GR1) — the normalizer's running segment format, which "
            + "§14.9.28.4 GR14's implicit PUSH ALL / POP ALL reach through ImplicitFormatOps (kb/Work PB1066)."),
        new(Constructs.DefineDirective2002, [nameof(ConditionalCompilationProcessor)],
            [nameof(DirectiveResults.CompilationVariables)],
            "The compilation-variable table — every instance at once (§7.3.22.4 GR3, §7.3.20.4 GR1: \"all instances "
            + "of that directive are restored\") — held by the conditional-compilation driver for the directives it "
            + "evaluates, and carried onward as a timeline of DEFINE events for the uses §7.3.11.4 GR1 permits outside "
            + "conditional compilation: a constant entry's FROM phrase and a later directive's literal slot (kb/Work PB1368)."),
        new(Constructs.TurnDirective2002, [nameof(TurnDirectiveProcessor)], [nameof(DirectiveResults.TurnEvents)],
            "The exception-checking toggles (§7.3.25.4), folded by the binder's TurnState."),
        new(Constructs.RefModZeroLength2023, [nameof(RefModZeroLengthDirectiveProcessor)],
            [nameof(DirectiveResults.RefModZeroLengthEvents)],
            "The zero-length allowance toggles (§7.3.23.3), folded by the binder's RefModZeroLengthState."),
        new(Constructs.Flag02Directive2014, [nameof(FlagDirectiveProcessor), nameof(ConditionalCompilationProcessor)],
            [nameof(DirectiveResults.FlagEvents)],
            "The FLAG-02 option toggles (§7.3.14.4) — bound options folded by FlagState, frontend-inline options by "
            + "the conditional-compilation driver's running scan."),
        new(Constructs.Flag14Directive2023, [nameof(FlagDirectiveProcessor), nameof(ConditionalCompilationProcessor)],
            [nameof(DirectiveResults.FlagEvents)],
            "The FLAG-14 option toggles (§7.3.15.4) — bound options folded by FlagState, frontend-inline options by "
            + "the conditional-compilation driver's running scan."),
        new(Constructs.CobolWordsDirective2023, [nameof(CobolWordsDirectiveProcessor)],
            [nameof(DirectiveResults.CobolWordsMap)],
            "The word-table modifications in effect for the group; §7.3.10.3 SR1 confines the directive to before "
            + "the first IDENTIFICATION DIVISION, so the state is the one in effect there."),
        new(Constructs.LeapSecondDirective2002, [nameof(LeapSecondDirectiveProcessor)],
            [nameof(DirectiveResults.LeapSecondEvents)],
            "The ON/OFF toggles (§7.3.17.4), folded per compilation unit at its first line by the binder's "
            + "LeapSecondState (§7.3.4 GR5; kb/Work PB1378) — §7.3.17.3 SR1 keeps the directive out of every unit, "
            + "so the fold at a unit's first line is the state its whole text reads."),
        new(Constructs.CallConventionDirective2002, [], [],
            "NONE HELD: WiseOwl COBOL defines no call-convention-name (DOC-A.1-68), so COBOL — the §7.3.9.3 GR1 "
            + "default — is the only operand the directive's row admits (any other word is COBOLNET1911, kb/Work "
            + "PB1383), every name is processed under GR2 a), and no call-convention state exists for a PUSH to save."),
        new(Constructs.ListingDirective2002, [], [],
            "NONE HELD: §7.3.18.3 GR1 — \"If the compiler does not produce a source listing, the LISTING directive "
            + "shall be ignored\", and this compiler produces none."),
        new(Constructs.DisplayDirective2023, [], [],
            "NONE HELD: the DISPLAY directive has no state — §7.3.12.4 GR1 transfers its operands when it is "
            + "processed, and nothing persists past its own line."),
        new(Constructs.PropagateDirective2002, [nameof(PropagateDirectiveProcessor)],
            [nameof(DirectiveResults.PropagateEvents)],
            "The automatic-propagation toggles (§7.3.21.4 GR1/GR3), folded per source element by the binder's "
            + "PropagateState (kb/Work PB1119)."),
        new(Constructs.Flag85DirectiveWindow, [], [],
            "NONE HELD: removed at 2023 (Annex E.2 item 21) while PUSH/POP exist only at 2023, so no source may "
            + "contain both."),
        new(Constructs.FlagNativeArithmeticDirectiveWindow, [], [],
            "NONE HELD: removed at 2023 (Annex E.2 item 21) while PUSH/POP exist only at 2023, so no source may "
            + "contain both."),
    ];

    /// <summary>The <see cref="DirectiveResults"/> members that are NOT directive state — the position-ruled
    /// directive sites (§7.3.20.3 SR4 / §7.3.22.3 SR4 / §7.3.25.3 SR5) record where a directive was written.</summary>
    public static IReadOnlyList<string> NotState { get; } = [nameof(DirectiveResults.DirectiveSites)];

    private static readonly IReadOnlySet<string> Carried =
        Entries.Where(e => e.IsCarried).Select(e => e.Row).ToHashSet(StringComparer.Ordinal);

    /// <summary>True when <paramref name="row"/> is declared carried.</summary>
    public static bool IsCarried(string row) => Carried.Contains(row);
}
