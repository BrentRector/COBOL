// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Common;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The control-flow verb binder (P7 Step 10l — the audit-extension class mirroring the emitter-side
/// <c>CodeGen/Verbs/ControlFlowEmitter</c>; a RECORDED deviation from the plan sketch's three-class split
/// [Perform/If/ControlFlow] — one class matches the emitter topology and the singular-pattern rule):
/// STOP RUN/literal, GO TO (plain / DEPENDING / the ALTER-subsystem delegations — host edges until 10n),
/// EXIT (paragraph/perform/program/section/method/function — the Oo/Udf/Ec dispatches ride host hooks until
/// 10r/10s), IF, and the PERFORM family (inline/out-of-line/TIMES/UNTIL/VARYING — the one control resolver
/// covering both grammar shapes, the NC106A/NC176A lesson). The condition/expression/blocks spine stays on
/// the host until 10o/10q/10t.</summary>
/// <remarks>The varying-phrase half — the §14.9.28.2 brace-group operand screen and the §14.9.28.3 SR4/SR5/SR6
/// operand rules — lives in <c>ControlFlowBinder.PerformVarying.cs</c>.</remarks>
internal sealed partial class ControlFlowBinder(BinderContext ctx, StatementBinder host)
{
    public BoundStatement BindStop(Core.StopStatementContext stop)
    {
        // STOP RUN … WITH STATUS (§14.9.42) is a COBOL-2002 introduction; the edition gate (StopRunStatus2002)
        // lives in the post-bind VersionConformancePass (Step 14d), reading the PARSE tree (ctx.statusPhrase()).
        // The status VALUE → process-exit-code wiring is decoded here into BoundStop.Status (§14.9.42.4 GR5).
        // §8.8.3.3 GR3: a concatenation expression stands anywhere a literal of its class may — fold a
        // STOP literal-1 concat to the equivalent single literal before decoding (GetText on the whole
        // literal context would glue the operands and mis-decode).
        return stop.literal() is { } slit
            ? new BoundStopLiteral(slit.nonNumericLiteral()?.concatenationExpression() is { } ce
                ? ConcatFolder.Fold(ce, ctx.Edition, ctx.Data.LiteralEnv).Value
                : CobolLiteral.Decode(slit.GetText()))
            : new BoundStop(BindTerminationStatus(stop.statusPhrase()));
    }

    /// <summary>Decode a shared <c>statusPhrase</c> (<c>WITH? (ERROR|NORMAL) (STATUS (dataReference|literal)?)?</c>,
    /// ISO §14.9.42.2 / §14.9.18.2) into a <see cref="TerminationStatus"/>, or null when the phrase is absent. The
    /// ERROR/NORMAL keyword is mandatory when the phrase is present; the STATUS value operand is optional.
    /// Shared by STOP RUN and GOBACK — ONE method, so the two verbs cannot acquire different rules (verified, not
    /// assumed: CallBinder's GOBACK arm is this method's only other caller).
    ///
    /// <para>⛔ THE POSITION IS NOT AN ARITHMETIC EXPRESSION, and binding it as one rejected legal COBOL
    /// (kb/Work PB169). The general format §14.9.42.2 writes the operand <c>{identifier-1 | literal-1}</c>, not
    /// <c>arithmetic-expression-1</c>, so §8.8.1.1 never governed it — and both arms went through
    /// <c>host.Expr.BindExpr</c>. Measured on 9a89fbd1: <c>STOP RUN WITH ERROR STATUS "ABEND"</c> AND
    /// <c>STOP RUN WITH ERROR STATUS WS-CODE</c> with <c>WS-CODE PIC X(3)</c> each drew COBOLNET0844 citing
    /// §8.8.1.1, while THIS METHOD'S OWN DOC COMMENT already said the position takes "an integer literal or a
    /// display/national/integer data item" — the code rejected two of the three shapes its documentation
    /// promised. This is the <c>BindByValueExpr</c> / COBOLNET1628 shape exactly: a rule enforced at a site that
    /// could not know its own context, quoting the wrong clause at the programmer.</para>
    ///
    /// <para><b>The position's OWN rules, now enforced</b> (§14.9.42.3 for STOP, the identical §14.9.18.3 SR6–SR8
    /// for GOBACK), each cite.py-checked:
    /// <list type="bullet">
    ///   <item>SR2/SR6 — "Identifier-1 shall reference an integer data item or a data item with usage display or
    ///         usage national" (§14.9.18.3 SR6 is the same rule word for word, over <b>identifier-2</b> — GOBACK's
    ///         identifier-1 is the RAISING object; cite.py --check fails on the identifier-1 spelling there, so do
    ///         not propagate it). Excluded: a BIT group and a usage-BIT item, an index data item, a pointer, an
    ///         object reference and a non-integer COMP/BINARY item. ADMITTED: a <c>PIC X(3)</c> DISPLAY item, a
    ///         GROUP-USAGE NATIONAL group (§13.18.29.4 GR2 b), an ALPHANUMERIC group (§8.5.2.1 — "An alphanumeric
    ///         group item is treated as though it had a usage of display"; kb/Work PB411), and a
    ///         REFERENCE-MODIFIED display/national operand (§8.4.3.3.4 GR6 preserves usage) — the last three were
    ///         the residual over-rejections PB217 and PB411 closed.</item>
    ///   <item>SR3/SR7 — "If literal-1 is numeric, it shall be an integer". The CONDITIONAL is what makes the
    ///         non-numeric form conforming: a rule that has to say "if it is numeric" presupposes that it may
    ///         not be.</item>
    ///   <item>SR4/SR8 — "Literal-1 shall not be a zero-length literal" (previously unenforced at both verbs).</item>
    /// </list></para>
    ///
    /// <para>GR5 leaves the VALUE constraint implementor-defined and <c>docs/CONFORMANCE.md</c> item 192 already
    /// published this compiler's determination — "the integer value of literal-1 / identifier-1 (truncated toward
    /// zero) becomes the exit code; a non-integer display/national operand is interpreted numerically" — so the
    /// mapping here is not a new decision, it is the published one finally implemented. The renderer already had
    /// it: <c>NumericRenderer.FieldNumCore</c> decodes an alphanumeric/national place through
    /// <c>CobolNum.FromAlphanumeric</c>, and <c>Visit(BoundStringLiteral)</c> does the same for a literal.</para></summary>
    internal TerminationStatus? BindTerminationStatus(Core.StatusPhraseContext? sp)
    {
        if (sp is null) return null;
        return new TerminationStatus(sp.ERROR() is not null, StatusOperand(sp));
    }

