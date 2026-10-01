// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;

namespace CobolNet.Frontend.Preprocessor;

using Core = CobolParserCore;

/// <summary>
/// The implicit PUSH ALL / POP ALL of ISO/IEC 1989:2023 §14.9.28.4 GR14 as ordinary directive-stack ops (kb/Work
/// PB1004, PB1066): "An implicit PUSH ALL followed by TURN OFF ALL is assumed at the end of imperative-statement-1.
/// Immediately preceding the END PERFORM phrase, there is an implicit POP ALL …".
///
/// <para>⛔ ONE MECHANISM. The two ops go through the SAME <see cref="DirectiveStateStack"/> as a written
/// <c>&gt;&gt;PUSH ALL</c> / <c>&gt;&gt;POP ALL</c> (§7.3.22.4 GR2 / §7.3.20.4 GR3), so EVERY directive state the
/// stack carries is saved at the end of imperative-statement-1 and restored before END-PERFORM — in ALL THREE places
/// that state lives: the post-COPY line-scoped timelines (TURN, REF-MOD-ZERO-LENGTH, FLAG-02/14 bound options —
/// <see cref="DirectiveResults.WithStackOps"/>); the reference-format normalizer's SOURCE FORMAT
/// (<see cref="ImplicitFormatOps"/>, keyed to the physical lines of the file each op was written in); and the
/// conditional-compilation driver's compilation-variable table and frontend-inline FLAG options
/// (<see cref="ConditionalCompilationProcessor"/>'s implicit-op program, keyed to its directive encounters by
/// <see cref="ConditionalCompilationResult.KeyImplicitOps"/>). Only the parse tree can place the ops, so the front end
/// collects them after the parse and — when a DEFINE, FLAG or SOURCE FORMAT written inside a bracket would change what
/// a later directive or line sees — re-runs the stages that hold the state with them (kb/Work PB1066;
/// <c>Frontend.Parse</c>). The "TURN OFF ALL" half of the same sentence is the binder's
/// <c>TurnState.WithAllDisabledFrom</c>.</para>
///
/// <para>Placement, in the resultant-line frame every directive event uses: the PUSH at the LAST line of
/// imperative-statement-1 (every directive written after it — a directive occupies its own line — is pushed
/// over), the POP at the END-PERFORM line (every construct from there on sees the restored state). Ops are
/// returned in BOUNDARY order (the PUSH after its token, the POP before its), so on a shared line an inner PERFORM's
/// POP precedes an outer one's PUSH or POP — also when the inner END-PERFORM is the token that ends the outer
/// imperative-statement-1.</para>
/// </summary>
public static class ExceptionPerformDirectiveScope
{
    /// <summary>Every exception-checking PERFORM's implicit PUSH ALL / POP ALL in <paramref name="tree"/>, in
    /// source order.
    /// <para><paramref name="tree"/> may be the parser's RECOVERED tree of a text that did not parse (kb/Work PB1066): the
    /// state a POP restores is what misreads the text after it — a handler's <c>&gt;&gt;SOURCE FORMAT</c> left in force
    /// turns END-PERFORM itself into a syntax error — so such a PERFORM reaches here with no END-PERFORM token.
    /// <paramref name="locateEndPerform"/> then names the RESULTANT line the phrase is written on, given the construct
    /// (0 or less: not found, and the PERFORM yields no ops, as it did before). The guess is verified by the caller's
    /// fixed point: ops that do not make the text parse are discarded with the speculative run.</para></summary>
    public static IReadOnlyList<DirectiveStackOp> ImplicitOps(Antlr4.Runtime.Tree.IParseTree tree,
        Func<Core.PerformStatementContext, int>? locateEndPerform = null)
    {
        var collector = new Collector(locateEndPerform);
        collector.Visit(tree);
        // Ordered by KEY, stably (OrderBy), never by an unstable Sort. The key is the BOUNDARY between tokens an op sits
        // on — the boundary b lies between token b − 1 and token b: a PUSH is "at the end of" imperative-statement-1,
        // so AFTER its last token (b = token + 1); a POP is "immediately preceding" END-PERFORM, so BEFORE that token
        // (b = token). That orders an inner construct's POP before the outer PUSH when the inner END-PERFORM is the
        // very token that ends the outer imperative-statement-1 — a token-index key put the outer PUSH first, so the
        // stack paired the inner POP with the outer PUSH and dropped the pair (kb/Work PB1066).
        return [.. collector.Ops.OrderBy(o => o.Key).Select(o => o.Op)];
    }

    private sealed class Collector(Func<Core.PerformStatementContext, int>? locateEndPerform) : CobolParserCoreBaseVisitor<object?>
    {
        public List<(long Key, DirectiveStackOp Op)> Ops { get; } = [];

        public override object? VisitPerformStatement(Core.PerformStatementContext p)
        {
            // The ONE Format-3 discriminator the binder and the edition gate share (PerformFormat.IsFormat3);
            // only the inline arm carries statementBlock + END-PERFORM.
            if (p.procedureName().Length == 0 && p.statementBlock() is { Stop: { } imp1End } && PerformFormat.IsFormat3(p))
            {
                // The two ops are a PAIR — one without the other has nothing to save or restore — so both or neither.
                (long Key, DirectiveStackOp Op)? pop = null;
                if (p.END_PERFORM() is { } end)
                    pop = (end.Symbol.TokenIndex, new DirectiveStackOp(end.Symbol.Line, DirectiveStackKind.Pop, null));
                else if (locateEndPerform is not null && locateEndPerform(p) is var line and > 0)
                    pop = ((p.Stop ?? imp1End).TokenIndex + 1L, new DirectiveStackOp(line, DirectiveStackKind.Pop, null));
                if (pop is { } closing)
                {
                    Ops.Add((imp1End.TokenIndex + 1L, new DirectiveStackOp(imp1End.Line, DirectiveStackKind.Push, null)));
                    Ops.Add(closing);
                }
            }
            return VisitChildren(p);
        }
    }
}
