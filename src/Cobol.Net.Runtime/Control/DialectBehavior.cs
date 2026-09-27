// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// ⛔ THE NAMED BEHAVIOUR CHANGES BETWEEN ISO COBOL EDITIONS — the BEHAVIOUR twin of
/// <c>CobolNet.Editions.ConstructRegistry</c> (kb/Work PB344). A registry row there answers "does this edition
/// HAVE this construct?" and produces a diagnostic; a member here answers "which edition's RULE does this
/// construct obey?" and produces an ANSWER. Both questions arise on every edition-varying feature and they are
/// NOT the same question: the source of a behaviour change is legal at every edition, so no diagnostic can
/// carry it.
/// <para>⛔ <b>THE ADMISSION TEST FOR A MEMBER</b> (kb/Work PB344, earned by a member that had to be withdrawn).
/// Annex E is INFORMATIVE and, per §E.1, lists "the substantive changes between <b>the previous COBOL
/// standard</b> and this Working Draft International Standard" — ONE prior edition, the 2014 one. An E.2 item is
/// therefore evidence about a prior edition's RULES only when it says what the prior rule STATED; where its
/// justification says instead that the previous standard "was not clear or missing processing" — i.e. the prior
/// text was SILENT, and each sub-item adds "This appears to be an error in previous standards" — there is no
/// prior behaviour to serve, silence is not a prohibition, and the 2023 rule stands at every edition. Item 19
/// a)/b) (the open-mode USE tier) failed that test and is NOT a member: the 1985 edition's own validation suite
/// requires the opposite of what a gate would have produced (NIST CCVS SQ122A/SQ136A/SQ137A/SQ138A/SQ148A).
/// Item 22 below passes it — "the rule itself stated that the first record would be retrieved".</para>
/// <para>Each member names one row of <c>docs/VERSION_CHANGE_REFERENCE.md</c> Table 1 whose action is
/// <c>gate-behavior-by-dialect</c>. The row's <c>&lt;!-- gate:… --&gt;</c> marker is this member's
/// <see cref="DialectBehaviors.Row.Id"/>, which is how the drift test
/// (<c>DialectBehaviorRegistryDriftTests</c>) proves the two registers still agree — a member with no row, or a
/// row whose marker names no member, fails the build's test gate rather than rotting.</para>
/// <para>⛔ IT LIVES IN THE RUNTIME, NOT IN <c>Cobol.Net.Editions</c>, for the same reason
/// <c>SignEncoding</c> does: BOTH sides need it. The emitter folds a behaviour that is decided at COMPILE time
/// (it knows <c>--std</c>, so a suppressed branch is never rendered), and the generated program's connectors
/// ask it at RUN time for a behaviour that is decided inside the runtime's own walk — over the edition the
/// COMPILATION baked into them (<c>FileConnector.Edition</c>). <c>Cobol.Net.Editions</c> is a leaf that the
/// runtime does not reference, so putting it there would force either a project-reference inversion or a
/// SECOND copy of the table.</para>
/// </summary>
public enum DialectBehavior
{
    /// <summary>ISO/IEC 1989:2023 §14.9.30.4 GR21 indexed rule d) 3 — "If no such record is found or PREVIOUS
    /// is specified and the previous operation on the file was an OPEN statement, the at end condition exists".
    /// 2023 CHANGED IT: Annex E.2 item 22, "READ PREVIOUS statement following an OPEN statement. Ensure that an
    /// at end condition occurs", whose justification records the PRIOR rule — "the rule itself stated that the
    /// first record would be retrieved" — so at 2002/2014 a READ PREVIOUS immediately after an OPEN makes the
    /// first existing record available, exactly as NEXT does. (The conflicting NOTE Annex E.2 also mentions is
    /// informative — this document is drafted to the ISO/IEC Directives Part 2, under which a note states no
    /// requirement — so the RULE is the prior standard's normative content.) INDEXED ONLY: the relative and
    /// sequential sub-rule blocks print rule b) unamended.</summary>
    IndexedReadPreviousAfterOpenAtEnd = 0,

