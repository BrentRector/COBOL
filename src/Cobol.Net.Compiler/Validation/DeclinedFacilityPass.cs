// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;              // EditionContext
using CobolNet.Binding.Passes;       // GroupBindContext
using CobolNet.Editions.Diagnostics; // DiagnosticCatalog
using CobolNet.Frontend.Generated;   // CobolParserCore
using CobolNet.Runtime;

namespace CobolNet.Validation;

/// <summary>
/// THE ONE PLACE A DECLINED ANNEX-A.4 OPTIONAL ELEMENT IS REFUSED BY NAME (COBOLNET1708 / COBOLNET1709) — a
/// sibling to <see cref="VersionConformancePass"/> and <see cref="FlagConformancePass"/>, run right after them
/// from <c>BinderDriver</c>.
///
/// <para>WHY A SEPARATE PASS AND NOT AN ARM OF THE VERSION PASS. Edition gating answers "does this construct
/// exist in the edition you targeted"; this answers "does this implementation claim support for this optional
/// module". They are orthogonal — an A.4 decline fires at EVERY edition the element exists in, never depends on
/// <c>--std</c> beyond that, and its severity comes from the strict/permissive seam rather than from
/// <see cref="Editions.ConstructAvailability"/>. Folding it into <c>VersionConformancePass</c> would give that
/// pass two answers to two different questions, which is the shape
/// <c>docs/rearchitecture/DESIGN-version-conformance-pipeline.md</c> exists to prevent (its ParseArm reports
/// through <c>ConstructRegistry.Check</c> and nothing else).</para>
///
/// <para>WHY A PARSE-TREE WALK AND NOT A BINDER HOOK. A declined clause has no bound node — that is what
/// declining it means — so there is no binder arm to hang it on, and the binder's own entry paths would drop it
/// silently in exactly the places it matters most: <c>DataBinder.BindEntry</c> returns early for levels 66 and
/// 88, and <c>BindCondition</c> returns early for an UNNAMED level-88, which is precisely the §13.16.2 Format-4
/// validation entry (<c>88 [condition-name-2] value-clause .</c>). A walk over the parse tree sees the written
/// syntax whether or not anything binds it.</para>
///
/// <para>⛔ ADDING A DECLINED CONSTRUCT: add its rule to <c>Grammar/Core/CobolDeclined.g4</c> and, if it is an
/// ENTRY POINT there (a rule no other rule in that file references), a <c>VisitXxx</c> override here.
/// <c>DeclinedFacilityDriftTests</c> derives that obligation FROM the grammar file, so a new entry-point rule
/// with no override fails the build rather than parsing into silence — the failure mode a hand-maintained list
/// would have re-created. Alternatives added UNDER <c>validationClause</c> need no code at all: the message
/// names the clause from its own leading keywords.</para>
///
/// <para>UNDER <c>--permissive</c> these downgrade to warnings through the ONE declined-element seam
/// (<c>EditionContext.Declined</c>, whose <c>DiagnosticDescriptor.PermissiveInert</c> fact says so) —
/// the same seam every
/// removed construct uses, and the program compiles with the declined element simply ABSENT: a level-88 whose
/// only content was the format-5 VALID phrase keeps its ordinary format-3 reading, an unnamed format-4 entry
/// contributes nothing, and an <c>APPLY COMMIT</c> clause is ignored — which is the truthful outcome, because
/// there is no transaction manager for it to configure. That is the migration mode's documented bargain, not a
/// silent wrong answer: the warning names the facility at every site.</para>
/// </summary>
internal sealed class DeclinedFacilityPass(EditionContext edition) : CursorFollowingVisitor(edition)
{
    private readonly EditionContext _edition = edition;

    /// <summary>Words that lead a clause but carry no identity — §5.2.3 optional words, not underlined in the
    /// printed general formats. Dropped when composing the clause's name from its own leading keywords, so
    /// <c>DEFAULT IS "AB"</c> is reported as the DEFAULT clause rather than the "DEFAULT IS" clause.</summary>
    private static readonly HashSet<string> Connectives =
        new(CobolNames.Comparer) { "IS", "ARE" };

    public static void Run(GroupBindContext group, EditionContext edition)
    {
        // Below COBOL-2002 no declined-facility node can exist: every rule in CobolDeclined.g4 is behind an
        // {is2002()}? / {is2023()}? left-edge predicate, because its keywords are user-defined words at COBOL-85
        // (§8.9). Skipping the walk there is not an optimization that could hide a diagnostic — it is the same
        // fact the predicates state, and it keeps a pure COBOL-85 compile byte-for-byte free of this pass.
        if (edition.DialectLevel < 2002) return;
        new DeclinedFacilityPass(edition).VisitPositioned(group.Tree);
    }