    /// <summary>The STATUS phrase's <c>{identifier-1 | literal-1}</c> operand, bound as the position's own operand
    /// and screened by the position's own syntax rules — the <c>CountOperand</c> pattern (PERFORM … TIMES), which
    /// is the model this copies rather than adding a fifth <c>OperandContext</c> member: the slot does not admit
    /// an arithmetic EXPRESSION at all, so routing it through the expression spine would repeat the very category
    /// error PB169 is.</summary>
    private BoundOperand? StatusOperand(Core.StatusPhraseContext sp)
    {
        if (sp.literal() is { } l)
            return ScreenStatusOperand(host.Expr.LiteralOperand(l), l.GetText());   // THE one literal→operand mapping (§8.3.3)
        if (sp.dataReference() is { } d)
        {
            var op = host.Expr.FieldOperand(d);
            // §13.18.38.3 r7 — the STATUS phrase is NOT one of the five contexts that may reference an
            // index-name (a subscript, PERFORM/SEARCH VARYING, SET, a relation condition). The R16 screen for
            // exactly this already existed and simply was not applied at this site.
            if (host.Expr.ScreenIndexNameOperand(op, d.GetText(), "the STOP RUN / GOBACK status operand"))
                return BoundOperandError.Refused(ctx.Edition, $"index-name '{d.GetText()}' in a termination-status phrase");
            return ScreenStatusOperand(op, d.GetText());
        }
        return null;   // WITH ERROR / NORMAL, no STATUS value — GR2/GR3's implementor indication
    }

    /// <summary>⛔ THE POSITION'S SCREEN, KEYED ON THE BOUND SHAPE — never on which parse arm produced it
    /// (kb/Work PB216). The first cut asked SR3/SR4 inside the <c>sp.literal()</c> arm and SR2 inside the
    /// <c>sp.dataReference()</c> arm, which is wrong on BOTH sides of the general format: §13.10.4 GR1 makes a
    /// constant-name's effect "as if literal-1 … were written where constant-name-1 is written", so
    /// <c>78 K VALUE 1.5.</c> + <c>STATUS K</c> arrives on the dataReference arm AS a numeric literal and skipped
    /// SR3 entirely (measured: it compiled clean and exited 1); and §12.3.7.4 GR11 makes a symbolic-character
    /// reference a figurative constant, likewise on the dataReference arm. One screen over the shapes the ONE
    /// §8.3.3 literal→operand mapping and <c>FieldOperand</c> can produce is what keeps the next literal form
    /// from finding a hole — the identifier arm had a completeness structure (<see cref="AdmittedStatusItem"/>)
    /// while the literal arm was two ad-hoc <c>is</c> patterns over a six-shape mapping, and that asymmetry WAS
    /// the defect generator.
    /// <para>⛔ WHAT IS *NOT* SCREENED IS THE POINT. §8.3.3.6.3 SR1 admits a figurative constant "whenever
    /// 'literal' appears in a format", narrowed only (a) where "the literal is restricted to a numeric literal"
    /// — §14.9.42.3 SR3's CONDITIONAL "if literal-1 is numeric" is the proof that it is not — and (b) where a
    /// syntax rule prohibits it; SR2/SR3/SR4 prohibit nothing about class. §8.3.3.6.4 GR3 NOTE 2 names the STOP
    /// statement BY NAME and gives the figurative a length of one character there. So <c>STATUS SPACE</c>,
    /// <c>STATUS ALL "5"</c> and <c>STATUS B"01"</c> are CONFORMING source: the remedy is to RENDER them under
    /// the GR5 mapping docs/CONFORMANCE.md item 192 already publishes, not to add a sixth rejection arm — which
    /// would mint the rejects-legal-source defect PB169 exists to close, one literal form over. Measured before
    /// the fix: each compiled clean and died at run time with NotImplementedCobolFeatureException.</para>
    /// <para>The one word the grammar carries here that is NOT a literal is <c>NULL</c>: §8.3.3.6.2 lists no NULL
    /// format (it is a predefined address / object reference, §8.4.3.10.1) and §8.4.3.10.3 SR1 confines it to an
    /// INITIALIZE/SET sending operand, a prototype argument, and a pointer-or-object-reference relation
    /// condition — the status slot is none of those.</para></summary>
    private BoundOperand ScreenStatusOperand(BoundOperand op, string text)
    {
        switch (op)
        {
            // SR3/SR7 — a NUMERIC literal-1 shall be an integer. (A non-numeric literal-1 is unconstrained here;
            // SR4/SR8 bars only the zero-length one.)
            case BoundNumericLiteral num when !IsIntegerLiteralText(num.Text):
                StatusError($"the numeric status literal '{text}' shall be an integer", "SR3", "SR7");
                break;
            // SR4/SR8 — literal-1 shall not be a zero-length literal (alphanumeric, national or boolean alike).
            case BoundStringLiteral { Value.Length: 0 }:
                StatusError("the status literal shall not be a zero-length literal", "SR4", "SR8");
                break;
            // NULL never reaches here: the phrase is no §8.4.3.10.3 SR1 context, so the ONE literal mapping refused
            // it (COBOLNET2576, kb/Work PB1427) and handed back a BoundOperandError.
            // SR2/SR6 — an integer data item, OR a data item with usage display or usage national.
            case BoundFieldOperand { Place: var place } when !AdmittedStatusItem(place):
                StatusError($"the status operand '{text}' shall reference an integer data item or a data "
                    + "item with usage display or usage national", "SR2", "SR6");
                break;
        }
        return op;
    }

