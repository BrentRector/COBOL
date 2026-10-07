// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Binding.Bound;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The PERFORM statement's FORMAT half: the inline head's cardinality (§14.9.28.2 Formats 2 and 3) and the
/// varying-phrase's operand brace group (§14.9.28.2 varying-phrase + §14.9.28.3 SR3/SR4/SR5/SR6).
///
/// <para>⛔ THE OPERAND'S KIND IS DECIDED EXACTLY ONCE, HERE. Every rule about a varying-phrase operand keys on
/// the same three-way question the printed format asks — is this slot holding a LITERAL, an INDEX-NAME or an
/// IDENTIFIER? — so the answer is computed where the operand is bound (<see cref="VaryingOperand"/>) and carried
/// on the bound level (<see cref="VaryingOperandKind"/>). Before this, SR4/SR5/SR6 were absent entirely and the
/// §14.9.28.4 GR3 guard re-derived the kind in the EMITTER from a bound-node C# type, which is one identifier
/// SHAPE and not the rule's question (kb/Work PB432, PB439).</para>
/// </summary>
internal sealed partial class ControlFlowBinder
{
    // ── The inline head's cardinality (kb/Work PB431) ───────────────────────────────────────────────────────

    /// <summary>§14.9.28.2 Formats 2 and 3, rendered from the printed page 683 / PDF 712: Format 2 is
    /// <c>PERFORM [ times-phrase | until-phrase | varying-phrase ] imperative-statement-1 END-PERFORM</c> — ONE
    /// pair of square brackets over three STACKED alternatives, so at most ONE loop-control phrase — and Format 3
    /// prints <b>no</b> loop-control phrase at all (its head is <c>[ WITH LOCATION ]</c>).
    /// <para>The grammar keeps the head a LIST on the project's superset posture, so both violations arrive here
    /// as something the binder can NAME. Before this screen, a second phrase was silently deleted by
    /// <c>BindPerformControl</c>'s <c>FirstOrDefault()</c> and a Format-3 head's phrases by the Format-3 binder
    /// never asking for one: <c>PERFORM 3 TIMES UNTIL X &gt; 100</c> ran three times, <c>PERFORM UNTIL X &gt; 4
    /// 3 TIMES</c> ran five (the FIRST phrase always won), and <c>PERFORM 3 TIMES … WHEN EC-ALL …</c> ran its
    /// body ONCE — every one of them with no diagnostic at any <c>--std</c>.</para>
    /// <para>⛔ WHY AN ERROR AND NOT THE §4.2.2 WARNING — the COBOLNET1970 reading, which this follows: §4.2.2's
    /// second paragraph obliges an implementation to be ABLE to "indicate violations of the general formats",
    /// which is the floor; its FIRST paragraph fixes what may be ACCEPTED — "An implementation shall accept the
    /// syntax and provide the functionality for all standard language elements required by this Working Draft
    /// International Standard and the optional or processor-dependent language elements for which support is
    /// claimed" — and a second loop-control phrase is neither. Refused at every edition and every strictness:
    /// nothing in the format changed across 1985/2002/2014/2023 (Format 3 is 2023-only and gated separately by
    /// COBOLNET0900).</para></summary>
    private void CheckInlineHeadCardinality(Core.PerformStatementContext p)
    {
        if (p.performInlineHead() is not { } head) return;
        var options = head.performOptions();
        if (options.Length == 0) return;   // a [WITH] LOCATION head — Format 3's own, screened by COBOLNET1597

        if (PerformFormat.IsFormat3(p))
        {
            ctx.Edition.Error(DiagnosticCatalog.PerformFormat3LoopControlPhrase,
                $"an exception-checking (Format-3) PERFORM carries the loop-control phrase "
                + $"'{PhraseName(options[0])}', and ISO §14.9.28.2 Format 3 prints none — its head is "
                + "[ WITH LOCATION ] and nothing else. Write the loop as an ordinary inline PERFORM and put the "
                + "exception-checking PERFORM inside it, or the other way round");
            return;
        }
        if (options.Length > 1)
            ctx.Edition.Error(DiagnosticCatalog.PerformInlineHeadMultipleControlPhrases,
                $"an inline PERFORM carries {options.Length} loop-control phrases "
                + $"({string.Join(", ", options.Select(PhraseName))}), and ISO §14.9.28.2 Format 2 prints ONE "
                + "bracket over the three stacked alternatives times-phrase / until-phrase / varying-phrase, so "
                + "at most one may be specified. A VARYING phrase already carries its own UNTIL");
    }

