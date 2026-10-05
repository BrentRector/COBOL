// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;                // DiagnosticCursorAt (the ParserRuleContext At overload)
using CobolNet.Editions;               // IDiagnosticSink / EditionDiagnostic / EditionSeverity
using CobolNet.Editions.Diagnostics;   // DiagnosticCatalog
using CobolNet.Frontend.Expressions;   // ArithmeticFormationRules — the SHARED rule
using CobolNet.Frontend.Generated;     // CobolParserCore

namespace CobolNet.Validation;

/// <summary>
/// The EXPRESSION FORMATION pass — ISO §8.8.1.2 Table 3 and §8.8.2 Table 4, the two tables that state which
/// ordered pairs of adjacent symbols an expression may contain — and the formation ORDER of an expression's
/// operand, the identifier (§8.4.2.3.2: qualifiers, then subscripts; §8.4.3.1.2 Format 3: then the reference
/// modifier; kb/Work PB1455). A SIBLING to <see cref="VersionConformancePass"/> and <c>FlagConformancePass</c>, run
/// right after them in <c>BinderDriver</c>.
///
/// <para><b>Why a separate pass.</b> This is an orthogonal axis to both existing passes, on the
/// <c>FlagConformancePass</c> precedent ("a SEPARATE pass, not a bolt-on, because flagging is an orthogonal axis
/// to edition gating"). It is not EDITION gating: Tables 3 and 4 are formation rules with no <c>introducedIn</c>
/// — the (unary, unary) pair is invalid in 1985, 2002, 2014 and 2023 alike — so folding it into
/// <see cref="VersionConformancePass"/> would silently widen that pass's stated charter, under which "the two arms
/// are disjoint: a Check for any one construct fires from EXACTLY one arm" describes construct EDITION checks. Nor
/// is it directive-state flagging, and unlike a flag it is an ERROR. Giving non-edition syntax-rule conformance
/// its own home is also what makes the NEXT such rule automatic instead of adding a fourth place to remember.</para>
///
/// <para><b>Why it is not dialect-gated.</b> The two-axes model gates LENIENCIES, and <c>--permissive</c> softens
/// exactly one verdict — <c>ConstructAvailability.Removed</c> → Warning (<c>EditionSeverityPolicy</c>), the
/// migration mode for constructs an edition REMOVED. An invalid symbol pair was never legal at any edition, so it
/// is not a removed construct and there is no permissive arm to write. Measured against
/// <c>EditionSeverityPolicy.For</c> before choosing this shape (kb/Work PB158); the contrary assumption would have
/// produced a dialect flag nothing could ever set.</para>
///
/// <para>The RULE itself lives in the frontend (<see cref="ArithmeticFormationRules"/>) because the compile-time
/// expression evaluator needs it during compiler-directive processing, before any compiler pass exists. This pass
/// is only the compiler's INVOCATION of it — ONCE per parse tree, never once per binding site.
/// <c>ExpressionBinder</c> has some eight public entry points and a call at each would be a hand-maintained list
/// where a single traversal belongs. Riding <see cref="CursorFollowingVisitor"/> means the diagnostic cursor
/// follows the walk, so the position comes from the same mechanism every other pass uses.</para>
/// </summary>
internal sealed class ExpressionFormationPass(IDiagnosticSink sink) : CursorFollowingVisitor(sink)
{
    /// <summary>Screen the group's raw parse tree for §8.8.1.2 Table 3 / §8.8.2 Table 4 invalid symbol pairs.</summary>
    internal static void Run(CobolParserCore.CompilationUnitContext tree, IDiagnosticSink sink) =>
        new ExpressionFormationPass(sink).VisitPositioned(tree);

    /// <summary>§8.8.1.2 Table 3, row "Unary + or −" × column "Unary + or −" = '—'.</summary>
    public override object? VisitUnaryExpression(CobolParserCore.UnaryExpressionContext ctx)
    {
        if (ArithmeticFormationRules.StackedUnarySign(ctx) is { } sign)
        {
            using var _ = Sink.At(sign.Line, sign.Column + 1);
            Report(ArithmeticFormationRules.StackedUnaryMessage);
        }
        return base.VisitChildren(ctx);
    }

    /// <summary>§8.8.2 Table 4, row "B-NOT" × column "B-NOT" = '—'. The grammar comment above
    /// <c>booleanExpression</c> used to assert that "the tiers enforce the formation rules 1–3 + Table 4 adjacency
    /// STRUCTURALLY"; that was true of every cell but this one, and a green-looking claim held the gap open.</summary>
    public override object? VisitBooleanFactor(CobolParserCore.BooleanFactorContext ctx)
    {
        if (ArithmeticFormationRules.StackedNot(ctx) is { } not)
        {
            using var _ = Sink.At(not.Line, not.Column + 1);
            Report(ArithmeticFormationRules.StackedNotMessage);
        }
        return base.VisitChildren(ctx);
    }

    /// <summary>§8.8.2 Table 4, row "B-SHIFT-L … B-SHIFT-RC": only "Identifier or literal" is permissible next —
    /// rule 5's integer operand (kb/Work PB1370 / PB1413).</summary>
    public override object? VisitBooleanShiftSuffix(CobolParserCore.BooleanShiftSuffixContext ctx)
    {
        if (ArithmeticFormationRules.ShiftCountNotSoleOperand(ctx) is { } count)
        {
            using var _ = Sink.At(count.Line, count.Column + 1);
            Report(ArithmeticFormationRules.ShiftCountMessage(ctx));
        }
        return base.VisitChildren(ctx);
    }