    /// <summary>§14.9.42.3 SR2 / §14.9.18.3 SR6, read as written: an INTEGER data item, or a data item whose
    /// USAGE is DISPLAY or NATIONAL. An index data item, a pointer and an object reference have their own usages
    /// and are excluded by name.
    /// <para>⛔ THE USAGE ALTERNATIVE ASKS <see cref="ItemCategory.UsageOf"/>, THE ONE §8.5.2.1 USAGE READER —
    /// never <see cref="DataItem.OperandPic"/> alone, and never <c>Pic</c> guarded by <c>IsGroup</c> (the
    /// spelling D20's own doc comment forbids by name, and which this screen had before kb/Work PB217).
    /// §13.18.29.3 SR3 implies USAGE NATIONAL for the subject of a GROUP-USAGE NATIONAL entry and §13.18.29.4
    /// GR2 b makes such a group "treated as though it were an elementary data item of usage national … described
    /// with PICTURE N(m)" — so it IS "a data item with usage national" and SR2 admits it; §8.5.2.1 says "An
    /// alphanumeric group item is treated as though it had a usage of display", so an ALPHANUMERIC group is a
    /// data item with usage display and SR2 admits it too. Asking <c>OperandPic</c> instead answered NULL for
    /// that group and the screen concluded "no usage at all", rejecting — with a diagnostic quoting SR6 — the
    /// very shape SR6's second alternative names (kb/Work PB411: measured on both verbs). The reader settles all
    /// group kinds with no hand-list: national admitted, BIT (usage bit) not, alphanumeric admitted, and a
    /// strongly-typed or variable-length group refused because §3.11 excludes it from "alphanumeric group item"
    /// and the standard gives it no usage of its own.</para>
    /// <para>⛔ A REFERENCE-MODIFIED OPERAND IS ADMITTED ON ITS SUBJECT'S USAGE (kb/Work PB217). §8.4.3.3.3 SR5
    /// permits reference modification "anywhere an identifier referencing a data item of class alphanumeric,
    /// boolean, or national is permitted", and §8.4.3.3.4 GR6 gives the unique data item "the same class,
    /// category, and usage as that defined for identifier-1" — the three lettered exceptions rewrite class and
    /// category only, never USAGE. So SR2's SECOND alternative decides a slice, and its FIRST cannot: GR6 c has
    /// already removed category numeric (<see cref="RefModPlace.Category"/>, THE ONE GR6 reader, records that no
    /// ref-mod result is ever category NUMERIC), so a slice of <c>PIC 9(5)</c> DISPLAY is admitted as a
    /// display item and not as an integer one. A slice of a usage-BIT item stays rejected; a slice of an
    /// ALPHANUMERIC group is admitted on that group's §8.5.2.1 display usage, exactly as the whole group is.
    /// The former blanket <c>p is RefModPlace</c> rejected <c>STATUS WS-CODE(1:2)</c> with a diagnostic
    /// quoting the very rule that admits it.</para></summary>
    private static bool AdmittedStatusItem(Place p)
    {
        // ⛔ THE ONE §8.5.2.1 USAGE READER (kb/Work PB411) — null is an item the standard gives no usage, which
        // meets neither alternative; it is NOT the same answer as "has no PICTURE".
        if (ItemCategory.UsageOf(p.Item) is not { } usage) return false;
        if (usage is Usage.Index) return false;   // §8.5.2.1 Table 2 — class index, not a display/national item
        if (IntrinsicArgumentRules.ClassOfPlace(p) is CobolClass.Pointer or CobolClass.Object) return false;
        // §8.4.3.3.4 GR6 — a slice keeps identifier-1's USAGE and loses category numeric (GR6 c), so only SR2's
        // usage alternative can admit it.
        if (p is RefModPlace) return usage is Usage.Display or Usage.National;
        // An INTEGER data item (any usage — BINARY, PACKED, COMP-5 …) is the first alternative. A group has no
        // operand PICTURE, so it can only ever reach the usage alternative below — which is where §8.5.2.1 put it.
        if (p.Item.OperandPic is { Category: PicCategory.Numeric, Scale: 0, IsFloat: false }) return true;
        // …or a data item WITH USAGE DISPLAY OR NATIONAL, whatever its category (this is the alternative that
        // makes `WS-CODE PIC X(3)` legal, and the one the arithmetic funnel could not express).
        return usage is Usage.Display or Usage.National;
    }

    /// <summary>A numeric literal is an INTEGER when it carries no decimal separator and no exponent
    /// (§8.3.3.3.2 / §8.3.3.3 — the literal's written form decides; DECIMAL-POINT IS COMMA is normalized by
    /// <c>ExpressionBinder.CheckLiteral</c> before this, so only the dot form reaches here).</summary>
    private static bool IsIntegerLiteralText(string text) =>
        !text.Contains('.') && !text.Contains('E') && !text.Contains('e');

    private void StatusError(string what, string stopRule, string gobackRule) =>
        ctx.Edition.Error(DiagnosticCatalog.TerminationStatusOperand,
            $"{what} (ISO §14.9.42.3 {stopRule} for STOP RUN; §14.9.18.3 {gobackRule} for GOBACK)");

    /// <summary>CONTINUE [AFTER arithmetic-expression-1 SECONDS] (ISO §14.9.9). Plain CONTINUE is a 1985-continuous
    /// no-op (<see cref="BoundNop"/>). The AFTER … SECONDS timed-pause phrase (COBOL-2023, introduction-gated on the
    /// phrase by the VersionConformancePass) binds to a <see cref="BoundContinueAfter"/>. Whether
    /// EC-CONTINUE-LESS-THAN-ZERO checking is enabled at this statement is captured from the TurnState NOW (a bound
    /// node carries no parse line), so the runtime raises the nonfatal exception (GR1b) only under CHECKING ON.</summary>
    public BoundStatement BindContinue(Core.ContinueStatementContext cont)
    {
        if (cont.arithmeticExpression() is not { } secs) return new BoundNop();   // no-op: plain CONTINUE (§14.9.9)
        bool checkLtz = ctx.EcState.Turn.Enabled("EC-CONTINUE-LESS-THAN-ZERO", null, cont.Start.Line);
        return new BoundContinueAfter(host.Expr.BindExpr(secs), checkLtz);
    }