    /// <summary>ISO/IEC 1989:2023 §7.3.6.3 GR2 — "The implementor shall define and document which mode of
    /// arithmetic is to be used when evaluating compile-time arithmetic." 2023 CHANGED IT: Annex E.2 item 6 says
    /// the mode "is now explicitly implementor defined", and its justification records the PRIOR rule — "The
    /// previous COBOL Standard required the use of an arithmetic mode that is no longer supported" — so it passes
    /// the admission test above. The one arithmetic mode 2023 removed is Standard Arithmetic (E.2 item 21), so at
    /// 2002 and 2014 a compile-time arithmetic expression (CONSTANT AS, <c>&gt;&gt;DEFINE</c>,
    /// <c>&gt;&gt;IF</c>/<c>&gt;&gt;EVALUATE</c>) is evaluated in standard arithmetic, and from 2023 in the
    /// implementor's documented mode (docs/CONFORMANCE.md DOC-A.1-29). The consumer is
    /// <c>CobolNet.Frontend.Expressions.CompileTimeArithmetic.For</c> — kb/Work PB1592.</summary>
    CompileTimeArithmeticImplementorDefined = 1,
}

/// <summary>
/// The <see cref="DialectBehavior"/> table and its ONE predicate. Indexed by the enum member's ordinal, so
/// <see cref="IsActive"/> is an array read — it sits inside the indexed connector's record-selection walk.
/// </summary>
public static class DialectBehaviors
{
    /// <summary>One behaviour change: the edition it took effect in, the
    /// <c>docs/VERSION_CHANGE_REFERENCE.md</c> Table 1 row that owns it, and the normative + informative
    /// citations the gate is written against.</summary>
    /// <param name="Behavior">The enum member this row describes (its ordinal IS the row's index).</param>
    /// <param name="Id">The kebab identifier, identical to the owning VCR row's <c>&lt;!-- gate:… --&gt;</c>
    /// marker.</param>
    /// <param name="ChangedIn">The FIRST edition whose rules state the NEW behaviour.</param>
    /// <param name="VcrRow">The <c>docs/VERSION_CHANGE_REFERENCE.md</c> Table 1 row number.</param>
    /// <param name="SpecRef">The normative clause the rule is written in.</param>
    /// <param name="AnnexRef">The informative Annex E item that records the change.</param>
    /// <param name="Title">A one-line statement of what changed.</param>
    public readonly record struct Row(DialectBehavior Behavior, string Id, int ChangedIn, int VcrRow,
        string SpecRef, string AnnexRef, string Title);

    private static readonly Row[] Table =
    [
        new(DialectBehavior.IndexedReadPreviousAfterOpenAtEnd, "indexed-read-previous-after-open-at-end", 2023,
            29, "14.9.30.4 GR21 d) 3", "E.2 item 22",
            "READ ... PREVIOUS immediately after an OPEN on an INDEXED file raises the at end condition; "
            + "before 2023 the rule made the first existing record available"),
        new(DialectBehavior.CompileTimeArithmeticImplementorDefined, "compile-time-arithmetic-mode", 2023,
            12, "7.3.6.3 GR2", "E.2 item 6",
            "compile-time arithmetic expressions are evaluated in the implementor-defined mode; before 2023 the "
            + "standard prescribed standard arithmetic (the mode E.2 item 21 removed)"),
    ];

    static DialectBehaviors()
    {
        // The ordinal-IS-the-index invariant this class's O(1) lookup rests on, asserted where it is authored
        // rather than only in the drift test: a member inserted mid-enum without its row moved would otherwise
        // silently answer with its neighbour's edition.
        for (int i = 0; i < Table.Length; i++)
            if ((int)Table[i].Behavior != i)
                throw new InvalidOperationException(
                    $"DialectBehaviors.Table[{i}] describes {Table[i].Behavior}; the table is ordinal-indexed.");
        if (Table.Length != Enum.GetValues<DialectBehavior>().Length)
            throw new InvalidOperationException("DialectBehaviors.Table does not cover every DialectBehavior.");
    }

    /// <summary>Every behaviour change, in enum order — the drift test's and the documentation generators'
    /// enumeration. There is no second list.</summary>
    public static ReadOnlySpan<Row> All => Table;

    /// <summary>The row describing <paramref name="behavior"/>.</summary>
    public static Row Of(DialectBehavior behavior) => Table[(int)behavior];

    /// <summary>⛔ THE ONE PREDICATE: does the edition targeted by this compilation state the NEW behaviour?
    /// <paramref name="editionYear"/> is the ISO edition the program was compiled for (the CLI's <c>--std</c>,
    /// 85 / 2002 / 2014 / 2023). Every per-edition behaviour gate — emitter-side or runtime-side — asks this
    /// and never an inline year comparison, so the edition a change landed in is written down exactly once.</summary>
    public static bool IsActive(DialectBehavior behavior, int editionYear) =>
        editionYear >= Table[(int)behavior].ChangedIn;
}