    /// <summary>Do not descend into a PROCEDURE DIVISION: every declined construct this pass refuses is a DATA
    /// DIVISION clause or an ENVIRONMENT DIVISION (I-O-CONTROL) clause, so the procedure body — much the largest
    /// part of a real compilation unit — cannot contain one. The declined STATEMENTS (VALIDATE §14.9.50, COMMIT
    /// §14.9.7, ROLLBACK §14.9.36, MCS SEND/RECEIVE) are recognized-and-named at BIND instead
    /// (<c>StatementBinder.BindUnsupportedFacility</c>, the recognize-and-name warning band — mandated by
    /// §4.2.6 ¶3 for the Annex A.3 halves, MCS and COMMIT/ROLLBACK, and a documented §4.2.7 choice for the
    /// Annex A.4.14 VALIDATE statement; kb/Work PB709), because they DO have a bound
    /// node — an inert one — and the program is expected to keep running.
    /// <para>⚠ VERIFIED AGAINST THE GRAMMAR, not assumed, because the skip would be a SILENT hole if any data
    /// division hung below a procedure division. It does not: <c>nestedProgram*</c> is a SIBLING of
    /// <c>procedureDivision</c> in <c>programUnit</c>, not a child; a METHOD's <c>dataDivision?</c> is a
    /// sibling of its <c>procedureDivision?</c> inside <c>methodDefinition</c>; and the FACTORY / OBJECT
    /// paragraphs spell their method list with the LITERAL tokens <c>PROCEDURE DIVISION DOT
    /// methodDefinition*</c> rather than the <c>procedureDivision</c> rule, so this override never fires
    /// there. <c>conformance:negative/oo-multi-base-super</c> and the OO corpus exercise the method path.</para></summary>
    public override object? VisitProcedureDivision(CobolParserCore.ProcedureDivisionContext ctx) => null;

    /// <summary>The rule contexts this pass REFUSES WHOLE — exactly the entry-point rules of
    /// <c>Grammar/Core/CobolDeclined.g4</c>, each with its <c>VisitXxx</c> override below.
    /// <c>DeclinedFacilityDriftTests.TheDeclinedRoots_AreTheGrammarsEntryPoints</c> derives the same set from the
    /// grammar file, so a new declined construct cannot join the grammar without joining this set.</summary>
    internal static readonly IReadOnlySet<Type> DeclinedConstructRoots = new HashSet<Type>
    {
        typeof(CobolParserCore.ValidationClauseContext),
        typeof(CobolParserCore.ValidateValidPhraseContext),
        typeof(CobolParserCore.ApplyCommitClauseContext),
    };

    /// <summary>The declined construct enclosing <paramref name="node"/>, or null. ⛔ THE ONE ANSWER to "is this
    /// syntax inside a construct refused whole?", for any other pass that must not diagnose inside one — today
    /// the §8.9 funnel (<c>VersionConformancePass.VisitCobolWord</c>; kb/Work PB764 moved it here from a private
    /// ancestor walk there). There is no syntax of OURS inside a declined A.4 construct to diagnose: §4.2.7 makes
    /// the element one an implementation "may, but need not, implement", and A.4.1 admits its syntax "only when
    /// support … is claimed" — so one diagnostic per declined construct (the override below, which does not
    /// descend) is the whole posture. The §13.18.62.2 ON group's FORMAT / CONTENT / RELATION were the sharpest
    /// case: the funnel printed "'FORMAT' is a reserved word … cannot be used as a user-defined word" beside
    /// COBOLNET1708, a false statement next to the true one.
    /// <para>⛔ NOT §4.2.6: that clause's "not required to diagnose syntax errors within this unsupported syntax"
    /// is written of Annex A.3 PROCESSOR-DEPENDENT elements, and these rules are Annex A.4 optional ones (kb/Work
    /// PB709).</para></summary>
    internal static Antlr4.Runtime.RuleContext? EnclosingDeclinedConstruct(Antlr4.Runtime.RuleContext node)
    {
        for (var a = node.Parent; a is not null; a = a.Parent)
            if (DeclinedConstructRoots.Contains(a.GetType())) return a;
        return null;
    }

    /// <summary>The §13.16.2 "validation-clauses" group of the DECLINED A.4.14 VALIDATE facility: CLASS
    /// (§13.18.11), DEFAULT (§13.18.17), DESTINATION (§13.18.18), INVALID (§13.18.31), PRESENT WHEN format 2
    /// (§13.18.41), VARYING's validation leg (§13.18.64) and VALIDATE-STATUS / VAL-STATUS (§13.18.62).
    /// <para>⚠ CLASS is the one the annex does not list. It joins the group by owner decision (kb/Work PB375,
    /// 2026-09-02) on §13.16.2's own ground — the printed Format-1 validation-clauses block opens with
    /// <c>[ class-clause ]</c> and maps it to "13.18.11, CLASS clause" — and because §13.18.11.1 gives the
    /// clause no content outside the module. It needed no arm here, which is the derived namer working.</para>
    /// <para>⚠ PRESENT WHEN and VARYING are SHARED with report writer, which IS supported (Annex A.4.11 items 14
    /// and 20; <c>docs/CONFORMANCE.md</c> §5 records report writer as Partial with both implemented). The two
    /// legs are told apart by WHERE the clause is written, not by how — so the report-writer forms have their
    /// own grammar rules (<c>reportPresentWhenClause</c> / <c>reportVaryingClause</c>) and never reach here.
    /// A change that made this arm reachable from a report group description entry would silently decline a
    /// claimed facility; <c>conformance:2023/declined_rw_present_varying_control</c> is the witness that it has
    /// not.</para></summary>
    public override object? VisitValidationClause(CobolParserCore.ValidationClauseContext ctx)
    {
        _edition.Declined(DiagnosticCatalog.ValidateDataDivisionClauseUnsupported,
            $"the {ClauseName(ctx)} clause of the §13.16.2 validation-clauses group");
        return null;   // nothing below a declined A.4 clause is diagnosed (§4.2.7 / A.4.1: the element's
                       // syntax is not this implementation's) — one diagnostic per clause
    }