    /// <summary>Bind a GO TO statement — ONE ARM PER PRINTED GENERAL FORMAT (ISO §14.9.17.2; kb/Work PB412).
    /// <para>⛔ The format is not reconstructed here from optional children any more: <c>goToStatement</c> carries
    /// one grammar alternative per printed format, so <see cref="GoToFormats.Of"/> is TOTAL and the switch below
    /// has no fall-through arm to catch the complement. It used to: the Format-2 test was
    /// <c>dataReference() is not null &amp;&amp; names.Length >= 1</c> and the bare test <c>names.Length == 0</c>,
    /// which left "everything else" — <c>GO TO A B.</c> among it — dropping into the Format-1 arm, where
    /// <c>names[0]</c> was bound and the remaining procedure-names were discarded in silence.</para></summary>
    public BoundStatement BindGoTo(Core.GoToStatementContext g)
    {
        var names = g.procedureName();
        switch (GoToFormats.Of(g))
        {
            case GoToFormat.Depending:              // GO TO p1 p2 … DEPENDING ON identifier-1
                var targets = new List<int>();
                bool resolved = true;
                foreach (var n in names)
                {
                    // A section target transfers to its first paragraph (ISO §14.9.17 GR1). An unresolvable
                    // procedure-name is REPORTED by the ONE operand resolution (kb/Work PB390) — GO TO states no
                    // syntax rule of its own about procedure-name-1, so §8.4.2.1 alone decides. EVERY name in the
                    // series is screened: `GO TO A B C DEPENDING` with two bad names draws two diagnostics, not the
                    // first one only (the CheckCorrespondingGroupOperand discipline — a short-circuit hides the rest).
                    if (ctx.Table.ResolveProcedureOperand(n, "GO TO DEPENDING") is { } proc) targets.Add(proc.Range.Start);
                    else resolved = false;
                }
                // §14.9.17.3 SR1 — identifier-1 is a class-closed position (kb/Work PB210): screened through the
                // ONE operand-class screen, BOTH halves of the rule at once (numeric elementary AND integer). A
                // PIC 9V9 selector used to be truncated by the emitter and silently select procedure-name-2.
                // identifier-1 is any identifier format the grammar's sending slot admits (kb/Work PB1421): a
                // function-identifier's temporary item (§8.4.3.2.4 GR1) is classed by its result by the same screen.
                var (selector, written) =
                    g.functionCall() is { } fc ? (host.Intrinsic.IntrinsicOperand(fc), (Antlr4.Runtime.ParserRuleContext)fc)
                    : g.inlineMethodInvocation() is { } imi ? (host.Oo.OoInlineInvocationOperand(imi), imi)
                    : (host.Expr.FieldOperand(g.dataReference()), g.dataReference());
                bool admitted = OperandClassScreen.Screen(ctx.Edition, OperandPositions.GoToDependingSelector,
                    selector, DataBinder.WrittenText(written));
                return resolved && admitted
                    ? new BoundGoToDepending(selector, targets, ctx.SourceLine(g))
                    : BoundRejected.Reported(ctx.Edition);

            case GoToFormat.Unconditional:          // GO TO procedure-name-1
                return ctx.Table.ResolveProcedureOperand(names[0], "GO TO") is not { } target
                    ? BoundRejected.Reported(ctx.Edition)
                    // alterable when the owning paragraph is an ALTER target, else a plain GO TO
                    : host.Alter.AlterGoTo(g, target.Range.Start);

            default:                                // the 85-only target-less GO TO (ALTER subsystem)
                return host.Alter.AlterBindBareGoTo(g);
        }
    }

    /// <summary>The EXIT statement's own name for a source element kind, for the SR7 diagnostic — the standard's
    /// noun phrases from ISO §14.2.2 SR10, so the programmer is told which of the five elements this is.</summary>
    private static string ElementNoun(SourceElementKind kind) => kind switch
    {
        SourceElementKind.FunctionDefinition => "a function definition",
        SourceElementKind.FunctionPrototype => "a function prototype definition",
        SourceElementKind.MethodDefinition => "a method definition",
        SourceElementKind.ProgramPrototype => "a program prototype definition",
        _ => "a program definition",
    };

