// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Common;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// WHY an expression is being bound, which decides whether an ALPHANUMERIC operand is admissible in it (DA6).
/// <para>
/// The COBOL grammar production is named <c>arithmeticExpression</c> but is REUSED as the generic argument
/// expression, so one spine serves two rule sets and the leaf cannot tell them apart on its own:
/// </para>
/// <list type="bullet">
///   <item><see cref="Arithmetic"/> — ISO §8.8.1.1: "An arithmetic expression may be an identifier referencing a
///         NUMERIC data item, a numeric literal, the figurative constant ZERO …". A group (class alphanumeric,
///         §8.5), an elementary alphanumeric/national item, and a reference-modified slice (§8.4.3.3.4 GR6) are all
///         inadmissible. This is the default and by far the common case: COMPUTE, the arithmetic verbs,
///         subscripts, reference-modifier offsets, relation conditions, SET, PERFORM VARYING.</item>
///   <item><see cref="FunctionArgument"/> — governed instead by the individual function's §15.x ARGUMENT RULE,
///         which for the string functions explicitly admits alphanumeric data: <c>FUNCTION TRIM(S)</c>,
///         <c>SUBSTITUTE</c>, <c>FIND-STRING</c> and <c>CONVERT</c> over a <c>PIC X</c> item are all legal.</item>
///   <item><see cref="CallByValue"/> — governed by ISO §14.9.4.3 SR22 ("identifier-4 shall be of class numeric,
///         object, or pointer"), a NARROWER rule than §8.8.1.1 rather than a wider one. Binding it as arithmetic
///         happened to reject the right programs while citing the wrong rule.</item>
/// </list>
/// <para>
/// ⛔ This is an ENUM and not a <c>bool</c> deliberately. A boolean would read as <c>BindExpr(node, true)</c> at the
/// call site, would not survive the arrival of a third context, and — as an optional parameter — silently breaks
/// the method-group conversions this spine is used through (<c>Select(host.Expr.BindExpr)</c>). The public surface
/// is therefore three intention-revealing entry points over one private core, so no caller ever passes a flag.
/// </para></summary>
internal enum OperandContext
{
    /// <summary>An ISO §8.8.1.1 arithmetic expression: numeric operands only.</summary>
    Arithmetic,

    /// <summary>An intrinsic-function argument: the function's own §15.x argument rule governs, so an alphanumeric
    /// operand may be perfectly legal here.</summary>
    FunctionArgument,

    /// <summary>
    /// A <c>CALL … USING BY VALUE</c> operand: ISO §14.9.4.3 SR22 governs, NOT §8.8.1.1.
    /// </summary>
    /// <remarks>
    /// ⛔ The grammar production is named <c>arithmeticExpression</c>, and the binder took that at its word and
    /// bound the operand as arithmetic — so DA6's §8.8.1.1 screen fired on it and
    /// <c>CALL "PROG2" USING BY VALUE X</c> (X alphanumeric) was refused with a message about *arithmetic
    /// expressions* and a "digit-decoding extension". The VERDICT was right by accident — SR22 requires class
    /// numeric, object or pointer, so an alphanumeric operand is indeed illegal — but the rule quoted was not the
    /// rule broken, which tells the programmer to look in the wrong place. Caught by the pre-merge GnuCOBOL
    /// differential as a two-case AGREE_ACCEPT→WE_REJECT flip, then traced rather than waved through.
    /// <para>
    /// A production's NAME is not its operand's rule. This is the same shape DA6 recorded for itself: a rule
    /// enforced at a site that could not know its own context.
    /// </para>
    /// </remarks>
    CallByValue,

    /// <summary>An arithmetic-expression position that BOTH index clauses list — the SET statement's amounts and
    /// values: §13.18.38.3 r7 admits an INDEX-NAME here and §13.18.60.3 SR10 admits an INDEX DATA ITEM here
    /// (kb/Work R29, PB215). The §8.8.1.1 class screening is otherwise identical to <see cref="Arithmetic"/>, so
    /// <c>SET IX UP BY N</c> and <c>SET IX TO IDN</c> stay legal while <c>COMPUTE N = IX + 1</c> does not.
    /// ⚠ A RELATION OPERAND IS NOT HERE (kb/Work PB2018). r7's "as an operand in a relation condition" and
    /// §8.8.4.2.13's "Relation tests may be made only between" an index-name and a numeric data item or numeric
    /// literal name the relation's OPERAND, which <c>ConditionBinder.ComparisonOperandOf</c> binds through
    /// <c>FieldOperand</c> when it is a bare name. An operand that is an arithmetic expression
    /// (<c>IF N &lt; (K + 2)</c>, <c>IF IX + 1 = 4</c>) holds the name as an operand of the EXPRESSION, which §8.8.1.1
    /// does not admit, so it binds under <see cref="Arithmetic"/>. R29 had bound the whole relation operand here and
    /// its golden read "inside an expression" as legal, a wider reading than r7 and §8.8.4.2.13 give.</summary>
    ArithmeticIndexWindow,

    /// <summary>An arithmetic-expression position that §13.18.38.3 r7 lists and §13.18.60.3 SR10 does NOT — a
    /// SUBSCRIPT and the VARYING phrase of a PERFORM statement (kb/Work PB215). An index-NAME is a legal operand
    /// here (<c>T(IX + 1)</c>, <c>PERFORM … VARYING V FROM IX</c>); an index DATA ITEM is not, because SR10's
    /// closed list has no subscript and no PERFORM entry — so <c>E(IDX / 1)</c> and
    /// <c>PERFORM VARYING V FROM IDX</c> take §8.8.1.1's class screen like any arithmetic operand.</summary>
    ArithmeticIndexNameWindow,
}

/// <summary>⛔ THE TWO AXES EVERY OPERAND SLOT DECIDES, AS A TABLE (kb/Work PB169–PB172 — the burn-down cluster's
/// spine). An operand position in this compiler used to acquire ISO §8.8.1.1 BY DEFAULT — by being handed to
/// <see cref="ExpressionBinder.BindExpr"/> — rather than by declaring that §8.8.1.1 governs it, and the two
/// questions it actually has to answer were recorded in four different places: an enum member at ten sites, a
/// call-site comment at two, a private category switch at three, and NOWHERE AT ALL in
/// <c>ReferenceResolver</c>'s subscript reader. All four defects of the cluster are one site answering one axis
/// wrongly or not at all.
/// <list type="number">
///   <item><b>NumericClassScreen</b> — does §8.8.1.1's class-numeric screen govern this position, or does the
///         position have its own syntax rule? <c>CALL … BY VALUE</c> (§14.9.4.3 SR22) and an intrinsic argument
///         (§15.3) are governed by NARROWER rules of their own, so quoting §8.8.1.1 at their programmer names a
///         rule that was not broken.</item>
///   <item><b>IndexNameScreen</b> — is the §13.18.38.3 r7 index-name screen applied HERE? r7's closed context
///         list is "as a subscript · PERFORM VARYING · SEARCH VARYING · SET · an operand in a relation
///         condition", which is exactly <see cref="OperandContext.ArithmeticIndexWindow"/> plus
///         <see cref="OperandContext.ArithmeticIndexNameWindow"/>; CALL BY VALUE is exempt because SR22 screens
///         the operand itself.</item>
///   <item><b>IndexDataItemAdmitted</b> — does §13.18.60.3 SR10 admit a class-INDEX <i>data item</i> here?
///         ⛔ THIS IS A THIRD AXIS, NOT A CONSEQUENCE OF THE FIRST, and treating it as one was a measured
///         over-reach: SR10's list is "a SEARCH or SET statement, a relation condition, an intrinsic function
///         argument, an inline method invocation argument, the USING phrase of a procedure division header, or
///         the USING phrase of a CALL or INVOKE statement" — which is NOT §13.18.38.3 r7's list and NOT
///         §8.8.1.1's class rule. Deriving it from "class index is not class numeric" rejected
///         <c>SET IN1 TO IDN1</c> in EIGHT NIST programs (IC106A · IC107A · IC207A · IC208A · NC131A · NC133A ·
///         NC135A · NC236A), every one of them a SET statement SR10 names outright. A rule that enumerates
///         CONTEXTS cannot be modelled as a property of the operand (feedback_model_the_rule_shape_not_one_case).
///         </item>
/// </list>
/// ⛔ A NEW MEMBER CANNOT SILENTLY INHERIT §8.8.1.1. The discard arm THROWS rather than defaulting — an enum
/// switch expression needs one at all (CS8509 covers the undeclared-value space, and this repository builds with
/// <c>TreatWarningsAsErrors</c>), and a defaulting arm is exactly the mechanism this table exists to remove: it
/// would hand a new context whichever answer the author of the default happened to prefer. `OperandContextRules`
/// is therefore guarded from OUTSIDE as well — <c>OperandContextRulesTests</c> enumerates
/// <c>Enum.GetValues&lt;OperandContext&gt;()</c> and calls this for each, and a second fact asserts no screen has
/// gone back to keying on a hand-written member list. That is what turns "which arm did I fix" — the
/// repository's most reproducible defect shape — into a question that cannot be asked.</summary>
internal static class OperandContextRules
{
    internal static (bool NumericClassScreen, bool IndexNameScreen, bool IndexDataItemAdmitted) Rules(
        this OperandContext c) => c switch
    {
        //                                       §8.8.1.1   §13.18.38.3 r7   §13.18.60.3 SR10
        OperandContext.Arithmetic            => (true,      true,            false),
        OperandContext.FunctionArgument      => (false,     true,            true),   // SR10: "an intrinsic function argument"
        OperandContext.CallByValue           => (false,     false,           true),   // SR10: "the USING phrase of a CALL"
        // ⛔ THE TWO INDEX LISTS ARE DIFFERENT, SO THEY ARE TWO ROWS (kb/Work PB215). r7 (index-NAME) lists "as
        // a subscript; in the VARYING phrase of a PERFORM statement; in the VARYING phrase of a SEARCH
        // statement; in the SET statement; as an operand in a relation condition". SR10 (index DATA ITEM)
        // lists "a SEARCH or SET statement, a relation condition, an intrinsic function argument, …". One
        // member used to serve both, so it could not say no to an index data item in a subscript or PERFORM
        // VARYING, and every such site answered SR10's question with r7's list.
        //   · SET (every format, incl. CAPACITY / SIZE / pointer UP BY): on BOTH. (A relation condition and SEARCH are
        //     on both lists too, but their OPERAND is a bare name — FieldOperand — never an expression: PB2018.)
        OperandContext.ArithmeticIndexWindow => (true,      false,           true),
        //   · a subscript's compound segment, PERFORM VARYING FROM/BY: on r7's list only. (The SIMPLE
        //     subscript never comes through here — ReferenceResolver screens it by the same two rules.)
        OperandContext.ArithmeticIndexNameWindow => (true,  false,           false),
        //   · the Report Writer's VARYING FROM/BY (§13.18.64.2: plain arithmetic-expression-1/-2) is on
        //     NEITHER list, so it binds under Arithmetic — both screens on. It needs no member of its own: the
        //     row it would declare IS Arithmetic's.
        _ => throw new ArgumentOutOfRangeException(nameof(c), c,
            "a new OperandContext must declare ALL THREE axes in OperandContextRules.Rules() — whether ISO "
            + "§8.8.1.1's class screen governs the position, whether §13.18.38.3 r7's index-name screen applies, "
            + "and whether §13.18.60.3 SR10 admits a class-index DATA item. A context that declares none "
            + "acquires §8.8.1.1 by default, which is the shape of kb/Work PB169–PB172."),
    };
}