    /// <summary>The phrase's own name as the general format prints it, for the cardinality diagnostic.</summary>
    private static string PhraseName(Core.PerformOptionsContext o) =>
        o.performTimes() is not null ? "times-phrase"
        : o.performUntil() is not null ? "until-phrase"
        : "varying-phrase";

    // ── The varying phrase (kb/Work PB432, PB437, PB439) ────────────────────────────────────────────────────

    /// <summary>Bind a VARYING phrase (ISO §14.9.28, the VARYING phrase of Formats 1 and 2 — §14.9.28.4
    /// GR12/GR13; §14.9.28.2 prints three formats and none of them is "VARYING") into its ordered induction
    /// levels — the VARYING level first, then each AFTER level left-to-right. TEST AFTER is the phrase's own
    /// <c>TEST AFTER</c> (the AFTER tokens of the after-levels live in their sub-contexts, not here).</summary>
    private BoundPerformControl BindVarying(Core.PerformVaryingContext v)
    {
        var levels = new List<VaryingLevel>();
        if (BindVaryingLevel(v.dataReference(), v.valueOperand(), v.performUntilTarget(), firstLevel: true) is not { } head)
            return Unsupported($"PERFORM VARYING induction variable '{v.dataReference().GetText()}'");
        levels.Add(head);
        foreach (var a in v.performVaryingAfter())
        {
            if (BindVaryingLevel(a.dataReference(), a.valueOperand(), a.performUntilTarget(), firstLevel: false) is not { } level)
                return Unsupported($"PERFORM VARYING AFTER induction variable '{a.dataReference().GetText()}'");
            levels.Add(level);
        }
        // §14.9.28.4 GR3: an index-name varied/AFTER from an IDENTIFIER FROM whose value is non-positive raises
        // the fatal EC-RANGE-PERFORM-VARYING. Capture the enable flag NOW (F10/V3 template) so the emitter keeps
        // the directive-free output byte-identical (no check emitted when off).
        bool checkIndexRange = ctx.EcState.Turn.Enabled("EC-RANGE-PERFORM-VARYING", null, v.Start.Line);
        return new PerformVarying(levels, v.TEST() is not null && v.AFTER() is not null, checkIndexRange);
    }

    /// <summary>A varying-phrase operand POSITION. It is an enum and not a bool because the position IS the rule:
    /// the §14.9.28.3 operand rules and the §14.9.28.4 GR12 evaluation WINDOW are both functions of it, so a new
    /// position gets both answers by being added here rather than by someone remembering two call sites.</summary>
    private enum VaryingSlot
    {
        /// <summary>The VARYING level's FROM — identifier-3 / index-name-2 / literal-1.</summary>
        FirstFrom,
        /// <summary>An AFTER level's FROM — identifier-6 / index-name-4 / literal-3.</summary>
        AfterFrom,
        /// <summary>Any level's BY — identifier-4 / literal-2, identifier-7 / literal-4.</summary>
        By,
    }

    /// <summary>⛔ THE EVALUATION WINDOW OF EACH VARYING-PHRASE OPERAND, WRITTEN ONCE (kb/Work PB437). ISO
    /// §14.9.28.4 GR12 states it as one sentence: "Item identification for identifier-3, identifier-4,
    /// identifier-6, identifier-7, index-name-2, and index-name-4 is done each time the content of the data item
    /// referenced by the identifier or the index referenced by the index-name is used in a SETTING or AUGMENTING
    /// operation" — so every FROM and every BY is re-read per OPERATION, and a function reference in one
    /// activates per operation (§8.4.3.2.4 GR1/GR6a).
    /// <para>The FIRST level's FROM is the one slot whose setting operation happens exactly ONCE per execution of
    /// the statement — GR13 a) (TEST BEFORE) and GR13 b) (TEST AFTER) both initialize it before any transfer of
    /// control and never again — so the statement-scoped hoist is EXACT for it and it alone. Every other slot
    /// (an AFTER level's FROM, re-initialized on each outer augment by GR13 e) 2 a. / GR13 c) 4; any BY, read at
    /// every augment) must carry its activations to the site. <c>PerformVaryingOperandWindowTests</c> is the
    /// drift test that re-derives this table from GR12's sentence.</para></summary>
    private static bool IsPerEvaluationWindow(VaryingSlot slot) => slot is not VaryingSlot.FirstFrom;