    /// <summary>Bind an EXIT statement (ISO §14.9.14), including EVERY placement syntax rule §14.9.14.3 states
    /// for it — each asked of the ONE bind-position probe <c>ctx.Enclosing</c> (kb/Work PB403), never of a
    /// per-format predicate re-derived here:
    /// <list type="bullet">
    ///   <item><b>SR7</b> "An EXIT PROGRAM statement may be specified only in a program procedure division" —
    ///         <c>Enclosing.InProgramProcedureDivision</c>. It used to be the single test <c>host.InMethod</c>,
    ///         so ONE of the four non-program source elements §14.2.2 SR10 names was refused and a FUNCTION
    ///         definition's EXIT PROGRAM was accepted AND handed the program-activation machinery.</item>
    ///   <item><b>SR8</b> "The EXIT PERFORM statement may be specified only in an inline or exception-checking
    ///         PERFORM statement. The CYCLE phrase shall not be specified within an exception-checking PERFORM
    ///         statement." — BOTH sentences, <c>Enclosing.InPerform</c> and
    ///         <c>Enclosing.InExceptionCheckingPerform</c>. Sentence 1 was enforced NOWHERE, and the accepted
    ///         statement did not quietly do nothing: the emitter's F3Region.None arm wrote a bare C# <c>break;</c>
    ///         into the pc dispatcher's <c>switch (__pc)</c> without advancing <c>__pc</c>, so the paragraph ran
    ///         forever. Sentence 2 was enforced by a parse-subtree walk owned by the Format-3 PERFORM binder —
    ///         the same question answered twice, in two places, one of them not at all.</item>
    ///   <item><b>SR9</b> "The EXIT statement with the SECTION phrase may be specified only in a section" —
    ///         <c>ctx.CurrentSection</c>, the bind position's section (kept: it is the same probe's data, and the
    ///         bound node needs the section's end pc anyway).</item>
    ///   <item><b>SR10</b> "… with the PARAGRAPH phrase may be specified only in a paragraph" — ⛔ NOT A CHECK,
    ///         AND THE ABSENCE IS THE IMPLEMENTATION. §14.4.3 defines a paragraph as "a paragraph-name followed
    ///         by a separator period and by zero, one, or more successive sentences OR, IF THE PARAGRAPH-NAME IS
    ///         OMITTED, one or more successive sentences following the procedure division header or a section
    ///         header", so EVERY sentence of a procedure division is in a paragraph and SR10 admits no violating
    ///         program. A screen here would be dead code that could only ever reject legal source.</item>
    /// </list>
    /// <para>⛔ <b>SR1</b> ("The EXIT statement shall appear in a sentence by itself that shall be the only sentence in
    /// the paragraph or in a section without paragraphs", Format 1) is NOT asked here, and neither are GO TO's
    /// §14.9.17.3 SR2 and STOP's §14.9.42.3 SR1: all three are stated over a statement SEQUENCE, which is a fact
    /// of the parse tree and not of the bind cursor, so they are asked once for every statement by the funnel
    /// (<c>PlacementRules.RefusedOutOfSequence</c> over <c>StatementPosition</c>; kb/Work PB397).</para>
    /// SR2 ("shall not be specified in a declarative procedure for which the GLOBAL phrase is specified") and
    /// SR6 (the RAISING LAST placement) are the remaining §14.9.14.3 placement rules; both are stated again for
    /// GOBACK by §14.9.18.3 SR1/SR5 over the SHARED raising binder, so they are one cluster with the GOBACK verb
    /// (kb/Work PB404, PB409, PB410) and land with it — each is now one <c>ctx.Enclosing</c> question
    /// (<c>InGlobalDeclarative</c>, <c>InDeclarative || InPerformWhen</c>).</summary>
    public BoundStatement BindExit(Core.ExitStatementContext e)
    {
        if (e.PARAGRAPH() is not null) return new BoundExitParagraph(ctx.SourceLine(e));   // SR10: no violating program exists
        if (e.PERFORM() is not null)
        {
            var where = ctx.Enclosing;
            // §14.9.14.3 SR8 sentence 1. The rule is what makes §14.9.14.4 GR5 a) meaningful at all: control
            // passes "to an implicit CONTINUE statement immediately following the END-PERFORM phrase that matches
            // the most closely preceding, and as yet unterminated, inline PERFORM statement" — outside every
            // PERFORM there is no such END-PERFORM, so the statement has no meaning to carry.
            if (!where.InPerform)
            {
                return BoundRejected.Report(ctx.Edition, "COBOLNET0827",
                    "EXIT PERFORM may be specified only in an inline or exception-checking PERFORM statement "
                    + "(ISO §14.9.14.3 SR8)");
            }
            // §14.9.14.3 SR8 sentence 2 — an exception-checking PERFORM is not a loop, so there is no cycle to
            // take. Measured against the NEAREST enclosing PERFORM, NOT against "does any ancestor check
            // exceptions": §14.9.14.4 GR5 a)/b) fix the referent of every EXIT PERFORM as "the END-PERFORM phrase
            // that matches the most closely preceding, and as yet unterminated, inline PERFORM statement", and
            // GR4/GR5 partition on the SAME phrase SR8 uses ("specified in an exception-checking PERFORM"). The
            // any-ancestor reading would make GR4 claim an EXIT PERFORM written in an ordinary inline PERFORM
            // nested inside a Format-3 one, contradicting GR5 a) and leaving that inner loop with no way out.
            // So a CYCLE inside an ordinary inline PERFORM nested in a Format-3 PERFORM cycles the inline one
            // and is legal — which is also what the emitter's innermost-wins F3Region tracking already does.
            if (e.CYCLE() is not null && where.InExceptionCheckingPerform)
            {
                return BoundRejected.Report(ctx.Edition, "COBOLNET1604", "EXIT PERFORM CYCLE shall not appear within an exception-checking "
                    + "PERFORM (ISO §14.9.14.3 SR8)");
            }
            return new BoundExitPerform(e.CYCLE() is not null);
        }
        if (e.PROGRAM() is not null)   // §14.9.14 GR2/GR3 — CONTINUE in a non-called program, return-to-caller in a called one (runtime-contextual)
        {
            var element = ctx.Enclosing.SourceElement;
            if (!ctx.Enclosing.InProgramProcedureDivision)   // §14.9.14.3 SR7
            {
                return BoundRejected.Report(ctx.Edition, "COBOLNET0827",
                    $"EXIT PROGRAM may be specified only in a program procedure division, not in "
                    + $"{ElementNoun(element)} (ISO §14.9.14.3 SR7; the five source elements that may "
                    + "carry this procedure division are §14.2.2 SR10's — a function, function prototype or "
                    + "method definition returns via GOBACK)");
            }
            // §14.9.14.3 SR2, through the ONE asker that also serves GOBACK's §14.9.18.3 SR1 (kb/Work PB404).
            // The rule is stated under FORMAT 2, so it is EXIT PROGRAM's; and it is a FLAT prohibition — RESUME's
            // §14.9.33.4 GR1 dynamic-scope CONTINUE arm has no counterpart in §14.9.14.4.
            if (PlacementRules.RefusedInGlobalDeclarative(ctx, EcRaiseSite.Exit("EXIT PROGRAM")))
                return BoundRejected.Reported(ctx.Edition);
            if (e.raisingPhrase() is { } raising)   // Format 2's RAISING tail (§14.9.14.2) — re-raise in the activator
                return host.Ec.EcBindRaising(raising, e.Start.Line, EcRaiseSite.Exit("EXIT PROGRAM")) is { } r
                    ? new BoundExitProgram(r)
                    : new BoundUnsupported("EXIT PROGRAM RAISING identifier (the exception-object form of the RAISING phrase — the OO wave)");
            return new BoundExitProgram();
        }
        if (e.SECTION() is not null)   // §14.9.14 Format 4, GR7 — transfer to the section's end (its return mechanism)
        {
            if (ctx.CurrentSection is not { } sec)   // §14.9.14.3 SR9 — EXIT SECTION may be specified only in a section
            {
                return BoundRejected.Report(ctx.Edition, "COBOLNET0827",
                    "EXIT SECTION may be specified only in a section (ISO §14.9.14.3 SR9)");
            }
            return new BoundExitSection(sec.EndPc, ctx.SourceLine(e));
        }
        if (e.METHOD() is not null) return host.Oo.OoBindExitMethod(e);   // method-return synonym ≤2014; 0902 at 2023 (validator)
        if (e.FUNCTION() is not null) return host.Udf.UdfBindExitFunction(e);   // function-return synonym ≤2014; 0900/0902 window (validator)
        return new BoundNop();   // bare EXIT (§14.9.14.4 GR1); its SR1 placement is asked by the funnel, see above
    }