    /// <summary>The IDENTIFIER's formation order (kb/Work PB1455, PB1426): every qualifier precedes the subscripts —
    /// §8.4.2.3.2 Format 1 `qualified-data-name-1 [ ( subscript … ) ]` (Format 2 the condition-name twin) — and a
    /// reference modifier follows the whole identifier (§8.4.3.1.2 Format 3 `identifier-1 reference-modifier-1`). The
    /// grammar's <c>dataReferenceSuffix*</c> loop and <c>qualification</c>'s own <c>(subscriptPart | refModPart)*</c>
    /// tail admit them in any order, and every binder that reads a reference flattens the suffixes, so
    /// `E (1) OF T` used to resolve as if written `E OF T (1)` — silently, at every edition. Refused HERE, once per
    /// parse tree, for the reason this pass exists: a check at each consumer of <c>dataReference</c> would be a
    /// hand-maintained list of some thirty sites. No edition prints the interleaving, so there is no permissive arm.
    /// Nested references (a subscript's own identifier, a reference modifier's expression) are reached by the walk.</summary>
    public override object? VisitDataReference(CobolParserCore.DataReferenceContext ctx)
    {
        if (QualifierAfterSuffix(ctx) is { } q)
        {
            using var _ = Sink.At(q);
            ReportSuffixBeforeQualifier($"{q.GetChild(0).GetText()} {q.GetChild(1).GetText()}",
                ctx.cobolWord()?.GetText());
        }
        return base.VisitChildren(ctx);
    }

    /// <summary>The same order inside a SUBSCRIPT-mode capture, where a subscript's or reference modifier's own
    /// identifier is a flat token run rather than a <c>dataReference</c> (`X (E (1) OF T)`): a nested parenthesized
    /// group followed by OF / IN is a subscript written before a qualifier. No other SUBSCRIPT-mode shape puts a
    /// qualifier connective after a closing parenthesis.</summary>
    public override object? VisitSubscriptOrRefMod(CobolParserCore.SubscriptOrRefModContext ctx)
    {
        ScreenCapturedGroup(ctx.subToken());
        return base.VisitChildren(ctx);
    }

    /// <summary>A nested group's own content (`X (Y (E (1) OF T))`).</summary>
    public override object? VisitSubToken(CobolParserCore.SubTokenContext ctx)
    {
        if (ctx.SUB_LPAREN() is not null) ScreenCapturedGroup(ctx.subToken());
        return base.VisitChildren(ctx);
    }

    private void ScreenCapturedGroup(CobolParserCore.SubTokenContext[] run)
    {
        for (int i = 0; i < run.Length; i++)
        {
            if (run[i].SUB_LPAREN() is null) continue;
            int next = i + 1;
            while (next < run.Length && run[next].SUB_WS() is not null) next++;
            if (next < run.Length && (run[next].SUB_OF() ?? run[next].SUB_IN()) is { } connective)
            {
                int word = next + 1;
                while (word < run.Length && run[word].SUB_WS() is not null) word++;
                string qualifier = word < run.Length && run[word].SUB_IDENTIFIER() is { } w
                    ? $"{connective.GetText()} {w.GetText()}" : connective.GetText();
                using var _ = Sink.At(connective.Symbol.Line, connective.Symbol.Column + 1);
                ReportSuffixBeforeQualifier(qualifier, HeadWordBefore(run, i));
            }
        }
    }

    /// <summary>The word that heads the reference whose subscript group starts at <paramref name="group"/>, for the
    /// message only (null when the group follows no word — then the message names no item).</summary>
    private static string? HeadWordBefore(CobolParserCore.SubTokenContext[] run, int group)
    {
        for (int i = group - 1; i >= 0; i--)
        {
            if (run[i].SUB_WS() is not null) continue;
            return run[i].SUB_IDENTIFIER()?.GetText();
        }
        return null;
    }

    private void ReportSuffixBeforeQualifier(string qualifier, string? head) => Sink.Report(new EditionDiagnostic(
        DiagnosticCatalog.SuffixBeforeQualifier.Code, EditionSeverity.Error,
        DiagnosticCatalog.SuffixBeforeQualifier.Id,
        $"'{qualifier}' follows a subscript or reference modifier{(head is null ? "" : $" of '{head}'")}; the "
        + "subscripts follow the WHOLE qualified name (ISO §8.4.2.3.2) and a reference modifier the whole "
        + "identifier (§8.4.3.1.2 Format 3) — write the qualifiers first",
        "an identifier", DiagnosticCatalog.SuffixBeforeQualifier.IsoSection));

    /// <summary>The first qualifier written after a subscript or reference modifier of the same reference, or null.
    /// A qualification's OWN suffix tail counts: in `E OF T (1) OF G` the `(1)` hangs off `OF T`. A non-word property
    /// object (`P (2) OF SELF`, §8.4.3.1.2 Format 7) is an `OF` too: what precedes it is the property-name, which takes
    /// neither a subscript nor a reference modifier there (kb/Work PB1425).</summary>
    private static Antlr4.Runtime.ParserRuleContext? QualifierAfterSuffix(CobolParserCore.DataReferenceContext ctx)
    {
        bool suffixSeen = false;
        foreach (var s in ctx.dataReferenceSuffix())
        {
            if (s.propertyObject() is { } po) { if (suffixSeen) return po; continue; }
            if (s.qualification() is not { } q) { suffixSeen = true; continue; }
            if (suffixSeen) return q;
            suffixSeen = q.subscriptPart().Length > 0 || q.refModPart().Length > 0;
        }
        return null;
    }

    private void Report(string message) => Sink.Report(new EditionDiagnostic(
        DiagnosticCatalog.ExpressionFormationPair.Code, EditionSeverity.Error,
        "expression-formation-pair", message, "an expression",
        DiagnosticCatalog.ExpressionFormationPair.IsoSection));
}