    /// <summary>One varying-phrase operand, classified against the brace group the general format prints.</summary>
    /// <param name="Kind">Which alternative of the brace group this operand is (or <c>Invalid</c>).</param>
    /// <param name="Expr">The bound operand, with its per-evaluation activations attached where the slot's
    /// window requires them.</param>
    /// <param name="Literal">The literal's value when <paramref name="Kind"/> is <c>Literal</c> and the text
    /// parses — SR4 b) ("a positive integer"), SR4 c) ("a nonzero integer") and SR6 ("shall not be zero") are
    /// value tests, and they are decidable entirely at compile time because they constrain a LITERAL.</param>
    /// <param name="IsInteger">The §5.5 2) integer test, through the ONE classifier
    /// (<see cref="IntrinsicResultType.IsIntegerOperand(BoundExpr)"/>).</param>
    /// <param name="Text">The operand as written, for the diagnostics.</param>
    /// <param name="Present">False for an OMITTED BY phrase — §14.9.28.4 GR12 "For any BY phrase that is
    /// omitted, the augment value is 1", and the SR4 c) / SR5 c) / SR6 rules all speak of "the literal in the BY
    /// phrase", which an omitted phrase does not have.</param>
    private readonly record struct VaryingOperand(
        VaryingOperandKind Kind, BoundExpr Expr, decimal? Literal, bool IsInteger, string Text, bool Present = true);

    /// <summary>The omitted BY phrase: augment value 1 (§14.9.28.4 GR12), subject to no operand rule.</summary>
    private static readonly VaryingOperand OmittedBy =
        new(VaryingOperandKind.Literal, new BoundNumLiteral("1"), 1m, true, "1", Present: false);

    /// <summary>One induction level: the variable is a SET-style target (index-name or data item); the operand
    /// array is [FROM] or [FROM, BY].</summary>
    private VaryingLevel? BindVaryingLevel(
        Core.DataReferenceContext dref, Core.ValueOperandContext[] ops, Core.PerformUntilTargetContext until,
        bool firstLevel)
    {
        if (host.Set.SetTargetOf(dref) is not { } var) return null;
        // §14.9.28.3 SR2 — "Each identifier shall reference a numeric elementary item described in the data
        // division": the varied identifier is a class-closed position of the ONE operand-class screen. A PIC X
        // induction variable used to compile and throw at run time.
        if (var is SetPlaceTarget { Place: var vp })
            OperandClassScreen.Screen(ctx.Edition, OperandPositions.PerformVaryingIdentifier, vp, dref.GetText());
        var from = BindVaryingOperand(ops[0], firstLevel ? VaryingSlot.FirstFrom : VaryingSlot.AfterFrom);
        var by = ops.Length > 1 ? BindVaryingOperand(ops[1], VaryingSlot.By) : OmittedBy;
        CheckVaryingOperandRules(dref, var, from, by);
        var untilMark = host.Udf.Mark;
        // The varying-phrase prints `UNTIL condition-1` / `UNTIL condition-2`: an EXIT here is the superset
        // spelling CheckUntilExitPlacement has already refused under §14.9.28.3 SR8 (COBOLNET2954), so the level
        // carries a refused condition rather than a loop the program never asked for.
        var cond = until.condition() is { } c
            ? host.Cond.BindCondition(c)
            : host.Cond.Refused("an UNTIL EXIT in the varying-phrase (ISO §14.9.28.3 SR8)");
        return new VaryingLevel(var, from.Expr, by.Expr, host.Udf.UdfAttachPerEvaluation(cond, untilMark), from.Kind);
    }