/// <summary>
/// The shared operand/expression/receiving spine (P7 Step 10q — the expression-spine flip; the plan’s
/// `Binding/Procedure/ExpressionBinder`, a SIBLING of <see cref="BinderContext"/>/<see cref="PhraseBlocks"/>,
/// deliberately NOT `Verbs/` — every verb binder consumes it). Three families over <see cref="BinderContext"/>:
/// the OPERAND family (LiteralOperand / national §8.3.3.5 / boolean §8.3.3.4 / figurative §8.3.3.6.4 /
/// FieldOperand with the keyword-omitted-function, LINAGE-COUNTER §8.4.3.14, RW-counter §8.4.3.15 and
/// index-name §13.18.38 interceptions), the EXPRESSION spine (<see cref="BindExpr"/> / BindChain / BindPower /
/// BindPrimary / the breadth-first <see cref="BindOperandExpr"/> wrapper descent / NumLiteral, with
/// <see cref="CheckLiteral"/> carrying its DECIMAL-POINT normalization + edition digit-cap window AS-IS — the
/// sanctioned binder-side pattern), and the RECEIVING family (<see cref="ResolveReceiving"/> — THE one
/// receiving-side chokepoint — ResolveTargets / Receivers×3 / RoundingOf §14.7.4). Host edges that remain
/// (host.Intrinsic / host.Rw) flip at 10t; the generic tree statics DataRefs/Children STAY on
/// <see cref="StatementBinder"/> (they are not expression-specific).
/// </summary>
internal sealed class ExpressionBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>The C# <c>long</c> index cell when <paramref name="dref"/> is an INDEXED BY index-name reference
    /// — bare or QUALIFIED (§8.4.2.2.2 Format 3; §8.4.2.2.3 SR6 — <c>SET IX OF T TO 1</c>) — else
    /// <see langword="null"/>. Index-names are a separate name class (<see cref="DataBinder.IndexNames"/>), not the
    /// data-item tree; the ONE resolution <see cref="ReferenceResolver.ResolveIndexName"/> counts the candidates, so
    /// a duplicated spelling written without the qualifiers that settle it is §8.4.2.2.3 SR1's error — reported
    /// there, and still answered with an (inert) cell so the caller keeps the index-name shape instead of
    /// re-resolving the word as a data-name (kb/Work PB919).</summary>
    public string? IndexFieldOf(Core.DataReferenceContext dref)
    {
        if (dref.cobolWord()?.GetText() is not { } w) return null;
        List<string>? quals = null;
        foreach (var suffix in dref.dataReferenceSuffix())
        {
            // Only qualification: an index-name is never subscripted or reference-modified (§8.4.2.2.2 Format 3
            // has neither), so such a reference is not an index-name reference.
            if (suffix.qualification() is not { } q || q.ChildCount != 2) return null;
            (quals ??= []).Add(q.cobolWord().GetText());
        }
        var ix = ctx.Refs.ResolveIndexName(w, (IReadOnlyList<string>?)quals ?? [], dref);
        return ix.Outcome is ReferenceResolver.IndexRefOutcome.NotAnIndexName ? null : ix.Cell;
    }

    // ── Operands & expressions ─────────────────────────────────────────────────────────────────────────────

    /// <summary>⛔ THE ONE <c>nonNumericLiteral</c> → <see cref="BoundOperand"/> MAPPING (ISO §8.3.3). Returns
    /// <see langword="null"/> when the node is absent or is not a non-numeric literal, so each caller supplies its
    /// own fallback — a numeric literal for <see cref="LiteralOperand"/>, a named error for an intrinsic argument,
    /// the expression path for a comparison operand.
    /// <para>
    /// ⛔ WHY THIS IS EXTRACTED. This chain existed in THREE hand-maintained copies — here, in
    /// <c>IntrinsicBinder.NonNumericOperand</c>, and inline in <c>ConditionBinder</c>'s comparison-operand binder —
    /// and that duplication IS the defect DA3 reported. A hexadecimal-alphanumeric literal was simply MISSING from
    /// one copy, so <c>IF G = X"6162"</c> (conforming source) staged loud as a "comparison operand" at run time
    /// while the same literal worked in a MOVE. Three copies of one dispatch guarantee that the next literal form
    /// added will be wired into two of them; one copy makes the next form a one-line change.
    /// </para></summary>
    public BoundOperand? NonNumericLiteralOperand(Core.NonNumericLiteralContext? nn)
    {
        if (nn is null) return null;
        // A concatenation expression folds to its equivalent single literal at COMPILE time (ISO §8.8.3.3
        // GR3 — "equivalent to a literal of the same class and value"): no BoundConcat node, no emitter leg;
        // the folded value rides the same operand shapes the plain literals produce (P10 Step 14).
        if (nn.concatenationExpression() is { } ce) return ConcatOperand(ce);
        if (nn.figurativeConstant() is { } fig) return FigurativeOperand(fig);
        if (nn.STRINGLIT() is { } s) return new BoundStringLiteral(CobolLiteral.Decode(s.GetText()));
        // ⛔ DA3 — X"…" is an ALPHANUMERIC literal, not a separate species: §8.3.3.2 Format 2 IS the
        // hexadecimal-alphanumeric FORMAT of the alphanumeric literal, and §8.3.3.2.1 makes every format of it "of
        // the class and category alphanumeric" (both cite.py-verified). So it belongs wherever an alphanumeric
        // literal belongs — a relation condition (§8.8.4.2.1 constrains only that not BOTH operands be
        // literals, so a literal on one side is admitted; the phantom "§8.8.4.1.1" this used to cite does not
        // exist — cite.py --check, kb/Work PB182, whose note owns the remaining sites), MOVE, STRING, an
        // intrinsic argument. CobolLiteral.DecodeHex is the ONE hex codec (landed by DA1 for the §12.3.7 ALPHABET
        // path); this reuses it rather than decoding again.
        if (nn.HEXLIT() is { } hx) return new BoundStringLiteral(CobolLiteral.DecodeHex(hx.GetText()));
        // National N"…" (§8.3.3.5) / boolean B"…" (§8.3.3.4) literals — LIVE (Phase 4a): the introduction
        // gate rides every occurrence (0900 below 2002); the length rule (COBOLNET0814) is LiteralScreenPass's, for
        // every class and position. The lexer already restricts a BOOLLIT's content to [01]+ (CobolLexer.g4).
        if (nn.NATLIT() is { } nat) return NationalLiteralOperand(nat.GetText());
        if (nn.BOOLLIT() is { } b) return BooleanLiteralOperand(b.GetText());
        if (nn.predefinedNull() is { } pn) return RefusePredefinedNull(pn);
        return null;
    }

    /// <summary>A <c>literal</c> node as an operand: the non-numeric forms through the ONE
    /// <see cref="NonNumericLiteralOperand"/> mapping, else a numeric literal under the edition digit cap
    /// (ISO §8.3.3.3.2).</summary>
    public BoundOperand LiteralOperand(Core.LiteralContext lit) =>
        NonNumericLiteralOperand(lit.nonNumericLiteral())
        ?? NumericLiteralOperand(lit.GetText());

    /// <summary>⛔ THE ONE PRODUCER OF A NUMERIC LITERAL OPERAND THE PROGRAM WROTE (a literal, or the constant-name that
    /// substitutes one): the value is <see cref="CheckLiteral"/>'s canonical dot-decimal text, and the literal keeps
    /// the program's decimal separator for its character image. ISO §14.9.11.4 GR1 leaves the conversion of a literal
    /// to the device to the implementor, and §12.3.7.4 GR14 a) makes the comma the character "written in numeric
    /// literals to represent the decimal separator", so `DISPLAY 1,5` under DECIMAL-POINT IS COMMA shows `1,5`
    /// (DOC-A.1-56; kb/Work PB1643 — it showed `1.5`, because one text field was both the value and the image).</summary>
    public BoundNumericLiteral NumericLiteralOperand(string asWritten) =>
        new(CheckLiteral(asWritten)) { DecimalSeparator = ctx.Data.DecimalSeparator };

    /// <summary>The arithmetic-expression-tier twin of <see cref="NumericLiteralOperand"/>: the same literal, the same
    /// separator, as a <see cref="BoundNumLiteral"/> (it reaches a character image through
    /// <c>IntrinsicBinder.OperandOf</c>, e.g. a numeric literal argument of a function).</summary>
    private BoundNumLiteral NumericLiteralExpr(string asWritten) =>
        new(CheckLiteral(asWritten)) { DecimalSeparator = ctx.Data.DecimalSeparator };

    /// <summary>⛔ THE ONE PRODUCER OF <see cref="BoundPredefinedNull"/> — the operand slot of a §8.4.3.10.3 SR1
    /// context, which the predefined NULL may occupy: "it may be used only as a sending operand in an INITIALIZE or
    /// a SET statement; as an argument in a program-prototype format CALL statement, a function-prototype format
    /// function activation, or a method invocation; or in a pointer-or-object-reference relation condition" (SR1
    /// a; SR1 b's message-tag relation condition is the same relation slot). Every other slot binds through
    /// <see cref="NonNumericLiteralOperand"/>, which REFUSES NULL (COBOLNET2576) — so a slot nobody thought about is
    /// refused by construction instead of running NULL as LOW-VALUE (kb/Work PB1427).
    /// <para>Admission is not acceptance: each caller still decides NULL against its other operand or formal —
    /// the relation checkpoint's §8.8.4.2.3 SR5 band (COBOLNET0869), the §14.8.2 argument conformance
    /// (<c>ParameterConformance</c>), INITIALIZE's §14.9.20.3 SR4 category agreement. The callers are pinned by
    /// <c>PredefinedNullContextDriftTests</c>: a new one must be an SR1 context.</para></summary>
    public BoundOperand? NullAdmittingOperand(Core.NonNumericLiteralContext? nn) =>
        nn?.predefinedNull() is not null ? BoundPredefinedNull.Instance : NonNumericLiteralOperand(nn);

    /// <summary>The <c>literal</c>-node twin of <see cref="NullAdmittingOperand(Core.NonNumericLiteralContext?)"/>
    /// — the same §8.4.3.10.3 SR1 admission, else <see cref="LiteralOperand"/>'s mapping.</summary>
    public BoundOperand NullAdmittingOperand(Core.LiteralContext lit) =>
        NullAdmittingOperand(lit.nonNumericLiteral()) ?? NumericLiteralOperand(lit.GetText());

    /// <summary>The §8.4.3.10.3 SR1 refusal — the ONE diagnostic for NULL written in a slot the rule does not list
    /// (kb/Work PB1427). It replaced the per-statement NULL arms (the STOP/GOBACK status, DISPLAY, MOVE screens),
    /// each of which re-stated the rule for its own verb while every verb without an arm ran NULL as LOW-VALUE. The
    /// text is <see cref="PredefinedNullRule"/>'s, shared with the positions outside the operand model.</summary>
    private BoundOperandError RefusePredefinedNull(Core.PredefinedNullContext pn)
    {
        PredefinedNullRule.Report(ctx.Edition, "this operand position");
        return BoundOperandError.Refused(ctx.Edition, $"NULL at line {pn.Start.Line}");
    }

    /// <summary>Bind a §8.8.3 concatenation expression as a literal operand: fold to the equivalent single
    /// literal (§8.8.3.3 GR2/GR3 — the ONE ConcatFolder chokepoint enforces the §8.8.3.2 SRs) and produce the
    /// SAME bound shape the equivalent plain literal would have produced ("may be used anywhere a literal of
    /// that class may be used"). The introduction gate (concat-operator-2002 → 0900 below 2002) rides the
    /// VersionConformancePass parse arm on recognition, not this bind path.</summary>
    public BoundStringLiteral ConcatOperand(Core.ConcatenationExpressionContext ce)
    {
        var folded = ConcatFolder.Fold(ce, ctx.Edition, ctx.Data.LiteralEnv);
        return folded.Category switch
        {
            PicCategory.National => new BoundStringLiteral(folded.Value) { Category = PicCategory.National },
            PicCategory.Boolean => new BoundStringLiteral(folded.Value) { Category = PicCategory.Boolean },
            _ => new BoundStringLiteral(folded.Value),
        };
    }

    /// <summary>Bind an <c>N"…"</c> national literal (ISO §8.3.3.5). The content repertoire is the FULL national
    /// character set — one UTF-16 char per position (D-N1, §8.1.2 NOTE 2) — including characters above U+00FF: the
    /// alphanumeric↔national correspondence (Annex A.1 item 33 — the TOTAL UTF-16 identity, both directions, PB59)
    /// is live through FUNCTION DISPLAY-OF / NATIONAL-OF (§15.26/§15.66), so the former staged-loud Latin-1-only
    /// guard is lifted (P10 national wave). The §8.3.3.5.3 SR1 length and SR5 grouping rules are the literal's
    /// own and LiteralScreenPass asks them of every literal token (kb/Work PB1393) — never a binder funnel.</summary>
    public BoundStringLiteral NationalLiteralOperand(string raw) =>
        // NationalData2002 (the N"…" literal introduction) gates on RECOGNITION in the VersionConformancePass
        // parse-arm (VisitNonNumericLiteral, statement-scoped); Step 14h.4b.
        new(CobolLiteral.Decode(raw)) { Category = PicCategory.National };

    /// <summary>Bind a <c>B"…"</c> boolean literal (ISO §8.3.3.4): SR2 ('0'/'1' only) is lexer-enforced, and the
    /// §8.3.3.4.3 SR1 length rule is LiteralScreenPass's (kb/Work PB1393).</summary>
    public BoundStringLiteral BooleanLiteralOperand(string raw) =>
        // BooleanData2002 (the B"…" literal introduction) gates on RECOGNITION in the VersionConformancePass
        // parse-arm (VisitNonNumericLiteral, statement-scoped); Step 14h.4b.
        new(CobolLiteral.Decode(raw)) { Category = PicCategory.Boolean };

    /// <summary>Bind a figurative constant to a bound operand. <c>ALL "literal"</c> / <c>ALL X"…"</c> (a
    /// multi-character figurative, ISO §8.3.3.6.4 Format 6) → <see cref="BoundAllLiteral"/>; <c>ALL ZEROS</c> etc.
    /// are the single-character figurative repeated to width, identical to the bare word.</summary>
    /// <remarks>
    /// ⛔ The HEXLIT arm was the FIFTH site of the hexadecimal-literal defect (fix-queue PB4), and the one the
    /// grammar had been ready for all along — <c>figurativeConstant</c> lists <c>ALL HEXLIT</c>, and this method
    /// tested only <c>STRINGLIT</c>, so <c>MOVE ALL X"41" TO X</c> parsed and then died at RUN time with
    /// "figurative constant 'ALLX\"41\"'". §8.3.3.2 makes a hexadecimal literal one form of an ALPHANUMERIC
    /// literal, so Format 6's literal-1 admits it and the two arms are the same case.
    /// </remarks>
    public BoundOperand FigurativeOperand(Core.FigurativeConstantContext fig)
    {
        // ⛔ ONE ARM FOR EVERY LITERAL-1 KIND (kb/Work PB71): §8.3.3.6.3 SR2 admits an alphanumeric (plain or
        // hexadecimal), boolean or national literal-1, and the category rides on the literal's class through
        // BoundAllLiteral.Of. Two arms of four were written here: `ALL B"1"` (the grammar HAD the token) parsed and
        // died at RUN time exactly as `ALL X"41"` once did — the PB4 shape, one arm later, under the remark that
        // records it — and `ALL N"…"` had no grammar arm at all. Literal-1 in every written shape — a quoted literal,
        // a §8.8.3 concatenation expression, a constant-name (kb/Work PB1627) — folds through the ONE reader, which
        // reports §8.3.3.6.3 SR2 and the concatenation's §8.8.3.2 rules.
        if (ConcatFolder.FoldAllLiteral(fig, ctx.Data.LiteralEnv, ctx.Edition) is { } literal1)
            return BoundAllLiteral.Of(literal1);
        // Format 7 — ALL symbolic-character-1 (§8.3.3.6.2; §12.3.7.4 GR11 — kb/Work PB110): the one-character
        // figurative the SYMBOLIC CHARACTERS clause defined, as the ALL literal of that character (§8.3.3.6.4
        // GR10's "one or more of the character" IS the fill semantics BoundAllLiteral carries in every context).
        if (fig.cobolWord() is { } symWord)
        {
            if (ctx.Data.SymbolicOf(symWord.GetText()) is { } sym)
                return SymbolicOperand(sym) is BoundAllLiteral al7
                    ? al7 with { BeginsWithAll = true }   // the EXPLICIT ALL form — the word is written
                    : SymbolicOperand(sym);
            ctx.Edition.Error(DiagnosticCatalog.SymbolicCharactersViolation, $"ALL {symWord.GetText()}: the word names "
                + "no constant-name (§13.10.3 SR2) and no symbolic-character — symbolic-character-1 shall be specified in "
                + "the SYMBOLIC CHARACTERS clause of the SPECIAL-NAMES paragraph (ISO §8.3.3.6.3 SR4)");
            return BoundOperandError.Refused(ctx.Edition, $"ALL {symWord.GetText()}");
        }
        if (fig.zeroWord() is not null) return new BoundFigurative('Z');
        if (fig.spaceWord() is not null) return new BoundFigurative('S');
        if (fig.highValueWord() is not null) return new BoundFigurative('H');
        if (fig.lowValueWord() is not null) return new BoundFigurative('L');
        if (fig.quoteWord() is not null) return new BoundFigurative('Q');
        return BoundOperandError.Refused(ctx.Edition, $"figurative constant '{ConcatFolder.Spelling(fig)}'");
    }

    /// <summary>⛔ THE ONE §8.3.3.6.3 SR1 a) test: "If the literal is restricted to a numeric literal, the only
    /// figurative constant permitted is ZERO (ZEROS, ZEROES) without the ALL phrase." The grammar has a distinct
    /// <c>ALL zeroWord</c> alternative, so <c>zeroWord()</c> alone also answers true for <c>ALL ZERO</c>. The ALL
    /// test is part of the rule (kb/Work PB218), and every numeric-restricted literal position asks it here: the
    /// arithmetic operand, CALL §14.9.4.3 SR23 and INVOKE §14.9.23.3 SR16 (kb/Work PB1631). ⚠ The ALL half has
    /// an EDITION edge (kb/Work PB1415, VCR Table 7 row 7.28): at 85 the word ALL before ZERO is redundant, so
    /// the arithmetic operand routes <c>ALL ZERO</c> through <see cref="FigurativeAssociation.GateAllZeroAsNumeric"/>;
    /// the CALL and INVOKE positions are COBOL-2002 constructs, where this strict test and the gate agree.</summary>
    internal static bool IsNumericRestrictedZero(Core.FigurativeConstantContext? fig) =>
        fig?.zeroWord() is not null && fig.ALL() is null;

    /// <summary>The bound operand of a symbolic character (§12.3.7.4 GR11; kb/Work PB110): the ALL literal of its
    /// ONE character — GR10 ("one or more of the character") is the figurative fill in a fixed-length association
    /// (§8.3.3.6.4 GR2) and one character where the context does not size it (GR3 b), exactly BoundAllLiteral's
    /// semantics in every consumer.</summary>
    internal static BoundOperand SymbolicOperand((string Value, bool National) sym) =>
        new BoundAllLiteral(sym.Value) { Category = sym.National ? PicCategory.National : PicCategory.Alphanumeric,
            BeginsWithAll = false };   // the BARE Format-7 reference — fill semantics without the word ALL

    public BoundOperand FieldOperand(Core.DataReferenceContext dref) =>
        host.Intrinsic.KeywordOmittedFunction(dref) is { } kof ? IntrinsicBinder.OperandOf(kof)   // §8.4.3.2 SR2 — a repository intrinsic/function name + (args) without FUNCTION
        : dref.LINAGE_COUNTER() is not null
            ? LinageFileOf(dref) is { } lcf ? new BoundComputedOperand(new BoundLinageCounterRef(lcf))
                : BoundOperandError.Refused(ctx.Edition, $"LINAGE-COUNTER reference '{DataBinder.WrittenText(dref)}' (ISO §8.4.3.14)")
        // LINE-COUNTER / PAGE-COUNTER (ISO §8.4.3.15) — RWCS registers, intercepted ahead of name resolution
        // (the LINAGE-COUNTER idiom); a BoundExprError inside the computed wrapper stays loud (§1.4).
        : host.Rw.CounterExpr(dref) is { } rcx ? new BoundComputedOperand(rcx)
        : host.Rw.VaryingExpr(dref) is { } rvx ? new BoundComputedOperand(rvx)   // a report VARYING counter in scope (§13.18.64.3 SR2)
        : IndexFieldOf(dref) is { } ix ? new BoundComputedOperand(new BoundIndexRef(ix))
        : ConstantOperand(dref) is { } konst ? konst   // a constant-name substitutes its literal (§13.10.3 SR2)
        : ctx.Data.SymbolicOf(dref) is { } sym ? SymbolicOperand(sym)   // a symbolic character is a figurative constant (§12.3.7.4 GR11; PB110)
        : ctx.Refs.Resolve(dref) is var r && r.Place is { } p ? new BoundFieldOperand(p) : r.OperandError(ctx.Edition);   // kb/Work PB1030

    /// <summary>The §13.18.38.3 r7 screen for an operand slot OUTSIDE the five contexts that may reference an
    /// index-name (a subscript · PERFORM VARYING · SEARCH VARYING · SET · a relation-condition operand) — kb/Work
    /// R16. <see cref="FieldOperand"/> deliberately still BINDS the index-name (the r7-legal consumers share it),
    /// so each ILLEGAL slot screens the result: before this, DISPLAY/STRING/MOVE-to-string aborted at RUN TIME
    /// ("computed expression in a string context") and MOVE-to-numeric silently computed — against the same
    /// judgment COBOLNET0809 already applies to class-index DATA ITEMS in MOVE. Returns true when the operand
    /// was an index-name (the caller substitutes its own error operand or continues; the diagnostic is
    /// emitted here, once).
    /// <para>⛔ NO <c>--permissive</c> LANE HERE, AND THE REASON IS WRITTEN DOWN RATHER THAN INFERRED (kb/Work
    /// PB219). r7's four enforcement sites take ONE of two postures, chosen by what the SLOT IS: an ARITHMETIC
    /// position (<see cref="IndexNameExpr"/>, and <see cref="ReferenceResolver"/>'s ref-mod bound per §8.4.3.3.3
    /// SR4) carries the GnuCOBOL occurrence-number coercion under <c>--permissive</c>; an IDENTIFIER slot — this
    /// screen's callers (DISPLAY, MOVE, STRING, the STOP RUN / GOBACK status operand) and
    /// <c>InspectBinder</c>'s — rejects in BOTH lanes, because the slot needs an IDENTIFIER and an occurrence
    /// number is not one, so there is no coercion to offer. <c>dialect_two_axes</c> constrains the leniencies
    /// this compiler implements; it does not require inventing one.</para></summary>
    public bool ScreenIndexNameOperand(BoundOperand op, string sourceText, string where)
    {
        if (op is not BoundComputedOperand { Expr: BoundIndexRef }) return false;
        ctx.Edition.Error(DiagnosticCatalog.IndexNameContext,
            $"{where} references the index-name '{sourceText}', which is not an identifier "
            + "(ISO §8.4.3.1.2); §13.18.38.3 r7 admits an index-name only as a subscript, in PERFORM/SEARCH "
            + "VARYING, in SET, or in a relation condition. SET a data item to the index first "
            + $"(SET data-item TO {sourceText}) and reference the data item");
        return true;
    }

    /// <summary>The reusable operand-CLASS screen (kb/Work PB148): a statement operand slot whose syntax rule
    /// closes a class list ("shall not reference a data item of class …", or a closed reference list like
    /// §13.18.60.3 SR10's for an index DATA item) rejects here, over the ONE classifier
    /// (<see cref="IntrinsicArgumentRules.ClassOf"/> — so "class pointer" spans data-, function- and
    /// program-pointer, class object the object references, class index the index data items, and the
    /// grammar-carried NULL word (class pointer via its CandidateClasses singleton — NULL is NOT a §8.3.3.6.2
    /// figurative constant) rejects through the same gate). A figurative whose class the context would choose
    /// (ZERO, SPACE …) has no singleton class and passes untouched. Returns true when rejected.</summary>
    public bool ScreenOperandClass(BoundOperand op, IReadOnlyList<CobolClass> excluded, string sourceText,
        string where, string cite)
    {
        if (IntrinsicArgumentRules.ClassOf(op) is not { } cls || !excluded.Contains(cls)) return false;
        ctx.Edition.Error(DiagnosticCatalog.OperandClassExcluded,
            $"{where} operand '{sourceText}' is of class {cls.ToString().ToLowerInvariant()}, which the "
            + $"statement's rules exclude — {cite}");
        return true;
    }

    /// <summary>The bound literal a bare constant-name reference substitutes, or <see langword="null"/> when
    /// <paramref name="dref"/> names no constant (ISO §13.10.3 SR2 / §13.10.4 GR1 — "as if [the] literal were
    /// written where constant-name-1 is written"): the SAME bound shape the equivalent plain literal would
    /// produce (the <see cref="ConcatOperand"/> precedent). A numeric constant rides <see cref="CheckLiteral"/>
    /// so the edition digit-cap window applies to the substituted literal exactly as to a written one.</summary>
    internal BoundOperand? ConstantOperand(Core.DataReferenceContext dref) =>
        ctx.Data.ConstantOf(dref) is not { } k ? null
        : k.Category switch
        {
            PicCategory.Numeric => NumericLiteralOperand(k.Text),
            PicCategory.National => new BoundStringLiteral(k.Text) { Category = PicCategory.National },
            PicCategory.Boolean => new BoundStringLiteral(k.Text) { Category = PicCategory.Boolean },
            _ => new BoundStringLiteral(k.Text),
        };


    /// <summary>Bind a data reference in a numeric-expression position: an INDEXED BY index-name reads its
    /// occurrence number (valid in SET/SEARCH/relations, ISO §13.18.38); the LINAGE-COUNTER register reads its
    /// file's runtime counter (ISO §8.4.3.14 GR1 — an unsigned integer); otherwise the resolved item's value.
    /// The ONE dataReference→<see cref="BoundExpr"/> mapping, used by every expression path.</summary>
    private BoundExpr RefExpr(Core.DataReferenceContext dref, OperandContext context) =>
        // §8.4.3.2.3 SR2 — a repository intrinsic/function name + (args) without FUNCTION: a function-identifier
        // like any other, so it takes the SAME §8.8.1.1 class screen (kb/Work PB1142).
        host.Intrinsic.KeywordOmittedFunction(dref) is { } kof ? ScreenIdentifierOperand(kof, dref, context)
        : dref.LINAGE_COUNTER() is not null
            ? LinageFileOf(dref) is { } lcf ? new BoundLinageCounterRef(lcf)
                : BoundExprError.Refused(ctx.Edition, $"LINAGE-COUNTER reference '{DataBinder.WrittenText(dref)}' (ISO §8.4.3.14)")
        // LINE-COUNTER / PAGE-COUNTER (ISO §8.4.3.15): in the PROCEDURE DIVISION the registers may appear
        // wherever an integer item may (SR1) — read from the report's engine instance, never storage.
        : host.Rw.CounterExpr(dref) is { } rcx ? rcx
        : host.Rw.VaryingExpr(dref) is { } rvx ? rvx   // a report VARYING counter in scope (§13.18.64.3 SR2)
        : IndexFieldOf(dref) is { } ix ? IndexNameExpr(dref, ix, context)
        // A constant-name substitutes its literal (§13.10.3 SR2 / §13.10.4 GR1) — in a numeric-expression
        // position only a NUMERIC constant is legal, exactly as for a written literal (§8.8.1.1).
        : ctx.Data.ConstantOf(dref) is { } k
            ? k.Category is PicCategory.Numeric ? NumericLiteralExpr(k.Text)
                : NonNumericConstantExpr(dref.GetText(), k.Category)
        : ctx.Refs.Resolve(dref) is var r && r.Place is { } p ? OperandRef(dref, p, context)
        : r.ExprError(ctx.Edition);   // kb/Work PB1030 — the resolver's answer picks Refused or Unbuilt

    /// <summary>The §13.18.38.3 r7 screen for an INDEX-NAME reached as an expression operand (kb/Work R29 —
    /// the arithmetic sibling of R16's statement-slot screen). r7's closed context list admits an index-name
    /// in a subscript, PERFORM/SEARCH VARYING, SET, and a relation condition — those positions bind under
    /// <see cref="OperandContext.ArithmeticIndexWindow"/> or <see cref="OperandContext.ArithmeticIndexNameWindow"/>
    /// (the two rows differ only on §13.18.60.3 SR10's index DATA item, kb/Work PB215) and pass through. The
    /// report writer's VARYING is NOT r7's "VARYING phrase of a PERFORM statement" and takes this screen. A true arithmetic position
    /// (COMPUTE and the arithmetic verbs, RETRY, CONTINUE AFTER, a reference-modification bound per
    /// §8.4.3.3.3 SR4 …) or a compound function-argument expression is NOT in the list and §8.8.1.1 does not
    /// name index-names among arithmetic operands — the classic vendor extension (GnuCOBOL computes the
    /// occurrence number), so the disposition is the DA6/PB1 shape: strict = reject with the r7 citation,
    /// <c>--permissive</c> = the documented coercion (the occurrence number computes, with a warning).
    /// CallByValue keeps its own §14.9.4.3 SR22 screen and is deliberately not intercepted here.
    /// <para>⚠ THE STOP RUN / GOBACK STATUS OPERAND IS NO LONGER ONE OF THIS METHOD'S CALLERS, AND IT WAS NEVER
    /// "a true arithmetic position" (kb/Work PB169 + PB223 — this summary listed it as both). §14.9.42.2 writes
    /// the slot <c>{identifier-1 | literal-1}</c>, not <c>arithmetic-expression-1</c>, so §8.8.1.1 never governed
    /// it; <c>ControlFlowBinder.BindTerminationStatus</c> now binds it as the position's own operand and screens
    /// r7 there through R16's <see cref="ScreenIndexNameOperand"/> — an IDENTIFIER-slot posture (unconditional
    /// Error, no coercion to offer), not this one. The slot is named here only to record that it left.</para>
    /// <para>The lane split across r7's four sites is written down once, in
    /// <see cref="ReferenceResolver"/><c>.IndexNameInPositionError</c>: an ARITHMETIC position takes THIS
    /// method's two lanes, an IDENTIFIER slot takes R16's single one.</para></summary>
    private BoundExpr IndexNameExpr(Core.DataReferenceContext dref, string ix, OperandContext context)
    {
        if (context.Rules().IndexNameScreen)
        {
            if (ctx.Edition.Permissive)
                ctx.Edition.Warning(DiagnosticCatalog.IndexNameContext,
                    $"the index-name '{DataBinder.WrittenText(dref)}' is used as an arithmetic operand; §13.18.38.3 r7 "
                    + "admits an index-name only in a subscript, PERFORM/SEARCH VARYING, SET, or as an operand OF a "
                    + "relation condition, never inside an arithmetic expression (§8.8.4.2.13) — accepted under "
                    + "--permissive, computing the occurrence number");
            else
            {
                ctx.Edition.Error(DiagnosticCatalog.IndexNameContext,
                    $"the index-name '{DataBinder.WrittenText(dref)}' is not an arithmetic operand (ISO §8.8.1.1 names no "
                    + "index-names; §13.18.38.3 r7 admits an index-name only in a subscript, PERFORM/SEARCH "
                    + "VARYING, SET, or as an operand OF a relation condition, never inside an arithmetic "
                    + "expression, §8.8.4.2.13). SET a data item to the index first "
                    + $"(SET data-item TO {DataBinder.WrittenText(dref)}) — or --permissive accepts it as the occurrence "
                    + "number");
                return BoundExprError.Refused(ctx.Edition, $"index-name '{DataBinder.WrittenText(dref)}' in an arithmetic expression");
            }
        }
        return new BoundIndexRef(ix);
    }

    /// <summary>The §8.8.1.1 class screen for a resolved data reference used as an expression operand (DA6).
    /// <para>
    /// WiseOwl COBOL accepted every alphanumeric shape here and decoded its digit characters — and did so
    /// INCONSISTENTLY: a group of <c>PIC X</c> leaves computed, while a group of <c>PIC 9</c> leaves compiled and
    /// then THREW at run time, so the operand whose digits were unambiguous failed and the merely-textual one
    /// succeeded. Owner decision 2026-07-29: reject under strict conformance, keep the leniency DIALECT-GATED
    /// behind <c>--permissive</c> (the standing rule that every leniency is dialect-gated).
    /// </para>
    /// <para>
    /// Reuses <c>COBOLNET0844</c> rather than minting a code: 0844 already IS "not a numeric operand (ISO
    /// §8.8.1.1)", raised by <see cref="NonNumericConstantExpr"/> for a non-numeric constant-name and for a
    /// national/boolean literal in this same position. A data item is the third shape of ONE rule, not a new rule.
    /// </para></summary>
    private BoundExpr OperandRef(Core.DataReferenceContext dref, Place p, OperandContext context)
    {
        // BOTH arithmetic contexts (kb/Work PB155): ArithmeticIndexWindow differs ONLY in the index-name
        // interception (handled before this point), exactly as the enum doc states — R29 flipped eleven call
        // sites (SET, PERFORM/SEARCH VARYING, compound subscripts, compound relation/EVALUATE operands) to the
        // window context and this guard was never widened, so `SET IX TO <PIC X item>` silently digit-decoded
        // under STRICT while `ADD` drew 0844. The arithmetic contexts are now the rows of
        // <see cref="OperandContextRules.Rules"/> that declare NumericClassScreen, so they cannot drift apart
        // again (kb/Work PB172; the index window became two rows with kb/Work PB215).
        // ⛔ THE VERDICT IS THE ONE CLASSIFIER'S, not this method's (kb/Work PB170): before, a private category
        // switch decided admissibility and had no index-data-item arm, so `COMPUTE N = IDX + 1` with `01 IDX
        // USAGE INDEX` compiled clean under STRICT and computed the occurrence number — while the receiving-side
        // twin ScreenResultant DID reject it. NonNumericOperandKind now only DESCRIBES a rejection the
        // §8.5.2.1 Table-2 classifier has already made.
        var rules = context.Rules();
        // ⛔ SR10 IS ASKED BEFORE §8.8.1.1, because it is a DIFFERENT rule about a DIFFERENT thing: §8.8.1.1
        // says what an arithmetic expression may be built from, §13.18.60.3 SR10 enumerates the CONTEXTS an
        // index data item may be referenced in — and SET, SEARCH and a relation condition are on that list by
        // name. Deriving SR10 from "class index is not class numeric" rejected `SET IN1 TO IDN1` in eight NIST
        // programs; the rule enumerates contexts, so the context has to answer — and where it answers no (a
        // subscript, PERFORM VARYING: kb/Work PB215) the operand falls through to §8.8.1.1's class screen.
        bool indexItem = p.DenotedItem is not null && p.Item.Pic is { Usage: Usage.Index };
        if (indexItem && rules.IndexDataItemAdmitted) return new BoundNumRef(p);
        if (rules.NumericClassScreen
            && !IntrinsicArgumentRules.IsArithmeticOperandClass(new BoundFieldOperand(p)))
        {
            string what = NonNumericOperandKind(p);
            bool decodable = IntrinsicArgumentRules.IsDigitDecodable(new BoundFieldOperand(p));
            if (ctx.Edition.Permissive && decodable)
                ctx.Edition.Warning("COBOLNET0844", $"{what} is not a numeric operand (ISO §8.8.1.1); accepted "
                    + "under --permissive, decoding its digit characters as an unsigned integer");
            else
            {
                ctx.Edition.Error("COBOLNET0844", $"{what} is not a numeric operand: ISO §8.8.1.1 admits only an "
                    + "identifier referencing a NUMERIC data item, a numeric literal, or the figurative constant "
                    + "ZERO in an arithmetic expression." + PermissiveDecodingHint(decodable));
                return BoundExprError.Refused(ctx.Edition, $"{what} in an arithmetic expression (ISO §8.8.1.1)");
            }
        }
        return new BoundNumRef(p);
    }

    /// <summary>A human-readable description of WHY a place is not a numeric operand — the DESCRIPTION half only.
    /// <para>
    /// ⛔ IT NO LONGER DECIDES (kb/Work PB170). This was a private, incomplete re-statement of §8.5.2.1 Table 2
    /// whose null return meant "admissible", and the arms it lacked were the defect: an index DATA item
    /// (<c>PicInfo.IndexItem</c> carries category NUMERIC for the storage model, so it fell through the category
    /// arms), a pointer and an object reference all returned null and computed silently under STRICT, while the
    /// receiving-side twin <see cref="ScreenResultant"/> rejected the first of them by name — two arms of one
    /// rule, one written. The verdict is now <see cref="IntrinsicArgumentRules.IsArithmeticOperandClass"/>'s, over
    /// the ONE Table-2 classifier, and this method is called only AFTER that verdict is "reject". Its arms are
    /// therefore TOTAL: the trailing class-named arm catches every shape the specific ones do not.
    /// </para>
    /// <para>
    /// ⛔ <b>A NUMERIC-EDITED ITEM IS REJECTED (owner decision 2026-08-02, reversing DA6's admission).</b> This
    /// arm previously read "a numeric-EDITED item de-edits to a defined numeric value and is admissible", and it
    /// was derived from the wrong place — from what de-editing CAN do, not from what §8.8.1.1 ADMITS. The
    /// standard decides it three ways, all validated with <c>cite.py --check</c>:
    /// <list type="bullet">
    ///   <item>§8.8.1.1 admits "an identifier referencing a <b>numeric data item</b>", and §8.5.2.13 says such an
    ///         item "is referred to as a <b>numeric-edited data item</b>" — a DISTINCT defined term. Table 2
    ///         (§8.5.2.1) puts category numeric-edited in class ALPHANUMERIC (usage display) or NATIONAL, never
    ///         class numeric. It is neither the class nor the category §8.8.1.1 names.</item>
    ///   <item>Every de-editing rule in the standard is a MOVE/editing rule — §14.9.25.4 GR6d1 ("de-editing
    ///         establishes the operand's numeric value"), the CURRENCY SIGN and LOCALE clauses. GR6d1 has to
    ///         GRANT de-editing for the MOVE, which it would not need to do if de-editing were generally
    ///         available to any numeric context.</item>
    ///   <item>§15.3's integer type 6 offers "an arithmetic expression … or an <b>integer data item</b>" as its
    ///         only two alternatives; a numeric-edited item is neither.</item>
    /// </list>
    /// The sibling <c>IntrinsicArgumentRules</c> 'n' arm had ALREADY refuted this same reading (its negative
    /// fixture <c>pb1-numeric-arg-numeric-edited</c> pins it), so the two screens rested on readings of §8.8.1.1
    /// that could not both be right. They now agree. ⚠ This changes ONLY what an ARITHMETIC operand may be — the
    /// 's' string family still admits a numeric-edited item, because Table 2 genuinely makes it class
    /// alphanumeric and a CLASS rule means what it says.
    /// </para></summary>
    private static string NonNumericOperandKind(Place p) => p switch
    {
        RefModPlace => "a reference-modified operand (class alphanumeric, ISO §8.4.3.3.4 GR6)",
        _ when p.Item.IsGroup => $"group item '{p.Item.CobolName}' (class alphanumeric, ISO §8.5)",
        // The INDEX arm the old switch lacked (kb/Work PB170): §8.5.2.1 Table 2 puts an index data item in class
        // INDEX, and §13.18.60.3 SR10's closed reference list — "a SEARCH or SET statement, a relation condition,
        // an intrinsic function argument" — has no arithmetic-operand entry. Named by the same words
        // ScreenResultant already used for the receiving side, so the two halves of one rule read alike.
        _ when p.Item.Pic is { Usage: Usage.Index } =>
            $"item '{p.Item.CobolName}', an index data item (class index, ISO §8.5.2.1 Table 2; §13.18.60.3 SR10 "
            + "admits an index data item only in SEARCH/SET, a relation condition, or an intrinsic argument)",
        _ when p.Item.Pic is { Category: PicCategory.NumericEdited } =>
            $"item '{p.Item.CobolName}' of category numeric-edited (a numeric-edited data item is not a NUMERIC "
            + "data item — ISO §8.5.2.13 + §8.5.2.1 Table 2; de-editing is a MOVE rule, §14.9.25.4 GR6d1)",
        // NO EditMask condition (kb/Work PB155): an alphanumeric-edited/national-edited picture is modeled as
        // Category Alphanumeric/National WITH an EditMask (there is no *Edited enum member for them), and the
        // old `EditMask: null` pattern let `ADD <PIC XXBXX> TO N` slip past the screen and digit-decode under
        // STRICT — the --permissive leniency applied unconditionally. Edited or plain, class alphanumeric or
        // national is not class numeric (ISO §8.5.2.1 Table 2 / §8.8.1.1).
        _ when p.Item.Pic is { Category: PicCategory.Alphanumeric or PicCategory.National } pic =>
            $"item '{p.Item.CobolName}' of category {pic.Category.ToString().ToLowerInvariant()}"
            + (pic.EditMask is not null ? "-edited (an edited item is not a NUMERIC data item — ISO §8.5.2.1 "
                + "Table 2; the de-editing grant is MOVE's alone, §14.9.25.4 GR6d1)" : ""),
        // ⛔ TOTAL BY CONSTRUCTION. The classifier has already said "not class numeric", so there is no
        // admissible shape left to fall through to — a pointer, a program-pointer, an object reference, a PIC A
        // alphabetic item and a bit/national group all land here and are NAMED rather than silently admitted
        // (which is what the former `_ => null` did for the first three).
        _ => $"item '{p.Item.CobolName}' of class "
            + $"{IntrinsicArgumentRules.ClassOfPlace(p)?.ToString().ToLowerInvariant() ?? "unknown"} "
            + "(ISO §8.5.2.1 Table 2)",
    };

    /// <summary>Reject a non-numeric constant-name in a numeric-expression position (ISO §8.8.1.1 — arithmetic
    /// operands shall be numeric; the constant stands for its literal, §13.10.3 SR2), mirroring the written
    /// national/boolean-literal rejection above (COBOLNET0844).</summary>
    private BoundExprError NonNumericConstantExpr(string name, PicCategory category)
    {
        ctx.Edition.Error("COBOLNET0844", $"constant-name '{name}' substitutes a literal of category "
            + $"{category} and is not a numeric operand (ISO §8.8.1.1 / §13.10.3 SR2)");
        return BoundExprError.Refused(ctx.Edition, $"constant-name '{name}' in a numeric context");
    }

    /// <summary>Resolve a LINAGE-COUNTER reference to its file (ISO §8.4.3.14): in the grammar alternative
    /// <c>LINAGE_COUNTER ((OF|IN) cobolWord)?</c> the cobolWord IS the file-name qualifier. Unqualified, the
    /// register resolves only when exactly ONE file has a LINAGE clause — with several, qualification is
    /// required (§8.4.3.14 SR3 / §8.4.2.2). Null (the caller binds a loud error) for no/an ambiguous match,
    /// with a bind-time diagnostic naming the rule.</summary>
    private FileModel? LinageFileOf(Core.DataReferenceContext dref)
    {
        // ⛔ §8.4.3.14.3 SR1 — "LINAGE-COUNTER may be referenced only in procedure division statements". The report
        // section's clause expressions (SOURCE, PRESENT WHEN, SUM, VARYING) bind through THIS binder with the report in
        // scope, and this is the ONE producer of the LINAGE-COUNTER operand, so one test here refuses every such position
        // — present and future — by BINDING CONTEXT; before it, `SOURCE LINAGE-COUNTER + 1` and `PRESENT WHEN
        // LINAGE-COUNTER > 1` bound and read the live counter (kb/Work PB1431).
        if (ctx.Refs.ReportScope is { } report)
        {
            DataBinder.ReportLinageCounterOutsideProcedure(ctx.Edition, dref, $"RD '{report.Name}': clause operand", DataBinder.WrittenText(dref));
            return null;
        }
        if (dref.cobolWord() is { } q)   // qualified: LINAGE-COUNTER OF/IN file-name
        {
            if (ctx.Data.FilesByName.TryGetValue(q.GetText(), out var named) && named.Linage is not null) return named;
            ctx.Edition.Error(DiagnosticCatalog.FileKeyClauseRule, $"LINAGE-COUNTER OF '{q.GetText()}': the qualifier shall name a "
                + "file whose file description entry contains a LINAGE clause (ISO §8.4.3.14 / §13.18.34.4 GR7a)");
            return null;
        }
        // The VISIBLE set, not the program's own FD list (kb/Work PB123's sweep): FilesByName carries the
        // containers' GLOBAL FDs too (§13.18.27.4 GR1), so a contained program whose only LINAGE file is the
        // container's GLOBAL one resolves the unqualified register instead of drawing COBOLNET0864; two
        // visible LINAGE files — own plus inherited — still require qualification (§8.4.3.14 SR3).
        var linageFiles = ctx.Data.FilesByName.Values.Where(f => f.Linage is not null).Distinct().ToList();
        if (linageFiles.Count == 1) return linageFiles[0];
        ctx.Edition.Error("COBOLNET0864", linageFiles.Count == 0
            ? "LINAGE-COUNTER referenced, but no file description entry contains a LINAGE clause (ISO §8.4.3.14 — "
              + "the register is generated by the presence of a LINAGE clause)"
            : "unqualified LINAGE-COUNTER with more than one LINAGE file: qualify by file-name (ISO §8.4.3.14 "
              + "SR3 / §8.4.2.2 Qualification)");
        return null;
    }

    // Receiving references resolve through ResolveReceiving below — the ONE
    // receiving-side chokepoint: a report counter receiver is rejected at bind (LINE-COUNTER illegal per ISO
    // §8.4.3.15 SR3; PAGE-COUNTER staged loud) instead of being SILENTLY dropped by .OfType<Place>() (§1.4).
    public List<Place> ResolveTargets(IEnumerable<Core.DataReferenceContext> targets) =>
        targets.Select(ResolveReceiving).OfType<Place>().ToList();

    // ── ROUNDED phrase → rounding mode + receiver resolution (ISO §14.7.4) ───────────────────────────────────

    /// <summary>The rounding mode a (possibly absent) ROUNDED phrase selects (ISO §14.7.4.3). No phrase → TRUNCATION
    /// (rule 2); a bare <c>ROUNDED</c> → the program's DEFAULT ROUNDED mode (rule 1 / §11.9.6 — the OPTIONS
    /// <c>DEFAULT ROUNDED MODE IS x</c> clause, defaulting to NEAREST-AWAY-FROM-ZERO when absent); an explicit
    /// <c>MODE IS x</c> → the named mode (via the shared <see cref="RoundingModes"/> mapping).</summary>
    public CobolRounding RoundingOf(Core.RoundedPhraseContext? phrase)
    {
        if (phrase is null) return CobolRounding.Truncation;
        if (phrase.roundingModeName() is { } mode)
        {
            // The explicit MODE IS phrase (and the 8-mode set) is ISO 2014+ (§14.7.4); at 85/2002 a bare ROUNDED
            // means the single nearest-away-from-zero rounding. The RoundedModeIs2014 introduction gate fires on
            // RECOGNITION in the VersionConformancePass parse-arm (VisitRoundedPhrase, roundingModeName != null); 14h.4a.
            return RoundingModes.Map(mode);
        }
        return ctx.Options.DefaultRounding;
    }

    // ── The RECEIVING chokepoint (hoisted from the ReportWriter partial at 10f; HOME here since 10q —
    //    the shared receiving spine the arithmetic/MOVE/SET pipelines consume). ──

    /// <summary>Resolve a data reference that the statement does NOT store into — a sending operand, a key or
    /// record NAME, a CALL/INVOKE target, a pointer or object reference that is only read — to its
    /// <see cref="Place"/>: the demanding reference resolver (<see cref="ReferenceResolver.Resolve"/>) and
    /// nothing else, because every screen <see cref="ResolveReceiving"/> adds is a RECEIVING-operand rule
    /// (§13.18.15.3 SR2, §8.4.3.15.3 SR3, §8.4.3.6.3 SR1, §13.10.4 GR1) and would be a false rejection here.
    /// <para>⛔ WHY A VERB BINDER NEVER CALLS <c>ctx.Refs.Resolve</c> ITSELF (kb/Work PB881). The resolver answers
    /// only "where does this live", so a binder that reached it directly for a RECEIVING operand opted out of every
    /// receiving-operand prohibition in silence: <c>INSPECT CA REPLACING …</c> and <c>READ … INTO CA</c> rewrote a
    /// CONSTANT RECORD that the identical MOVE refuses (§13.18.15.3 SR2: "Neither the data item described by the
    /// subject of the entry nor any data item subordinate to the subject of the entry shall be specified as a
    /// receiving data item" — a rule over the OPERAND ROLE, never over a statement). Two named entries make the
    /// role a decision written at every site — this one or <see cref="ResolveReceiving"/> — and
    /// <c>ReceivingResolutionDriftTests</c> fails the build of a binder under <c>Verbs/</c> that bypasses both.</para></summary>
    /// <para>⛔ IT RETURNS THE RESOLVER'S CLOSED ANSWER (kb/Work PB1030), so a caller chooses its refusal node FROM
    /// it — <see cref="RefResolution.Refusal"/> and its operand/expression/condition twins — instead of guessing
    /// whether a null was reported or unbuilt, or reporting an "unresolvable" error of its own on top.</para>
    /// <para>⛔ A REPORT'S PAGE-COUNTER RESOLVES HERE AS IT DOES ON THE RECEIVING SIDE (kb/Work PB1137, the sending twin
    /// of PB429). §8.4.3.15.3 SR1 admits it "in any context where an integer data item may appear", and the ordinary
    /// resolver builds no place for a special register, so a position that needs the SENDING operand's Place — an
    /// INVOKE argument passed BY CONTENT, which §14.9.23.4 GR6 a) 2. makes of a keyword-less PAGE-COUNTER — was
    /// deferred as "a special register in a position that does not yet read it". The place is the one
    /// <see cref="ResolveReceiving"/> builds, through the ONE counter-to-report resolution (a null there has already
    /// reported the qualification rule, so it is a refusal, never a deferral).</para>
    public RefResolution ResolveSending(Core.DataReferenceContext dref) =>
        dref.PAGE_COUNTER() is null ? ctx.Refs.Resolve(dref)
        : host.Rw.CounterPlace(dref) is { } counter ? RefResolution.Resolved(counter, DataBinder.WrittenText(dref))
        : RefResolution.Refused(DataBinder.WrittenText(dref));

    /// <summary>Resolve a RECEIVING data reference to its <see cref="Place"/> — the ONE receiving-side
    /// chokepoint (MOVE targets, arithmetic resultants, SET receivers). A report counter here is rejected at
    /// bind time: LINE-COUNTER shall not be a receiving operand (ISO §8.4.3.15 SR3 — illegal); PAGE-COUNTER as a
    /// receiver is legal but not yet implemented (staged loud). Without this guard the
    /// <c>.OfType&lt;Place&gt;()</c> receiver pipelines would DROP the counter silently — a silent-miscompile
    /// hazard (§1.4).</summary>
    /// <para>⛔ ITS PROHIBITIONS ARE A TABLE, ASKED BY TWO QUESTIONS (kb/Work PB1418). "Is this identifier permitted as
    /// a receiving operand?" is also a question the standard asks WITHOUT the operand being stored into: §8.4.3.2.4
    /// GR5 a)/b) choose BY REFERENCE or BY CONTENT for a function argument by exactly that test ("any identifier
    /// that is not permitted as a receiving operand" crosses BY CONTENT). The prohibitions therefore live in
    /// <see cref="ReceivingFormBar"/> (the reference's FORM, asked before resolution) and
    /// <see cref="ReceivingPlaceBar"/> (the resolved PLACE), which this chokepoint REPORTS and
    /// <see cref="PermitsReceiving"/> only ASKS — so the two can never disagree about which items are
    /// receivable.</para></summary>
    public Place? ResolveReceiving(Core.DataReferenceContext dref)
    {
        if (ReceivingFormBar(dref) is { } formBar) return Refuse(formBar);
        // ⛔ PAGE-COUNTER IS A RECEIVING-CAPABLE PLACE, NOT A PER-VERB PERMISSION (kb/Work PB429). §8.4.3.15.3
        // SR1 — "In the procedure division, PAGE-COUNTER and LINE-COUNTER may be referenced in any context where
        // an integer data item may appear" — names no statement, and SR3 subtracts LINE-COUNTER and ONLY
        // LINE-COUNTER from the receiving side, so every integer-receiver context admits PAGE-COUNTER. Resolving
        // it HERE, at the one chokepoint, is what makes MOVE, every arithmetic resultant, INITIALIZE, INSPECT
        // TALLYING and every context added later work without an arm apiece — which is why the refusal this
        // replaced (COBOLNET0899 "not yet implemented", the compiler conceding the construct was legal) could
        // not be repaired verb by verb. The report is resolved by the SAME method the sending side uses.
        if (dref.PAGE_COUNTER() is not null) return host.Rw.CounterPlace(dref);
        var answer = ctx.Refs.Resolve(dref);
        var place = answer.Place;
        // ⛔ THE ONE RECEIVING CHOKEPOINT NEVER DROPS A RECEIVER SILENTLY (kb/Work PB70): `MOVE "Z" TO OK1 TB(2:1) OK2`
        // used to move into OK1 and OK2 and skip TB without a word — the resolver's unsupported-shape null fell
        // through .OfType<Place>() in ResolveTargets / Receivers. An undefined name or a rejected shape was already
        // reported by the resolver (WasDiagnosed); a name that RESOLVED but whose reference shape this compiler does
        // not implement as a receiver is reported HERE, recognized-not-implemented, so the compilation fails instead
        // of the statement running one receiver short.
        if (place is null)
        {
            // ⛔ THE RESOLVER'S ANSWER, NOT A GUESS (kb/Work PB1030): only a DEFERRED shape is reported here.
            if (answer.Outcome == RefOutcome.Deferred)
                ctx.Edition.Error(DiagnosticCatalog.ReceivingReferenceNotImplemented,
                    $"receiving operand '{DataBinder.WrittenText(dref)}' names a declared item in a reference shape WiseOwl COBOL does "
                    + "not yet implement as a receiver (COBOLNET_DESIGN §1.4 — rejected rather than dropped)");
            // ⛔ A REPORTED REFUSAL IS A CLAIM THE REFUSAL LEDGER CHECKS (kb/Work PB1407). The sending arm builds a
            // refusal NODE through a factory that records itself, so a resolver that said "reported" and did not is
            // caught per statement (COBOLNET2362). This receiving arm builds no node — the caller simply drops the
            // receiver — so the claim was never checked, and `MOVE "A" TO X (:2)` compiled CLEAN with the whole MOVE
            // missing. Recording the refusal here puts the receiving arm under the same per-statement check.
            else ctx.Edition.NoteRefusal(DiagnosticCatalog.UnreportedRefusal, $"receiving operand '{DataBinder.WrittenText(dref)}'");
            return null;
        }
        if (ReceivingPlaceBar(dref, place) is { } placeBar) return Refuse(placeBar);
        return place;

        Place? Refuse(ReceivingBar bar)
        {
            ctx.Edition.Error(bar.Code, bar.Message);
            return null;
        }
    }

    /// <summary>Whether <paramref name="dref"/>, already resolved (as a SENDING operand) to <paramref name="place"/>,
    /// is "an identifier that is permitted as a receiving operand" — the test ISO §8.4.3.2.4 GR5 a)/b) choose a
    /// function argument's passing manner by. Asks the SAME prohibitions <see cref="ResolveReceiving"/> reports,
    /// and reports nothing (the operand is legal either way; only its manner changes).</summary>
    internal bool PermitsReceiving(Core.DataReferenceContext dref, Place place) =>
        ReceivingFormBar(dref) is null && ReceivingPlaceBar(dref, place) is null;

    /// <summary>One receiving-operand prohibition: the diagnostic code it reports under and its message.</summary>
    private readonly record struct ReceivingBar(string Code, string Message);

    /// <summary>The prohibitions decided by the reference's FORM, before it is resolved — a report counter the
    /// input-output control system owns, the predefined EXCEPTION-OBJECT, and a constant-name (a literal).</summary>
    private ReceivingBar? ReceivingFormBar(Core.DataReferenceContext dref)
    {
        if (dref.LINE_COUNTER() is not null)
            return new(DiagnosticCatalog.ReportLineCounterReceiving.Code,
                "LINE-COUNTER shall not be referenced as a receiving operand (ISO §8.4.3.15.3 SR3)");
        // ⛔ THE THIRD ARM OF THE SAME DISPATCH (kb/Work PB489). LINE-COUNTER above is rejected by rule and
        // PAGE-COUNTER is a receiving-capable place (ResolveReceiving resolves it); LINAGE-COUNTER — the only one of the three
        // that is flatly ILLEGAL as a receiver — had no arm at all and fell through to the generic
        // recognized-not-implemented catch-all, so permanently illegal source was reported as a promise. The
        // rule that forbids it appeared nowhere in the compiler (feedback_two_arm_dispatch).
        if (dref.LINAGE_COUNTER() is not null)
            return new(DiagnosticCatalog.LinageCounterReceiving.Code,
                "LINAGE-COUNTER shall not be referenced as a receiving operand (ISO §8.4.3.14.3 SR2); "
                + "only the input-output control system may change its value (ISO §13.18.34.4 GR7 b)");
        // ⛔ THE FOURTH ARM OF THE SAME DISPATCH (kb/Work PB922). ISO §8.4.3.6.3 SR1 — "EXCEPTION-OBJECT shall
        // not be specified as a receiving operand" — is a rule about EVERY receiving operand in the language,
        // not about SET, which is the one statement that had an arm for it. The name RESOLVES now (the resolver
        // knows §8.4.3.6's predefined object reference), so without this screen a receiver would fall through to
        // a place whose PlaceRenderer.Write is an internal-error backstop; before it resolved, the reference drew
        // "'EXCEPTION-OBJECT' is not defined — Check the spelling, or declare the item" beside "'EXCEPTION-OBJECT'
        // is a reserved word … and cannot be used as a user-defined word": two diagnostics that contradict each
        // other, and neither of them the rule the program broke.
        // ⛔ THE RESOLVER'S OWN PREDICATE, NOT A SECOND SPELLING TEST: it carries the §8.9/2002 edition gate, so
        // a '85 program that legally declares `01 EXCEPTION-OBJECT PIC X(4).` keeps its ordinary receiving path.
        if (ctx.Refs.IsExceptionObjectRegister(dref))
            return new(DiagnosticCatalog.ExceptionObjectReceiving.Code,
                "EXCEPTION-OBJECT shall not be specified as a receiving operand (ISO §8.4.3.6.3 SR1) — it is "
                + "the predefined object reference for the CURRENT exception object (§8.4.3.6.4 GR1), set by the "
                + "run unit when an exception is raised");
        // §8.4.3.5.3 SR2 — "An object-view shall not be specified as a receiving operand" — for the view of a qualified data
        // item the grammar spells as one chain (`A OF G AS C`, kb/Work PB1425); the property of a view (`P OF U AS C`) is
        // a property and receivable (§8.4.3.9.3 SR6), and a view of a plain name parses as `objectViewReceiver`.
        if (ctx.Refs.IsObjectViewOfQualifiedItem(dref))
            return new(DiagnosticCatalog.ObjectViewReceiving.Code, $"'{DataBinder.WrittenText(dref)}' is written as a "
                + "receiving operand: an object-view shall not be specified as a receiving operand (ISO §8.4.3.5.3 SR2)");
        // A constant-name substitutes a LITERAL (ISO §13.10.3 SR2 / §13.10.4 GR1) — a literal can never be a
        // receiving operand; without this the name would fall to Refs.Resolve and fail as merely "unresolved".
        if (ctx.Data.ConstantOf(dref) is not null)
            return new(DiagnosticCatalog.ConstantAsReceiver.Code, $"constant-name '{DataBinder.WrittenText(dref)}' shall "
                + "not be specified as a receiving operand — it substitutes a literal (ISO §13.10.4 GR1)");
        return null;
    }

    /// <summary>The prohibitions decided by the RESOLVED place — the OCCURS DYNAMIC CAPACITY register and a
    /// CONSTANT RECORD's content.</summary>
    private ReceivingBar? ReceivingPlaceBar(Core.DataReferenceContext dref, Place place)
    {
        // The OCCURS DYNAMIC CAPACITY register (§13.18.38 SR30–32; D9) is set ONLY by a SET Format 14 statement
        // (which reroutes BEFORE this chokepoint). Any other receiving use — MOVE/arithmetic resultant/ordinary SET
        // receiver — is illegal; reject it here rather than reach CapacityRegisterPlace.Write (an internal throw).
        if (place is CapacityRegisterPlace cap)
            return new("COBOLNET1523", $"the CAPACITY register '{cap.RegisterItem.CobolName}' shall not be a "
                + "receiving operand except in a SET statement Format 14 (ISO §13.18.38.3 SR30–32)");
        // ISO §13.7.3 SR5 (kb/Work PB1250): "A formal parameter of a function shall not be used as a receiving operand."
        // A rule over the operand ROLE, so it is asked HERE and every statement's receiver — and §8.4.3.2.4 GR5's
        // BY-CONTENT choice for an argument — inherits it. The parameter in any reference form (whole or
        // reference-modified) is barred; an item subordinate to a group parameter is not "a formal parameter" (SR4 b)
        // of the same clause says "subordinate to" when it means it), and the RETURNING item is no formal parameter.
        if (ctx.Data.IsFunctionFormal(place.Item))
            return new(DiagnosticCatalog.FunctionFormalReceiving.Code,
                $"'{DataBinder.WrittenText(dref)}' is a formal parameter of this function and shall not be used as a "
                + "receiving operand (ISO §13.7.3 SR5); store into the RETURNING item or a WORKING-STORAGE copy");
        // A CONSTANT RECORD's content cannot be modified — neither the record nor any subordinate may be a
        // receiving operand (ISO §13.18.15.3 SR2 → COBOLNET1548; DataBinder.ConstantStoreProhibition).
        return ctx.Data.ConstantStoreProhibition(place, $"receiving operand '{DataBinder.WrittenText(dref)}'")
            is { } constant ? new(DiagnosticCatalog.ConstantAsReceiver.Code, constant) : null;
    }

    /// <summary>The arithmetic RESULTANT category screen (kb/Work PB128): every arithmetic statement's syntax
    /// rules fix its resultants' categories — the in-place receivers (ADD TO §14.9.2.3 SR2, SUBTRACT FROM
    /// §14.9.44.3 SR2, MULTIPLY BY §14.9.26.3 SR1, DIVIDE INTO §14.9.12.3 SR1) shall be elementary NUMERIC;
    /// the GIVING resultants, DIVIDE's REMAINDER (§14.9.12.3 SR2 — edited IS admitted there) and COMPUTE's
    /// identifier-1 (§14.9.8.3 SR1) admit numeric or NUMERIC-EDITED. Nothing screened this before: a PIC X or
    /// group resultant compiled clean and died in StoreArith's run-time loud, where §4.2.2 requires a
    /// compile-time mechanism (the SENDING side has had DA6's screen for a month). Also rejected here: an
    /// index DATA item (category numeric in the storage model, but §13.18.60.3 SR10's closed reference list
    /// has no arithmetic-resultant entry) and a ref-mod slice (§8.4.3.3.4 GR6c — category alphanumeric).
    /// <para>⛔ WHY THIS IS NOT ROUTED THROUGH <c>IntrinsicArgumentRules.IsArithmeticOperandClass</c>, ASKED AND
    /// ANSWERED (cluster-B review, sweep 478 — "a surviving fourth class-answer copy"). It is not a fourth copy:
    /// it answers a DIFFERENT question on the OTHER side of the statement. <c>IsArithmeticOperandClass</c> is
    /// §8.8.1.1's SENDING question — "is this operand of class numeric" — and has no position axis, because
    /// §8.8.1.1 has none. A RESULTANT's admissibility is fixed per-clause by the statement's own syntax rules and
    /// turns on an axis §8.8.1.1 does not possess: numeric-edited is admitted at GIVING / DIVIDE's REMAINDER
    /// (§14.9.12.3 SR2) / COMPUTE's identifier-1 (§14.9.8.3 SR1) and BARRED at the in-place receivers (ADD TO
    /// §14.9.2.3 SR2, SUBTRACT FROM §14.9.44.3 SR2, MULTIPLY BY §14.9.26.3 SR1, DIVIDE INTO §14.9.12.3 SR1) —
    /// which is the <paramref name="editedOk"/> parameter. Collapsing the two would either lose that axis or
    /// smuggle a receiver rule into the sending screen. The two DO share the class vocabulary
    /// (<c>ClassOfPlace</c> / <c>ClassOfItem</c>) already, which is the part that must not fork.</para></summary>
    internal Place? ScreenResultant(Place p, string refText, bool editedOk, string clause)
    {
        var pic = p.Item.Pic;
        // ⛔ THE IDENTITY QUESTION, ASKED ONCE (kb/Work PB602). It used to be `p is not RefModPlace` — PB128's
        // route AROUND the fact that a decorated place reported the item underneath — and §8.4.3.3.4 GR5's
        // "unique data item that is a subset of the data item referenced by identifier-1" is now what
        // Place.DenotedItem answers, for every screen that asks it, rather than once per caller.
        // ⛔ THE ANALYZED CATEGORY (kb/Work PB960). A recovery profile — an entry whose PICTURE was already
        // rejected at its declaration — has no category to screen, so the resultant is admitted rather than
        // re-diagnosed as "of category Alphanumeric": the declaration's own diagnostic is the true one.
        bool elementary = p.DenotedItem is not null && !p.Item.IsGroup;
        if (elementary && pic is { AnalyzedCategory: null }) return p;
        bool numeric = elementary
            && pic is { AnalyzedCategory: PicCategory.Numeric, Usage: not Usage.Index };
        bool edited = elementary
            && pic is { AnalyzedCategory: PicCategory.NumericEdited };
        if (numeric || (edited && editedOk)) return p;
        string actual = p is RefModPlace ? "a reference-modified slice (category alphanumeric, §8.4.3.3.4 GR6c)"
            : p.Item.IsGroup ? "a group item"
            : pic is { Usage: Usage.Index } ? "an index data item (§13.18.60.3 SR10)"
            : edited ? "a numeric-edited item, which this receiver position does not admit"
            : $"of category {pic?.Category.ToString() ?? "unknown"}";
        ctx.Edition.Error(DiagnosticCatalog.ArithmeticResultantCategory,
            $"the arithmetic resultant '{refText}' is {actual}; ISO {clause} requires an elementary numeric"
            + (editedOk ? " or numeric-edited" : "") + " data item");
        return null;
    }

    /// <summary>⛔ THE ONE CONSTRUCTION OF AN ARITHMETIC <see cref="Receiver"/> (kb/Work PB2078). Every arithmetic
    /// emitter stores its receivers one at a time through <c>ReceiverBracketEmitter.Receive</c>, so a receiver that is an
    /// object property has its accessors placed around ITS store (ISO §14.7.7 4) b): "Item identification for the
    /// receiving data items is done as each data item is accessed") — the CLAIM that says so is made here, where the
    /// receiver list is built, and nowhere else.</summary>
    private Receiver ReceiverOf(Place resolved, Core.RoundedPhraseContext? rounded)
    {
        ctx.Data.OoClaimInterleavedReceiver(resolved);
        return new Receiver(resolved, RoundingOf(rounded));
    }

    /// <summary>Resolve <c>receivingArithmeticOperand</c>s (the GIVING / TO / FROM / INTO resultants) to
    /// <see cref="Receiver"/>s, each carrying its own ROUNDED mode and screened per the caller's syntax rule
    /// (<paramref name="editedOk"/> — GIVING-style positions admit numeric-edited, in-place ones do not);
    /// an unresolvable or rejected reference is dropped after its diagnostic.</summary>
    public List<Receiver> Receivers(IEnumerable<Core.ReceivingArithmeticOperandContext> ops, bool editedOk, string clause) =>
        ops.Select(o => ResolveReceiving(o.dataReference()) is { } p
                && ScreenResultant(p, o.dataReference().GetText(), editedOk, clause) is { } sp
                ? ReceiverOf(sp, o.roundedPhrase()) : null)
           .OfType<Receiver>().ToList();

    /// <summary>Resolve the in-place <c>MULTIPLY … BY</c> receivers (<c>multiplyByOperand</c> = receiving operand +
    /// optional ROUNDED), each carrying its own mode; a literal BY operand (only valid in a GIVING form) is dropped.</summary>
    public List<Receiver> Receivers(IEnumerable<Core.MultiplyByOperandContext> ops) =>
        ops.Select(o => o.receivingOperand()?.dataReference() is { } d && ResolveReceiving(d) is { } p
                && ScreenResultant(p, d.GetText(), editedOk: false, "§14.9.26.3 SR1") is { } sp
                ? ReceiverOf(sp, o.roundedPhrase()) : null)
           .OfType<Receiver>().ToList();

    /// <summary>Resolve the <c>COMPUTE</c> Format-1 resultants (<c>computeStore</c> = data reference + optional
    /// ROUNDED) — §14.9.8.3 SR1 admits elementary numeric or numeric-edited.</summary>
    public List<Receiver> Receivers(IEnumerable<Core.ComputeStoreContext> stores) =>
        stores.Select(s => ResolveReceiving(s.dataReference()) is { } p
                && ScreenResultant(p, s.dataReference().GetText(), editedOk: true, "§14.9.8.3 SR1") is { } sp
                ? ReceiverOf(sp, s.roundedPhrase()) : null)
              .OfType<Receiver>().ToList();

    /// <summary>Bind any numeric node (expression, operand wrapper, literal, or data reference) as an ISO §8.8.1.1
    /// ARITHMETIC expression — numeric operands only. THE entry for COMPUTE, the arithmetic verbs and
    /// reference-modifier offsets; the §13.18.38.3 r7 index-name windows (subscripts, SET, PERFORM/SEARCH
    /// VARYING, relation/EVALUATE operands) moved to <see cref="BindIndexWindowExpr"/> at R29 (kb/Work PB155
    /// re-unified the §8.8.1.1 screening the move had silently dropped there).
    /// <para>Deliberately takes NO context parameter: an optional one would break the method-group conversions this
    /// spine is used through (<c>Select(host.Expr.BindExpr)</c>) and would put a bare <c>true</c> at a call site.
    /// The contexts that differ have their own named entries, <see cref="BindFunctionArgumentExpr"/> and
    /// <see cref="BindByValueExpr"/>.</para></summary>
    public BoundExpr BindExpr(IParseTree node) => BindExprCore(node, OperandContext.Arithmetic);

    /// <summary>Bind an arithmetic expression at a position BOTH index clauses list — SET amounts/values — where an
    /// index-name (§13.18.38.3 r7) and an index data item (§13.18.60.3 SR10) are legal operands (kb/Work R29,
    /// PB215). Identical to <see cref="BindExpr"/> otherwise. A relation or EVALUATE operand is not one (PB2018).</summary>
    public BoundExpr BindIndexWindowExpr(IParseTree node) => BindExprCore(node, OperandContext.ArithmeticIndexWindow);

    /// <summary>Bind an arithmetic expression at a position §13.18.38.3 r7 lists and §13.18.60.3 SR10 does not —
    /// a subscript segment: an index-NAME is legal, an index DATA ITEM is not (kb/Work PB215).</summary>
    public BoundExpr BindIndexNameWindowExpr(IParseTree node) =>
        BindExprCore(node, OperandContext.ArithmeticIndexNameWindow);

    /// <summary>Bind a <c>CALL … USING BY VALUE</c> operand. Identical to <see cref="BindExpr"/> except that the
    /// §8.8.1.1 numeric-operand screen does not apply: the operand's legality is ISO §14.9.4.3 SR22's business
    /// ("identifier-4 shall be of class numeric, object, or pointer"), which the CALL binder enforces with its own
    /// diagnostic. Binding it as arithmetic quoted §8.8.1.1 at a programmer who had broken SR22.</summary>
    public BoundExpr BindByValueExpr(IParseTree node) => BindExprCore(node, OperandContext.CallByValue);

    /// <summary>Bind an INTRINSIC-FUNCTION ARGUMENT expression. Identical to <see cref="BindExpr"/> except that the
    /// §8.8.1.1 numeric-operand screen does not apply: an argument's legality is governed by the individual
    /// function's §15.x argument rule, which for the string functions admits alphanumeric data
    /// (<c>FUNCTION TRIM(S)</c> over a <c>PIC X</c> item is legal). The grammar reuses the
    /// <c>arithmeticExpression</c> production for arguments, which is why the distinction has to be made by the
    /// CALLER and cannot be inferred at the leaf.</summary>
    public BoundExpr BindFunctionArgumentExpr(IParseTree node) => BindExprCore(node, OperandContext.FunctionArgument);

    /// <summary>The ONE recursive expression spine. <paramref name="context"/> rides every recursive call rather
    /// than living in a field — the same discipline the render-side receiver follows (P7 Step 3: "travels by
    /// parameter into every public entry, never mutable context state"), and for the same reason: ambient state
    /// goes stale across a re-entrant descent.</summary>
    private BoundExpr BindExprCore(IParseTree node, OperandContext context) => node switch
    {
        Core.ArithmeticExpressionContext a => BindExprCore(a.GetChild(0), context),
        Core.AdditiveExpressionContext or Core.MultiplicativeExpressionContext => BindChain(node, context),
        Core.PowerExpressionContext p => BindPower(p, context),
        Core.UnaryExpressionContext u => u.primaryExpression() is { } pr ? BindExprCore(pr, context)
            : u.addOp().GetText() == "-" ? new BoundNegate(BindExprCore(u.unaryExpression(), context))
                : BindExprCore(u.unaryExpression(), context),
        Core.PrimaryExpressionContext pe => BindPrimary(pe, context),
        Core.LiteralContext l => NumLiteral(l),
        Core.DataReferenceContext d => RefExpr(d, context),
        _ => BindOperandExprCore(node, context),   // operand wrappers (sendingOperand, multiplyByOperand, …)
    };

    /// <summary>A numeric literal expression from a <c>literal</c> node, mapping a figurative ZERO (incl. <c>ALL ZEROS</c>)
    /// to <c>0</c> (ISO §8.3.3.6.3 r1a / §8.3.3.6.4 r4 — ZERO is the one figurative a numeric literal admits, and it is the numeric value 0); a non-numeric figurative (SPACE / HIGH-VALUE / …)
    /// in a numeric context is a loud error rather than the raw word rendered as an identifier. A national or
    /// boolean literal is NOT a numeric operand (§8.8.1.1 — arithmetic operands shall be numeric): COBOLNET0844
    /// at bind, never raw literal text spliced into the generated expression.</summary>
    /// <param name="positionRule">See <see cref="NonNumericInNumericContext"/>. Threaded so the WRAPPED literal
    /// form (<c>literal : numericLiteral | nonNumericLiteral</c>) names the same position clause the BARE
    /// <c>nonNumericLiteral</c> arm does — before this, <c>IF "ABC" IS POSITIVE</c> cited §8.8.4.7.3 SR1 and the
    /// wrapped spelling of the same operand cited §8.8.1.1 alone (kb/Work PB218). The remaining two delegating
    /// arms — <c>BindExprCore</c> and <c>RefExpr</c> — still cite §8.8.1.1 alone; that residue is DECLARED in
    /// docs/rearchitecture/DESIGN-binder-bound-tree.md's funnel entry table rather than left to be rediscovered.</param>
    private BoundExpr NumLiteral(Core.LiteralContext lit, string? positionRule = null) =>
        NonNumericInNumericContext(lit.nonNumericLiteral(), lit.GetText(), positionRule)
        ?? NumericLiteralExpr(lit.GetText());

    /// <summary>⛔ THE ONE <c>nonNumericLiteral</c> → NUMERIC-CONTEXT reading (ISO §8.8.1.1), the numeric-side twin
    /// of <see cref="NonNumericLiteralOperand"/>. Returns null when the node is absent (so the caller falls back to
    /// its numeric literal), the constant zero for figurative ZERO — the ONE figurative §8.8.1.1 admits — and a
    /// diagnosed <see cref="BoundExprError"/> for every other alternative.
    /// <para>
    /// ⛔ WHY IT IS A SPLIT AND NOT A NEW DISPATCH (kb/Work PB171). This body was reachable only through a
    /// <c>Core.LiteralContext</c>, and the grammar's <c>valueOperand : arithmeticExpression | nonNumericLiteral</c>
    /// puts a BARE <c>NonNumericLiteralContext</c> under <c>comparisonOperand</c> — no <c>literal</c> wrapper — so
    /// <see cref="BindOperandExprCore"/>'s walk had no arm for it, drained, and returned <c>BoundNumLiteral("0")</c>:
    /// <c>IF "ABC" IS POSITIVE</c> compiled clean and evaluated <c>0 &gt; 0</c>. Copying the dispatch into the walk
    /// would have made a SECOND hand-maintained list of the literal forms, which is DA3's defect exactly (three
    /// copies, one missing the hexadecimal arm). One body, two entries.
    /// </para></summary>
    /// <param name="positionRule">The clause that closes THIS position's operand list, appended to the diagnostic so
    /// the programmer is sent to the rule they broke rather than to §8.8.1.1 alone (the COBOLNET1628 lesson) —
    /// e.g. the sign condition's §8.8.4.7.3 SR1. Null in a plain arithmetic position, where §8.8.1.1 IS the rule.</param>
    private BoundExpr? NonNumericInNumericContext(Core.NonNumericLiteralContext? nn, string text, string? positionRule)
    {
        if (nn is null) return null;
        string where = positionRule is null ? "" : $" — {positionRule}";
        if (nn.concatenationExpression() is not null)
        {
            // A concatenation expression is of class alphanumeric, boolean, or national (ISO §8.8.3.2 SR1) —
            // never numeric, so it is not an arithmetic operand (§8.8.1.1): the same 0844 posture as a bare
            // national/boolean literal in a numeric context.
            ctx.Edition.Error("COBOLNET0844", "a concatenation expression is not a numeric operand "
                + "(ISO §8.8.3.2 SR1 — class alphanumeric/boolean/national; §8.8.1.1 — arithmetic operands "
                + "shall be numeric)" + where);
            return BoundExprError.Refused(ctx.Edition, $"concatenation expression '{text}' in a numeric context");
        }
        if (nn.figurativeConstant() is { } fig)
        {
            // ⛔ §8.3.3.6.3 SR1a IS ENFORCED HERE, NOT MERELY QUOTED (kb/Work PB218). SR1a: "If the literal is
            // restricted to a numeric literal, the only figurative constant permitted is ZERO (ZEROS, ZEROES)
            // WITHOUT the ALL phrase." This position IS SR1a's antecedent, derived and not assumed: §8.8.1.1
            // enumerates what an arithmetic expression may be — "an identifier referencing a numeric data item,
            // a NUMERIC LITERAL, the figurative constant ZERO (ZEROS, ZEROES)" — so the literal admitted here is
            // restricted to a numeric literal, and the figurative it names is the bare word, never ALL ZERO.
            // ⚠ THE TEST IS `ALL() is null`, AND THE PREVIOUS COMMENT'S MECHANISM WAS FALSE. It claimed "the
            // grammar routes the bare word to ZERO() and every ALL form to allLiteral()"; `figurativeConstant`
            // has a DISTINCT `ALL ZERO` alternative (CobolExpressions.g4), so for `ALL ZEROS` both `ALL()` and
            // `ZERO()` are non-null and the old `fig.zeroWord() is not null` arm admitted it. Measured before the
            // fix: `IF ALL ZEROS IS POSITIVE` compiled clean and evaluated `0 > 0`, under a comment quoting the
            // very rule that bars it — a citation enforcing nothing is worse than no citation.
            if (IsNumericRestrictedZero(fig)) return new BoundNumLiteral("0");
            // ALL ZERO: the ALL half has an edition edge (kb/Work PB1415, VCR Table 7 row 7.28) — redundant at 85,
            // COBOLNET0902 from 2002 — asked through the ONE gate the VALUE clause asks; the operand is ZERO either way.
            if (fig.zeroWord() is not null)
            {
                FigurativeAssociation.GateAllZeroAsNumeric(ctx.Edition.Edition, ctx.Edition.Sink,
                    $"figurative constant '{ConcatFolder.Spelling(fig)}' as an arithmetic operand{where}");
                return new BoundNumLiteral("0");
            }
            // The bare BoundExprError here carried no diagnostic and rendered as a RUNTIME NotImplemented —
            // the wrong stage for a syntax-rule violation (kb/Work PB155).
            ctx.Edition.Error("COBOLNET0844", $"figurative constant '{ConcatFolder.Spelling(fig)}' is not a numeric "
                + "operand (ISO §8.8.1.1 — the only figurative constant an arithmetic expression admits is ZERO "
                + "(ZEROS, ZEROES); §8.3.3.6.3 SR1a — where the literal is restricted to a numeric literal, ZERO "
                + "is permitted WITHOUT the ALL phrase)" + where);
            return BoundExprError.Refused(ctx.Edition, $"figurative constant '{ConcatFolder.Spelling(fig)}' in a numeric context");
        }
        if ((nn.NATLIT() ?? nn.BOOLLIT()) is not null)
        {
            ctx.Edition.Error("COBOLNET0844", $"a {(nn.NATLIT() is not null ? "national" : "boolean")} "
                + "literal is not a numeric operand (ISO §8.8.1.1 — arithmetic operands shall be numeric)" + where);
            return BoundExprError.Refused(ctx.Edition, $"literal '{text}' in a numeric context");
        }
        if ((nn.STRINGLIT() ?? nn.HEXLIT()) is not null)
        {
            // kb/Work PB155: this arm fell through to BoundNumLiteral carrying the QUOTED text, and the
            // emitter rendered it into the generated arithmetic — a raw Roslyn error at the wrong stage
            // (the PB94 VALUE-clause fix's arithmetic sibling). Both formats of the alphanumeric literal —
            // quoted and hexadecimal — are of class and category alphanumeric (ISO §8.3.3.2.1).
            ctx.Edition.Error("COBOLNET0844", "an alphanumeric literal is not a numeric operand "
                + "(ISO §8.8.1.1 — arithmetic operands shall be numeric; §8.3.3.2.1 — both formats of the "
                + "alphanumeric literal are of class and category alphanumeric)" + where);
            return BoundExprError.Refused(ctx.Edition, $"literal {text} in a numeric context");
        }
        // The predefined NULL in an arithmetic position: no §8.4.3.10.3 SR1 context is one, so it is the ONE SR1
        // refusal (COBOLNET2576) rather than §8.8.1.1's — NULL is not a literal whose class failed the screen.
        if (nn.predefinedNull() is { } pn)
        {
            RefusePredefinedNull(pn);
            return BoundExprError.Refused(ctx.Edition, $"NULL in a numeric context");
        }
        // ⛔ NO SILENT FALL-THROUGH. Every `nonNumericLiteral` alternative the grammar lists is covered above, so
        // reaching here means the RULE grew an alternative this screen has not read — which is exactly the
        // condition that must fail loud rather than be admitted as a number (COBOLNET_DESIGN §1.4).
        ctx.Edition.Error("COBOLNET0844", $"the literal '{text}' is not a numeric operand "
            + "(ISO §8.8.1.1 — arithmetic operands shall be numeric)" + where);
        return BoundExprError.Refused(ctx.Edition, $"literal '{text}' in a numeric context");
    }

    /// <summary>Normalize the decimal separator (DECIMAL-POINT IS COMMA, ISO §12.3.7.4 GR14 a) — the comma form
    /// canonicalizes to dot-decimal so every emit-side decoder sees one shape) and edition-gate the digit count
    /// (ISO §8.3.3.3.2 — 1..18 at COBOL-85, 1..31 at 2002+). The ONE literal chokepoint for the expression paths.</summary>
    public string CheckLiteral(string text)
    {
        text = ctx.Data.NormalizeNumericLiteral(text);
        // A floating-point literal's digits are not the fixed-point 31/18 cap's subject (§8.3.3.3.3 SR2/SR3 —
        // checked by the normalizer); what it owes is r3's "The maximum permitted value and minimum permitted value
        // of the exponent is implementor-defined" (kb/Work PB99 — beyond the range the generated C# double literal
        // was Roslyn's CS0594, never a COBOL diagnostic).
        if (NumericLiteral.IsFloatingPointForm(text))
        {
            // ⛔ ONE RANGE, IN EVERY ARITHMETIC MODE (owner decision D-B, 2026-08-30; kb/Work PB156 + PB195). The
            // literal IS its exact §8.3.3.3.3 rule-5 value wherever it appears, carried on the SDIDI lane
            // (NumericRenderer.LiteralNum), so the implementor-defined range r3 asks for is that carrier's:
            // decimal128's. The former native arm screened the same literal against IEEE binary64 and REJECTED
            // `IF 1.0E+400 > X` — a hard error on source the identical program compiled under
            // ARITHMETIC IS STANDARD-DECIMAL (PB195, measured both arms). Nothing about the literal changed
            // between those two runs, and §14.9.2.4 GR4 / §14.9.44.4 GR4 exclude only operands "described with
            // usage" binary-*/float-* — never a literal — so the binary64 bound was screening the value against
            // the carrier of a lane it need not enter.
            // (The VALUE-clause range check is a DIFFERENT rule and keeps its binary64/binary32 bound:
            // §13.18.63.3 SR2 asks for "permissible values within the range indicated by the PICTURE clause or
            // the USAGE clause" of the item being initialized (cite.py-verified verbatim; the elided "the
            // PICTURE clause or" is what makes the rule cover a fixed-point subject too, and it is exactly the
            // half this comment used to drop) — DataBinder's FitsBinaryFloat screen, which is where that belongs.)
            bool inRange = NumericLiteral.TryParseExact(text, out var sig, out int exp10);
            if (inRange)
                try { CobolDec.FromParsed(sig, exp10, CobolRounding.NearestEven); }
                catch (CobolSizeError) { inRange = false; }
            if (!inRange)
                ctx.Edition.Error(DiagnosticCatalog.FloatingLiteral, $"floating-point numeric literal '{text}': its value lies outside "
                    + "the implementor-defined exponent range for a floating-point literal — the decimal128 range, about "
                    + "1E-6176 to 9.99E+6144 (ISO §8.3.3.3.3 r3; §8.8.1.5.2 r2; CONFORMANCE.md §7)");
            return text;
        }
        int digits = text.Count(char.IsAsciiDigit);
        ctx.Edition.CheckDigitCapacity(digits, $"numeric literal '{text}'");
        return text;
    }

    private BoundExpr BindChain(IParseTree node, OperandContext context)
    {
        BoundExpr? acc = null;
        char op = '+';
        foreach (var child in StatementBinder.Children(node))
        {
            if (child is Core.AddOpContext or Core.MulOpContext) op = child.GetText()[0];
            else { var x = BindExprCore(child, context); acc = acc is null ? x : new BoundBinary(acc, op, x); }
        }
        return acc ?? new BoundNumLiteral("0");
    }

    private BoundExpr BindPower(Core.PowerExpressionContext p, OperandContext context)
    {
        var bases = p.unaryExpression();
        BoundExpr acc = BindExprCore(bases[0], context);
        for (int i = 1; i < bases.Length; i++) acc = new BoundPower(acc, BindExprCore(bases[i], context));
        return acc;
    }

    private BoundExpr BindPrimary(Core.PrimaryExpressionContext pe, OperandContext context)
    {
        if (pe.numericLiteral() is { } num) return NumericLiteralExpr(num.GetText());
        if (pe.ZERO_ARITH() is not null) return new BoundNumLiteral("0");
        if (pe.dataReference() is { } dref) return RefExpr(dref, context);
        if (pe.arithmeticExpression() is { } paren) return BindExprCore(paren, context);
        // The two §8.4.3.1.2 identifier formats that are NOT a data-name reference — Format 1 (a FUNCTION call,
        // ISO §15; StatementBinder.Intrinsics.cs) and Format 4 (an inline method invocation, §8.4.3.4; kb/Work
        // PB428) — take ONE entry, shared with the operand-wrapper walk, so both reach §8.8.1.1's class screen.
        // Formats 5 and 6 — an object-view and SELF / SUPER (kb/Work PB1425) — are identifiers of class object; they
        // take the same entry, whose screen refuses them in an arithmetic position by §8.8.1.1's class rule.
        if (((Antlr4.Runtime.ParserRuleContext?)pe.functionCall() ?? pe.inlineMethodInvocation()
                ?? (Antlr4.Runtime.ParserRuleContext?)pe.objectView() ?? pe.selfAndSuper()) is { } id)
            return BindIdentifierOperand(id, context);
        return BoundExprError.Refused(ctx.Edition, "primary-expression operand");
    }

    /// <summary>⛔ THE ONE ENTRY FOR AN OPERAND WRITTEN AS A FUNCTION-IDENTIFIER (ISO §8.4.3.1.2 Format 1) OR AN
    /// INLINE METHOD INVOCATION (Format 4), from BOTH the expression spine (<see cref="BindPrimary"/>) and the
    /// operand-wrapper walk (<see cref="BindOperandExprCore"/>) — kb/Work PB1142.
    /// <para>
    /// Both formats reference a TEMPORARY data item whose class is the result's — a function's by §15.2 ("The type
    /// of a function is determined by the class and category of the unique data item that results from the
    /// evaluation of that function"), an invocation's by §8.4.3.4.4 GR1 ("An inline method invocation references
    /// a temporary data item with the same class, category, and content as the temp-identifier") — so §8.8.1.1
    /// ("An arithmetic expression may be an identifier referencing a numeric data item") asks of both the question
    /// <see cref="OperandRef"/> asks of a data item, through the ONE classifier
    /// (<see cref="IntrinsicArgumentRules.IsArithmeticOperandClass"/> over <see cref="IntrinsicBinder.OperandOf"/>),
    /// with the same dialect gate (strict rejects; --permissive decodes the digit characters).
    /// </para>
    /// <para>
    /// ⛔ WHY ONE ENTRY. The screen used to live INLINE in <see cref="BindPrimary"/>'s function arm, so it covered
    /// one arm of two dispatches: the walk's function arm — every arithmetic verb's written operand, e.g.
    /// <c>MULTIPLY FUNCTION UPPER-CASE("12") BY B</c> — bound the call UNSCREENED and digit-decoded an alphanumeric
    /// function where COMPUTE drew COBOLNET0844; and neither dispatch's inline-invocation arm was screened at all
    /// (<c>COMPUTE N = O :: "GETNAME"</c> digit-decoded under STRICT, while the comment on that arm claimed the
    /// data-item screen reached it through the temporary — it did not: the invocation binds to a
    /// <see cref="BoundNumRef"/> and never passes <see cref="OperandRef"/>).
    /// </para>
    /// <para>
    /// ⛔ BOTH ARITHMETIC CONTEXTS, and why that is safe (kb/Work PB172). The window context serves genuinely
    /// arithmetic positions (SET amounts, VARYING FROM/BY, subscripts) AND relation / EVALUATE comparands, where a
    /// SOLE alphanumeric identifier is a legal §8.8.4.2.1 operand (`IF FUNCTION LOWER-CASE(X) = Y`). The boundary
    /// is SOLE-vs-COMPOUND, not statement-vs-statement, so the comparand binders short-circuit the sole forms
    /// before the spine — <c>ConditionBinder.SoleFunctionCall</c> / <c>SoleInlineInvocation</c> in
    /// <c>ComparisonOperandOf</c> and <c>EvaluateBinder.BindValueOperand</c> — and this screen keys on the RULE
    /// (<c>Rules().NumericClassScreen</c>). ⛔ THE VERDICT READS THE CLASS, NOT ResultCategory (PB124 wave 5b):
    /// the storage model folds §15.2 item 6's INDEX functions into category numeric.
    /// </para></summary>
    private BoundExpr BindIdentifierOperand(Antlr4.Runtime.ParserRuleContext id, OperandContext context)
    {
        var bound = id switch
        {
            Core.FunctionCallContext fc => host.Intrinsic.BindIntrinsic(fc),
            Core.InlineMethodInvocationContext or Core.ObjectViewContext or Core.SelfAndSuperContext
                => host.Oo.OoBindOoIdentifier(id),
            _ => throw new InvalidOperationException(
                $"BindIdentifierOperand serves ISO §8.4.3.1.2 Formats 1, 4, 5 and 6 only, not {id.GetType().Name}"),
        };
        return ScreenIdentifierOperand(bound, id, context);
    }

    /// <summary>§8.8.1.1's class screen over an ALREADY-BOUND non-data-name identifier — the half of
    /// <see cref="BindIdentifierOperand"/> the keyword-omitted function form (§8.4.3.2.3 SR2: a repository
    /// function-name written without the word FUNCTION, which the grammar parses as a data reference) shares
    /// from <see cref="RefExpr"/>. Before, that third spelling of a function-identifier bypassed the screen in
    /// EVERY arithmetic position, COMPUTE included (kb/Work PB1142's sibling sweep: `COMPUTE C =
    /// UPPER-CASE("12") + 1` gave 13 under STRICT).</summary>
    internal BoundExpr ScreenIdentifierOperand(BoundExpr bound, Antlr4.Runtime.ParserRuleContext id, OperandContext context)
    {
        if (!context.Rules().NumericClassScreen) return bound;
        var operand = IntrinsicBinder.OperandOf(bound);
        if (IntrinsicArgumentRules.IsArithmeticOperandClass(operand)) return bound;
        string cls = IntrinsicArgumentRules.ClassOf(operand)?.ToString().ToLowerInvariant() ?? "non-numeric";
        string article = cls[0] is 'a' or 'e' or 'i' or 'o' or 'u' ? "an" : "a";
        string what = bound is BoundIntrinsicCall sc
            ? $"FUNCTION {sc.Sig.Name} ({article} {cls} function, ISO §15.2)"
            : id switch
            {
                Core.InlineMethodInvocationContext =>
                    $"the inline method invocation '{id.GetText()}' ({article} {cls} result, ISO §8.4.3.4.4 GR1)",
                Core.ObjectViewContext =>
                    $"the object-view '{DataBinder.WrittenText(id)}' (an object reference, ISO §8.4.3.5.4 GR1)",
                Core.SelfAndSuperContext =>
                    $"'{DataBinder.WrittenText(id)}' (an object reference, ISO §8.4.3.8.3 SR7)",
                _ => $"the function-identifier '{id.GetText()}' ({article} {cls} result, ISO §15.2)",
            };
        bool decodable = IntrinsicArgumentRules.IsDigitDecodable(operand);
        if (ctx.Edition.Permissive && decodable)
        {
            ctx.Edition.Warning("COBOLNET0844", $"{what} is not a numeric operand (ISO §8.8.1.1); accepted "
                + "under --permissive, decoding its digit characters as an unsigned integer");
            return bound;
        }
        ctx.Edition.Error("COBOLNET0844", $"{what} is not a numeric operand: ISO §8.8.1.1 admits only an "
            + "identifier referencing a NUMERIC data item, a numeric literal, or the figurative constant "
            + "ZERO in an arithmetic expression." + PermissiveDecodingHint(decodable));
        return BoundExprError.Refused(ctx.Edition, $"{what} in an arithmetic expression (ISO §8.8.1.1)");
    }

    /// <summary>The closing sentence of a strict §8.8.1.1 refusal: the <c>--permissive</c> digit-decoding extension is
    /// named only for an operand it can actually read (<see cref="IntrinsicArgumentRules.IsDigitDecodable"/>).</summary>
    private static string PermissiveDecodingHint(bool decodable) =>
        decodable ? " --permissive accepts it as a digit-decoding extension" : "";

    /// <summary>Descend an operand-wrapper node to its inner arithmetic expression, or its leaf literal / data
    /// ref. The wrapper chain can nest the expression MORE than one level deep (<c>comparisonOperand →
    /// valueOperand → arithmeticExpression</c>, CobolExpressions.g4), so the walk is BREADTH-FIRST to the
    /// shallowest match — a depth-first leaf grab would collapse a multi-term operand to its first data
    /// reference.
    /// <para>
    /// ⛔ THE CLAUSE THAT MAKES IT BREADTH-FIRST is ISO §8.8.4.7.3 SR1: "Arithmetic-expression-1 shall be any
    /// single numeric data item described with a usage other than a standard floating-point usage, or any form of
    /// arithmetic expression" — so a sign condition's operand is the WHOLE expression (NC250A IF--TEST-55/56),
    /// which a first-data-reference grab would silently truncate. ⚠ This comment used to cite §8.8.4.3, which is
    /// the SIMPLE BOOLEAN CONDITION ("Boolean-expression-1 shall reference only boolean items of length 1") — a
    /// real clause answering a different question, and it sat on the exact line PB171's defect lived on
    /// (CLAUDE.md rule 1's inherited-citation failure mode; repaired with kb/Work PB171).
    /// </para></summary>
    /// <param name="positionRule">See <see cref="NonNumericInNumericContext"/> — the clause closing this
    /// position's operand list, named in the diagnostic when the wrapper holds a non-numeric literal.</param>
    public BoundExpr BindOperandExpr(IParseTree node, string? positionRule = null) =>
        BindOperandExprCore(node, OperandContext.Arithmetic, positionRule);

    /// <summary>The INDEX-NAME-WINDOW twin of <see cref="BindOperandExpr"/>: the same breadth-first wrapper walk,
    /// run under <see cref="OperandContext.ArithmeticIndexNameWindow"/> so an index-name is a legal operand (ISO
    /// §13.18.38.3 r7 — "in the VARYING phrase of a PERFORM statement"; kb/Work R29) and an index DATA ITEM is
    /// not (§13.18.60.3 SR10 has no PERFORM entry; kb/Work PB215). It exists because PERFORM VARYING's FROM/BY
    /// slots are a <c>valueOperand</c> WRAPPER (§14.9.28.2's brace group, kb/Work PB432):
    /// <see cref="BindIndexNameWindowExpr"/> would reach <see cref="BindOperandExprCore"/> through
    /// <see cref="BindExprCore"/>'s default arm but could not thread <paramref name="positionRule"/>, so a
    /// non-numeric literal there cited §8.8.1.1 alone instead of the rule that closes THAT operand list.</summary>
    /// <param name="positionRule">See <see cref="NonNumericInNumericContext"/>.</param>
    public BoundExpr BindIndexNameWindowOperandExpr(IParseTree node, string? positionRule = null) =>
        BindOperandExprCore(node, OperandContext.ArithmeticIndexNameWindow, positionRule);

    /// <inheritdoc cref="BindOperandExpr"/>
    private BoundExpr BindOperandExprCore(IParseTree node, OperandContext context, string? positionRule = null)
    {
        var queue = new Queue<IParseTree>();
        queue.Enqueue(node);
        while (queue.Count > 0)
        {
            var n = queue.Dequeue();
            for (int i = 0; i < n.ChildCount; i++)
            {
                var c = n.GetChild(i);
                if (c is Core.ArithmeticExpressionContext ae) return BindExprCore(ae, context);
                // ⛔ A FUNCTION CALL MUST BE CAUGHT HERE, BEFORE THE WALK CAN DESCEND INTO IT (fix-queue PB45).
                // This walk is breadth-first over the wrapper's subtree looking for something bindable, and a
                // functionCall CONTAINS a dataReference — its argument. Without this arm the walk fell through to
                // the arm below and bound `FUNCTION SQRT(W-Z)` as plain `W-Z`: `ADD FUNCTION SQRT(W-Z) TO W-R`
                // with W-Z = 4 added FOUR instead of TWO. Silent, and it survives "does it compile" entirely —
                // it was caught only by checking the VALUE against the spec-derived answer.
                // The Format-4 twin — an inlineMethodInvocation — is caught here for the SAME reason: it CONTAINS
                // a dataReference (its receiver, and every identifier argument), so the walk would bind
                // `O :: "GET" (W)` as plain `O`. Both take the ONE entry BindPrimary uses, so §8.8.1.1's class
                // screen reaches every arithmetic verb's written operand (kb/Work PB1142: this arm used to bind
                // the call UNSCREENED, digit-decoding an alphanumeric function COMPUTE rejected).
                if (c is Core.FunctionCallContext or Core.InlineMethodInvocationContext)
                    return BindIdentifierOperand((Antlr4.Runtime.ParserRuleContext)c, context);
                // ⛔ THE BARE nonNumericLiteral ARM (kb/Work PB171), BEFORE the LiteralContext arm and for a
                // reason the tree hides: `valueOperand : arithmeticExpression | nonNumericLiteral` names
                // `nonNumericLiteral` DIRECTLY, bypassing the `literal : numericLiteral | nonNumericLiteral`
                // wrapper this walk knew — so a NonNumericLiteralContext is NOT a LiteralContext and matched no
                // arm at all. The queue drained and the fallback below returned zero: `IF "ABC" IS POSITIVE`
                // compiled clean and evaluated `0 > 0`. Routed through the ONE numeric-context literal reading,
                // never a second copy of the dispatch.
                if (c is Core.NonNumericLiteralContext nn
                    && NonNumericInNumericContext(nn, nn.GetText(), positionRule) is { } nnx) return nnx;
                if (c is Core.LiteralContext l) return NumLiteral(l, positionRule);
                if (c is Core.DataReferenceContext d) return RefExpr(d, context);
                queue.Enqueue(c);
            }
        }
        // ⛔ A WRAPPER HOLDING NOTHING BINDABLE FAILS LOUD (kb/Work PB171). This used to `return new
        // BoundNumLiteral("0")`, and its own comment already named the hazard — "a new operand alternative that
        // nobody adds an arm for degrades to `0` rather than failing" — while crediting a drift test that only
        // ever compared the four ARITHMETIC operand rules to each other and never looked at `comparisonOperand`.
        // The green guard is what stopped anyone looking. Every reachable shape now has an arm above, so the
        // next alternative added to `valueOperand` fails at the wrong-stage boundary (COBOLNET_DESIGN §1.4)
        // instead of computing zero; an ALREADY-errored parse reaching here costs nothing, because the compile
        // has already failed. `OperandWalkCoverageTests` enumerates the served rules from the .g4 so the arm is
        // added at BUILD time, not after the wrong answer ships.
        return BoundExprError.Refused(ctx.Edition, $"operand wrapper '{node.GetText()}' with no bindable content");
    }
}
