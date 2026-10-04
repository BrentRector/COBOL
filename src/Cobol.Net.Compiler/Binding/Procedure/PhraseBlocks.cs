// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Binding.Bound;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The ONE conditional-phrase pair extractor (P7 Step 10b — replaces the ~8 per-verb clones the phase doc
/// enumerates). Every ISO two-branch phrase shares the grammar shape
/// <c>ON-X b1 (NOT-ON-X b2)? | NOT-ON-X b1 (ON-X b2)?</c> (AT END §14.9.30/§14.9.34, INVALID KEY §9.1.14,
/// ON SIZE ERROR §14.7.5, ON OVERFLOW §14.9.43/§14.9.48, ON EXCEPTION §14.9.10, AT END-OF-PAGE §14.9.51):
/// blocks[0] is the first written branch, blocks[1] the second when both are present, and a leading NOT token
/// marks the NOT-first alternative. The positional swap below is TOTAL over every legal shape: NOT-only (one
/// block, NOT-led) → (null, b0); a reversed PAIR (two blocks, NOT-led) → (b1, b0); the normal order → (b0, b1).
/// <para>
/// ⚠ SUPERSEDED PREMISE (2026-07-19, DEVLOG 927/929): this doc used to state that three rules — readAtEnd,
/// writeAtEndOfPage, deleteFileOnException — could not produce a NOT-led form, making <see cref="StartsWithNot"/>
/// a provable no-op for them. THAT PREMISE IS DEAD. ISO 5.2.6.4's choice indicators — dropped by our spec
/// transcription and restored from the printed page — permit BOTH phrases in EITHER order on all of these
/// statements, so every such rule now carries the NOT-led arm. Because each call site already passed
/// <see cref="StartsWithNot"/> uniformly instead of relying on that no-op, the greenfield binder absorbed the
/// shape change with ZERO edits — which is exactly why the uniformity was worth paying for. The frozen legacy
/// binders hand-rolled the same split four times and every one had to be repaired.
/// </para>
/// </summary>
internal static class PhraseBlocks
{
    /// <summary>Split a phrase's statement blocks into its (positive, NOT) branch pair; either side is null
    /// when unwritten. <paramref name="bind"/> is the caller's statement-list binder (the collaborator split
    /// keeps binding on the verb binder; this class owns only the shared SHAPE).</summary>
    public static (List<BoundStatement>? On, List<BoundStatement>? NotOn) Split(
        Core.StatementBlockContext[] blocks, bool notFirst,
        Func<Core.StatementBlockContext, List<BoundStatement>> bind)
    {
        List<BoundStatement>? first = blocks.Length >= 1 ? bind(blocks[0]) : null;
        List<BoundStatement>? second = blocks.Length >= 2 ? bind(blocks[1]) : null;
        return notFirst ? (second, first) : (first, second);
    }

    /// <summary>Whether the phrase pair WRITES its positive (ON-X) branch: a lone block is the positive branch unless
    /// the phrase is NOT-led, and a pair always has both. The question <see cref="Split"/> answers with a non-null
    /// <c>On</c>, asked without binding anything — the migration-flag pass's "END-OF-PAGE phrase specified"
    /// (ISO §7.3.15.4 GR4 m, whose §14.9.51.3 SR19 names the AT END-OF-PAGE and NOT AT END-OF-PAGE phrases separately).</summary>
    public static bool HasOnBranch(Core.StatementBlockContext[] blocks, bool notFirst) =>
        blocks.Length >= 2 || (blocks.Length == 1 && !notFirst);

    /// <summary>True when the phrase context begins with a NOT token — the ONE discriminator of the NOT-first
    /// alternative every two-branch phrase rule shares.</summary>
    public static bool StartsWithNot(IParseTree ctx) =>
        ctx.ChildCount > 0 && ctx.GetChild(0) is ITerminalNode t && t.Symbol.Type == CobolLexer.NOT;
}