    /// <summary>Bind ONE operand of the varying phrase and classify it against the brace group §14.9.28.2 prints.
    /// <para>The slot is parsed as <c>valueOperand</c> (a superset of the group) and narrowed HERE, so a shape
    /// the group does not print is named rather than becoming a bare parse error — and so the figurative ZERO
    /// §8.3.3.6.3 SR1 permits (SR3 restricts every literal here to numeric, which is that rule's precondition)
    /// reaches the ONE numeric-context literal reading instead of failing to parse at all.</para></summary>
    private VaryingOperand BindVaryingOperand(Core.ValueOperandContext op, VaryingSlot slot)
    {
        var mark = host.Udf.Mark;
        // §14.9.28.3 SR3 is the rule that closes THIS operand list, so a non-numeric literal is sent to it
        // rather than to §8.8.1.1 alone. PERFORM VARYING is a §13.18.38.3 r7 index-name window (kb/Work R29) but
        // not on §13.18.60.3 SR10's index-data-item list, and §14.9.28.3 SR2 wants a numeric item (kb/Work PB215).
        BoundExpr e = host.Expr.BindIndexNameWindowOperandExpr(op,
            "\"Each literal shall be numeric\" (ISO §14.9.28.3 SR3)");
        // GR12's window, applied before anything else can read the operand: an operand whose setting/augmenting
        // operation repeats carries its function activations to that operation (kb/Work PB437).
        if (IsPerEvaluationWindow(slot)) e = host.Udf.UdfAttachPerEvaluation(e, mark);

        string text = op.GetText();
        var (kind, literal) = Classify(e);
        if (kind is VaryingOperandKind.Invalid && e is not BoundExprError)
            ctx.Edition.Error(DiagnosticCatalog.PerformVaryingOperandShape,
                $"'{text}' is not one of the operands ISO §14.9.28.2's varying-phrase prints in this slot — "
                + (slot is VaryingSlot.By
                    ? "the BY phrase's brace group is { identifier-4 | literal-2 }"
                    : "the FROM phrase's brace group is { identifier-3 | index-name-2 | literal-1 }")
                + ". An arithmetic expression is not admitted here; compute it into a data item first");
        return new VaryingOperand(kind, e, literal, IntrinsicResultType.IsIntegerOperand(e), text);
    }

    /// <summary>The bound operand → its §14.9.28.2 brace-group kind, plus a literal's value.
    /// <para>⛔ IT IS A TOTAL CLASSIFICATION OF THE OPERAND, NOT A LIST OF THE SHAPES SOMEONE HAPPENED TO THINK
    /// OF. Only the COMPOSITE shapes are Invalid: an identifier is EVERY §8.4.3.1.2 identifier format — a
    /// (qualified, subscripted, reference-modified) data reference, a function-identifier, a user-function
    /// result, a special register — so a new reference node is an identifier automatically, which is precisely
    /// what the <c>is BoundNumRef</c> test it replaced could never be (kb/Work PB439). A leading sign is part of
    /// the numeric literal (§8.3.3.3.2 rule 2), so a negated literal is a literal and a negated identifier is
    /// not an identifier.</para></summary>
    private static (VaryingOperandKind Kind, decimal? Literal) Classify(BoundExpr e) => e switch
    {
        BoundUdfEvaluatedExpr w => Classify(w.Inner),   // the per-evaluation carrier is transparent to the KIND
        BoundNumLiteral l => (VaryingOperandKind.Literal, ParseLiteral(l.Text)),
        BoundNegate n when Classify(n.Operand) is (VaryingOperandKind.Literal, var v) =>
            (VaryingOperandKind.Literal, -v),
        BoundIndexRef => (VaryingOperandKind.IndexName, null),
        BoundBinary or BoundPower or BoundNegate or BoundExprError => (VaryingOperandKind.Invalid, null),
        _ => (VaryingOperandKind.Identifier, null),
    };