    public BoundStatement BindIf(Core.IfStatementContext iff)
    {
        var thenBlocks = new List<Core.StatementBlockContext>();
        var elseBlocks = new List<Core.StatementBlockContext>();
        bool seenElse = false;
        foreach (var child in StatementBinder.Children(iff))
        {
            if (child is ITerminalNode t && t.Symbol.Type == CobolLexer.ELSE) seenElse = true;
            else if (child is Core.StatementBlockContext sb) (seenElse ? elseBlocks : thenBlocks).Add(sb);
        }
        return new BoundIf(host.Cond.BindCondition(iff.condition()), host.BindBlocks(thenBlocks), host.BindBlocks(elseBlocks));
    }


    /// <summary>PERFORM's own procedure-name rule, quoted, for the operand diagnostic — ONE sentence with the
    /// operand and rule number substituted, because §14.9.28.3 SR12 and SR13 ARE one sentence written twice
    /// (kb/Work PB390).</summary>
    private static string PerformNameRule(string operand, string rule) =>
        $"\"{operand} shall be the name of either a paragraph or a section in the same source element as that "
        + $"in which the PERFORM statement is specified\" (ISO §14.9.28.3 {rule})";

    public BoundStatement BindPerform(Core.PerformStatementContext p)
    {
        var names = p.procedureName();
        if (names.Length == 0)
        {
            // ⛔ ONE CARDINALITY SCREEN FOR BOTH INLINE FORMATS, ABOVE THE FORMAT-2/FORMAT-3 SPLIT (kb/Work PB431).
            // It is asked here — not inside either arm — because the question the two formats ask is the SAME
            // question with a different bound (Format 2: at most one; Format 3: none), and the Format-3 arm
            // (EcBindExceptionPerform) never asked it at all: that is this project's most reproducible defect
            // shape, a dispatch with two arms and only one ever fixed.
            CheckInlineHeadCardinality(p);
            // Format 3 (exception-checking, §14.9.28.2 Format 3) — any WHEN phrase, or a [WITH] LOCATION head,
            // marks the inline PERFORM as exception-checking. Everything else is a Format-2 inline PERFORM.
            if (PerformFormat.IsFormat3(p))
                return BindExceptionPerform(p);
            // §14.9.28.2 Format 2. imperative-statement-1 binds INSIDE the enclosing-construct frame, so an
            // EXIT PERFORM written in it can see the PERFORM that gives it meaning (§14.9.14.3 SR8 / §14.9.14.4
            // GR5) — and one written OUTSIDE every PERFORM sees no frame and is refused. The control phrase is
            // bound first and outside the frame: it is the PERFORM's own operand, not part of its body.
            CheckUntilExitPlacement(p);
            var control = BindPerformControl(p);
            bool varyingOrTest = PerformFormat.SpecifiesVaryingOrTest(p);
            IReadOnlyList<BoundStatement> body;
            using (ctx.EnterConstruct(EnclosingConstruct.InlinePerform, varyingOrTest))
                body = host.BindBlocks([p.statementBlock()]);
            // §14.9.28.3 SR8's out-of-line "under": every procedure this body performs and returns from is in this
            // PERFORM's range (§14.9.28.4 GR1), so an UNTIL EXIT anywhere in those procedures is under it.
            if (varyingOrTest)
                ctx.UntilExit.AddBarringRanges(ProcedureReach.ReturningTransfersIn(body), ctx.SourceLine(p));
            return new BoundInlinePerform(control, body);
        }

        // Out-of-line: the resolved procedure range — a paragraph, a SECTION (its whole paragraph range, ISO
        // §14.9.28.4 GR4 — first statement of its first paragraph through the last of its last), or the THRU
        // composition (GR4/GR5b — procedure-name-1's start through procedure-name-2's end).
        // ⛔ SR12 AND SR13 ARE TWO RULES WITH ONE SHAPE, AND THEY GO THROUGH THE ONE RESOLUTION (kb/Work PB390).
        // "Procedure-name-1 [-2] shall be the name of either a paragraph or a section in the same source element
        // as that in which the PERFORM statement is specified" (ISO §14.9.28.3 SR12, SR13) — decided here since
        // the first binder, but delivered as a BoundUnsupported, i.e. compiled into the program as a run-time
        // abort blaming WiseOwl COBOL for a gap. Each arm names its OWN rule number; fixing one and not the other is
        // the two-arm defect this project keeps finding.
        CheckUntilExitPlacement(p);   // §14.9.28.3 SR8 — a phrase rule, decided whether or not the names resolve
        if (ctx.Table.ResolveProcedureOperand(names[0], "PERFORM", PerformNameRule("Procedure-name-1", "SR12"),
                ProcedureReferenceKind.Perform) is not { } first)
            return BoundRejected.Reported(ctx.Edition);
        var range = first.Range;
        if ((p.THRU() is not null || p.THROUGH() is not null) && names.Length >= 2)
        {
            if (ctx.Table.ResolveProcedureOperand(names[1], "PERFORM THRU", PerformNameRule("Procedure-name-2", "SR13"),
                    ProcedureReferenceKind.Perform) is not { } thru)
                return BoundRejected.Reported(ctx.Edition);
            // ⛔ SR11 — THE DECLARATIVES CONSTRAINT ON THE COMPOSED RANGE (kb/Work PB433). It is checked HERE,
            // where BOTH ends are resolved and both still carry their owning sections, because the composition
            // below throws the sections away and leaves a pc pair no later pass can ask the question of.
            if (!ctx.Validation.CheckDeclarativesRange(
                    first, thru, names[0].GetChild(0).GetText(), names[1].GetChild(0).GetText(), "PERFORM"))
                return BoundRejected.Reported(ctx.Edition);
            // An INVERTED range (the THRU procedure physically precedes the first, reached by GO TO — GR6
            // "there is no necessary relationship between procedure-name-1 and procedure-name-2"; NIST NC102A
            // PFM-TEST-F1-10) is legal: the dispatcher returns when the exit procedure completes, wherever it is.
            range = range.Through(thru.Range);
        }

        // ⛔ ONE PATH, EMPTY SET INCLUDED (kb/Work PB440). The specified set MAY be empty — procedure-name-1
        // (or the whole THRU composition) may name a section with ZERO paragraphs, which §14.4.2 permits — and
        // GR4 DEFINES that set, so an empty set is a set. The control phrase is the PERFORM's OWN semantics
        // (GR8/GR9/GR10; GR13 a) sets every induction variable BEFORE any transfer of control), and the range only
        // says what the body does; returning a BoundNop here deleted the phrase along with the body, so no
        // condition was ever evaluated and no induction variable was ever set, while the byte-identical INLINE
        // form of the same phrase gave exactly the GR13 e) answer. Emptiness now rides the RANGE
        // (PcRange.IsEmpty) into the emitter, which emits the loop scaffold around an EMPTY body.
        // §14.9.33.4 GR2 b)'s PERFORM: written in the nondeclarative portion, referencing a declarative procedure
        // (SR11 has already put both THRU ends in the one declarative section). A PERFORM written INSIDE a
        // declarative is not it — a RESUME under that one belongs to whatever ran the enclosing declarative.
        bool entersDeclarative = first.IsDeclarative && ctx.Enclosing.Declarative is null;
        if (PerformFormat.SpecifiesVaryingOrTest(p))
            ctx.UntilExit.AddBarringRanges([range], ctx.SourceLine(p));   // §14.9.28.3 SR8 — its range is "under" it
        return new BoundOutOfLinePerform(range, BindPerformControl(p), ctx.SourceLine(p), entersDeclarative);
    }

