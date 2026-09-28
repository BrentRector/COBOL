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
/// ordered pairs of adjacent symbols an expression may contain. A SIBLING to <see cref="VersionConformancePass"/>
/// and <c>FlagConformancePass</c>, run right after them in <c>BinderDriver</c>.
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

    private void Report(string message) => Sink.Report(new EditionDiagnostic(
        DiagnosticCatalog.ExpressionFormationPair.Code, EditionSeverity.Error,
        "expression-formation-pair", message, "an expression",
        DiagnosticCatalog.ExpressionFormationPair.IsoSection));
}