    /// <summary>A numeric literal's value, or null when the text is not a value this screen can read (a
    /// floating-point literal's exponent form, a DECIMAL-POINT IS COMMA spelling). Null suppresses only the
    /// VALUE tests — SR4 b), SR4 c) and SR6 — never the integer tests, which read the literal's FORM.</summary>
    private static decimal? ParseLiteral(string text) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal d) ? d : null;

    /// <summary>ISO §14.9.28.3 SR4, SR5 and SR6 over one bound level.
    ///
    /// <para>⛔ SR4 AND SR5 ARE TWO ARMS OF ONE QUESTION — which POSITION holds the index-name. SR4's premise is
    /// "an index-name is specified in the VARYING or AFTER phrase"; SR5's is "an index-name is specified in the
    /// FROM phrase". They are neither exclusive nor nested (both premises hold for
    /// <c>VARYING IX FROM JX BY N</c>), so both arms are asked, always, from the one place that knows both
    /// answers. A screen that asked only the VARYING position would be this project's most reproducible defect
    /// shape written again.</para>
    ///
    /// <para>SR6 ("The literal in the BY phrase shall not be zero") is unconditional and OVERLAPS SR4 c) where
    /// the varied item is an index-name; the stronger, unconditional rule is reported so a programmer is not
    /// told about a premise that is beside the point.</para></summary>
    private void CheckVaryingOperandRules(
        Core.DataReferenceContext dref, BoundSetTarget var, in VaryingOperand from, in VaryingOperand by)
    {
        bool varIsIndexName = var is SetIndexTarget;                     // SR4's premise
        bool fromIsIndexName = from.Kind is VaryingOperandKind.IndexName;   // SR5's premise

        if (varIsIndexName)
        {
            // a) "The identifier in the associated FROM and BY phrases shall reference an integer data item."
            if (from.Kind is VaryingOperandKind.Identifier && !from.IsInteger) Sr(from.Text, "FROM", "SR4 a)",
                "The identifier in the associated FROM and BY phrases shall reference an integer data item");
            if (by.Present && by.Kind is VaryingOperandKind.Identifier && !by.IsInteger) Sr(by.Text, "BY", "SR4 a)",
                "The identifier in the associated FROM and BY phrases shall reference an integer data item");
            // b) "The literal in the associated FROM phrase shall be a positive integer."
            if (from.Kind is VaryingOperandKind.Literal && !(from.IsInteger && from.Literal is > 0m))
                Sr(from.Text, "FROM", "SR4 b)", "The literal in the associated FROM phrase shall be a positive integer");
        }
        if (fromIsIndexName)
        {
            // a) "The identifier in the associated VARYING or AFTER phrase shall reference an integer data item."
            // Asked of the SAME screen as SR2 — and only once SR2 has admitted the operand, so a PIC X variable
            // draws SR2 alone rather than both rules.
            if (var is SetPlaceTarget { Place: var p }
                && OperandClassScreen.Admits(OperandPositions.PerformVaryingIdentifier, p))
                OperandClassScreen.Screen(ctx.Edition, OperandPositions.PerformVaryingIdentifierFromIndex, p, dref.GetText());
            // b) "The identifier in the associated BY phrase shall reference an integer data item."
            if (by.Present && by.Kind is VaryingOperandKind.Identifier && !by.IsInteger) Sr(by.Text, "BY", "SR5 b)",
                "The identifier in the associated BY phrase shall reference an integer data item");
        }
        if (by is { Present: true, Kind: VaryingOperandKind.Literal })
        {
            // SR6 first — it is unconditional, and where SR4 c) also applies it is the stronger statement of the
            // same defect. §14.9.28.4 GR12 makes the consequence concrete: the augment value would be 0, so no
            // induction variable ever changes and the UNTIL condition can never become true through the phrase.
            if (by.Literal is 0m)
                Sr(by.Text, "BY", "SR6", "The literal in the BY phrase shall not be zero");
            // c) of SR4 ("a nonzero integer") and of SR5 ("an integer") — the residue once zero is out.
            else if (varIsIndexName && !by.IsInteger)
                Sr(by.Text, "BY", "SR4 c)", "The literal in the associated BY phrase shall be a nonzero integer");
            else if (fromIsIndexName && !by.IsInteger)
                Sr(by.Text, "BY", "SR5 c)", "The literal in the associated BY phrase shall be an integer");
        }

        void Sr(string operand, string phrase, string rule, string quoted) =>
            ctx.Edition.Error(DiagnosticCatalog.PerformVaryingOperandRule,
                $"the PERFORM VARYING {phrase} operand '{operand}' violates ISO §14.9.28.3 {rule}: \"{quoted}\"");
    }
}