    /// <summary>⛔ ISO §14.9.28.3 SR8, THE ONE SCREEN — "The UNTIL EXIT phrase shall not be specified in a PERFORM
    /// statement with or under a PERFORM statement with the VARYING phrase or either the TEST BEFORE or TEST AFTER
    /// phrase" (kb/Work PB434). Asked of every Format-1 and Format-2 PERFORM before its control phrase binds; a
    /// Format-3 PERFORM carries no loop-control phrase (COBOLNET2118). Three arms, one code (COBOLNET2954):
    /// <list type="bullet">
    ///   <item><b>with</b> — this statement also writes VARYING or a TEST phrase. The grammar spells EXIT in every
    ///     UNTIL of a PERFORM (<c>performUntilTarget</c>) so <c>WITH TEST BEFORE UNTIL EXIT</c> and
    ///     <c>VARYING … UNTIL EXIT</c> reach this rule instead of a bare COBOL0001;</item>
    ///   <item><b>under, lexically</b> — it is inside an inline PERFORM that writes one
    ///     (<see cref="EnclosingContext.UnderVaryingOrTestPerform"/>);</item>
    ///   <item><b>under, out of line</b> — its paragraph is reached from such a PERFORM's range: recorded here and
    ///     decided by <see cref="UntilExitPlacement.Report"/> once every procedure is bound.</item>
    /// </list>
    /// "Specified" in a TEST phrase means WRITTEN (<see cref="PerformFormat.SpecifiesVaryingOrTest"/>): SR1's
    /// assumed TEST BEFORE would otherwise make SR8 forbid every UNTIL EXIT.</summary>
    private void CheckUntilExitPlacement(Core.PerformStatementContext p)
    {
        if (!PerformFormat.SpecifiesUntilExit(p)) return;
        if (PerformFormat.SpecifiesVaryingOrTest(p))
            ctx.Edition.Error(DiagnosticCatalog.PerformUntilExitPlacement,
                "the UNTIL EXIT phrase is specified in a PERFORM statement with the VARYING phrase or a TEST BEFORE / "
                + "TEST AFTER phrase: \"The UNTIL EXIT phrase shall not be specified in a PERFORM statement with or "
                + "under a PERFORM statement with the VARYING phrase or either the TEST BEFORE or TEST AFTER phrase\" "
                + "(ISO §14.9.28.3 SR8)");
        else if (ctx.Enclosing.UnderVaryingOrTestPerform)
            ctx.Edition.Error(DiagnosticCatalog.PerformUntilExitPlacement,
                "the UNTIL EXIT phrase is specified in a PERFORM statement under an inline PERFORM statement with the "
                + "VARYING phrase or a TEST BEFORE / TEST AFTER phrase: \"The UNTIL EXIT phrase shall not be specified "
                + "in a PERFORM statement with or under a PERFORM statement with the VARYING phrase or either the TEST "
                + "BEFORE or TEST AFTER phrase\" (ISO §14.9.28.3 SR8)");
        else
            ctx.UntilExit.AddSite(ctx.BindCursor, ctx.Edition.Cursor);
    }