    /// <summary>The §13.18.63.2 Format-5 content-validation entry's own tail — <c>[IS|ARE] {INVALID|VALID}
    /// [WHEN condition-1]</c> — which is what turns an ordinary level-88 VALUE list into the A.4.14 item-7
    /// construct. Its own rule rather than a <c>validationClause</c> alternative because it is a PHRASE of the
    /// VALUE clause, whose formats 1–4 are fully supported: only this tail is declined.</summary>
    public override object? VisitValidateValidPhrase(CobolParserCore.ValidateValidPhraseContext ctx)
    {
        _edition.Declined(DiagnosticCatalog.ValidateDataDivisionClauseUnsupported,
            "the VALUE clause's content-validation entry (ISO §13.18.63 format 5 — the "
            + $"'{(ctx.VALID() is not null ? "VALID" : "INVALID")}' phrase; §13.16.2 format 4, Annex A.4.14 "
            + "item 7); the ordinary condition-name VALUE formats 1, 3 and 4 are unaffected");
        return null;
    }

    /// <summary>The I-O-CONTROL APPLY COMMIT clause (ISO §12.4.6.3) — Annex A.4.3 item 2, the DECLINED commit
    /// and rollback facility's declaration half. Refusing it is what keeps the rest of that decline coherent:
    /// with no clause ever accepted, no APPLY COMMIT clause is ever ACTIVE, which is exactly the state
    /// §14.9.7.4 GR1 / §14.9.36.4 GR1 give COMMIT and ROLLBACK their CONTINUE behaviour in — the behaviour
    /// <c>conformance:2023/pb137_commit_inert</c> already pins.</summary>
    public override object? VisitApplyCommitClause(CobolParserCore.ApplyCommitClauseContext ctx)
    {
        _edition.Declined(DiagnosticCatalog.ApplyCommitClauseUnsupported,
            "the I-O-CONTROL APPLY COMMIT clause (ISO §12.4.6.3; Annex A.4.3 item 2) — the COMMIT and ROLLBACK "
            + "statements themselves are accepted and behave as CONTINUE (COBOLNET1579; §14.9.7.4 GR1 / "
            + "§14.9.36.4 GR1), which is precisely what having no active APPLY COMMIT clause means");
        return null;
    }

    /// <summary>Name the clause from its OWN leading keywords — the terminals before the first sub-rule, minus
    /// the §5.2.3 optional connectives. Derived rather than switched so a new alternative added to
    /// <c>validationClause</c> is named correctly with no code change here (CLAUDE.md rule 5: prefer the shape
    /// that makes the next case automatic). Yields CLASS · DEFAULT · DESTINATION · INVALID WHEN · PRESENT WHEN ·
    /// VARYING · VALIDATE-STATUS · VAL-STATUS.
    /// <para>⛔ THE GRAMMAR CONTRACT THIS RESTS ON, stated because it is invisible from here and was first
    /// tested by the CLASS clause: an alternative of <c>validationClause</c> must put its OPERANDS in a
    /// sub-rule, never inline as terminals. Every alternative before CLASS took operands that were already
    /// sub-rules (<c>literal</c>, <c>dataReference</c>, <c>condition</c>), so the contract had never been
    /// exercised; CLASS's operands are the reserved words NUMERIC / ALPHABETIC / ALPHABETIC-LOWER /
    /// ALPHABETIC-UPPER, and inlining them would have produced "the CLASS NUMERIC clause" — a diagnostic that
    /// renames the clause after whatever the user wrote. <c>validateClassOperand</c> exists for that reason and
    /// <c>DeclinedFacilityTests.ClassClause_IsNamedByItsClauseWord_NotByItsOperand</c> makes the contract
    /// fail loudly rather than silently.</para></summary>
    private static string ClauseName(CobolParserCore.ValidationClauseContext ctx)
    {
        var words = new List<string>();
        // The single child is the matched alternative's own context; its leading terminals are the keywords.
        var alt = ctx.GetChild(0);
        for (int i = 0; i < alt.ChildCount; i++)
        {
            if (alt.GetChild(i) is not Antlr4.Runtime.Tree.ITerminalNode t) break;
            string w = t.GetText().ToUpperInvariant();
            if (!Connectives.Contains(w)) words.Add(w);
        }
        return words.Count > 0 ? string.Join(' ', words) : alt.GetText().ToUpperInvariant();
    }
}