    /// <summary>Bind a Format-3 (exception-checking) PERFORM (ISO §14.9.28 Format 3) — delegated to the EC binder,
    /// which owns the WHEN-operand resolution, the GR14 TurnState overlay, and the §14.9.28.3 syntax rules /
    /// cross-statement bans.</summary>
    private BoundStatement BindExceptionPerform(Core.PerformStatementContext p) => host.Ec.EcBindExceptionPerform(p);

    /// <summary>Bind the OPTIONAL control phrase (TIMES / UNTIL / VARYING) of a PERFORM. Per ISO §14.9.28 the phrase
    /// is independent of the THRU range (general format: <c>PERFORM proc-1 [THRU proc-2] [times|until|varying]</c>),
    /// but the grammar exposes it in two shapes: a direct child (<c>PERFORM proc TIMES</c>, alternatives without
    /// THRU) or wrapped in <c>performOptions</c> (the <c>PERFORM proc THRU proc [performOptions]</c> alternative and
    /// the inline <c>performOptions+</c> form). Resolving only the direct child dropped the count/condition on a THRU
    /// range, silently running the range once instead of N times (§14.9.28 GR9) — the NC106A/NC176A defect
    /// (DEVLOG 514). This one resolver handles every shape for both inline and out-of-line PERFORM.</summary>
    private BoundPerformControl BindPerformControl(Core.PerformStatementContext p)
    {
        // The optional control phrase appears in three tree shapes: a direct child (the out-of-line
        // `PERFORM proc TIMES` alternatives), the THRU form's `performOptions?`, or the inline head's
        // `performInlineHead performOptions…` (the Formats-2/3 merge moved the inline options under the head).
        // ⛔ THE `FirstOrDefault()` IS RECOVERY AFTER A REPORTED VIOLATION, NOT A CHOICE (kb/Work PB431). The
        // §14.9.28.2 Format-2 bracket admits ONE phrase, so a well-formed head has exactly one here; a head with
        // more has already been refused by CheckInlineHeadCardinality (COBOLNET2117) before this runs, and
        // binding the first phrase merely keeps the rest of the bind from cascading. It used to be the ONLY
        // treatment — every phrase after the first was silently DELETED from the bound tree, so no later pass
        // could even see it, and `PERFORM 3 TIMES UNTIL X > 100` ran three times with no diagnostic at any --std.
        var opt = p.performOptions() ?? p.performInlineHead()?.performOptions().FirstOrDefault();
        if ((p.performTimes() ?? opt?.performTimes()) is { } t) return new PerformTimes(CountOperand(t));
        if ((p.performUntil() ?? opt?.performUntil()) is { } u)
        {
            // UNTIL EXIT (§14.9.28.4 GR11, 2023): an infinite loop (a condition that never becomes true); escape
            // is the programmer's job (inline: EXIT PERFORM; out-of-line: GOBACK/STOP). Introduction-gated in the
            // pass; §14.9.28.3 SR8's placement was screened by CheckUntilExitPlacement before this runs.
            if (u.performUntilTarget().condition() is not { } condition) return new PerformForever();
            // The UNTIL condition is evaluated per iteration (§14.9.28 GR6/GR13), so a user-function
            // reference inside it activates per evaluation — the drained-suffix wrapper, never the
            // once-per-statement hoist (§8.4.3.2.4 GR1/GR6a; §8.8.4.13 r2).
            var udfMark = host.Udf.Mark;
            var cond = host.Cond.BindCondition(condition);
            return new PerformUntil(host.Udf.UdfAttachPerEvaluation(cond, udfMark), u.AFTER() is not null);
        }
        if ((p.performVarying() ?? opt?.performVarying()) is { } v) return BindVarying(v);
        return new PerformOnce();
    }

    private BoundPerformControl Unsupported(string feature) => new PerformTimes(BoundOperandError.Unbuilt(ctx.Edition, feature));

    /// <summary>The TIMES count (§14.9.28.2 Format 2 — <c>{identifier-1 | integer-1}</c>): an integer literal, a
    /// function-identifier (§8.4.3.2.4 GR1 — the FUNCTION spelling, or the keyword-omitted form under FUNCTION ALL
    /// INTRINSIC, which <see cref="ExpressionBinder.FieldOperand"/> already resolves), or a data reference. §14.9.28.3
    /// SR2 "Identifier-1 shall be an integer" is enforced HERE for every shape through the ONE integer classifier
    /// (<see cref="IntrinsicResultType.IsIntegerOperand"/>): a scale-0 numeric elementary item that is not USAGE
    /// INDEX, or a function whose resolved type is integer (§15.2 type 5). kb/Work PB86: a non-integer item was
    /// accepted and its unscaled digits iterated; a function count ran once.</summary>
    private BoundOperand CountOperand(Core.PerformTimesContext t)
    {
        BoundOperand op =
            t.integerLiteral() is { } lit ? new BoundNumericLiteral(lit.GetText())
            : t.functionCall() is { } fc ? host.Intrinsic.IntrinsicOperand(fc)
            // §14.9.28.2 Format 2's `{identifier-1 | integer-1} TIMES`; kb/Work PB428.
            : t.inlineMethodInvocation() is { } imi ? host.Oo.OoInlineInvocationOperand(imi)
            : t.dataReference() is { } d ? host.Expr.FieldOperand(d)
            : BoundOperandError.Refused(ctx.Edition, "PERFORM … TIMES count shape (ISO §14.9.28.2 Format 2)");
        if (op is not BoundOperandError && !IntrinsicResultType.IsIntegerOperand(op))
            ctx.Edition.Error(DiagnosticCatalog.PerformTimesCountNotInteger,
                $"PERFORM ... TIMES count '{(t.functionCall() ?? (Antlr4.Runtime.ParserRuleContext?)t.dataReference())?.GetText()}' "
                + "shall be an integer (ISO §14.9.28.3 SR2) — an integer data item, an integer literal, or an "
                + "integer-type function-identifier (§15.2 type 5)");
        return op;
    }
}
