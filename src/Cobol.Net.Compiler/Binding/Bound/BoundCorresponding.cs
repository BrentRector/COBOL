// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

using CobolNet.Binding.Model;

namespace CobolNet.Binding.Bound;

// The CORRESPONDING bound nodes (P7 Step 10e: the binder half moved to
// Binding/Procedure/Verbs/CorrespondingBinder.cs; these types STAY here — consumed by the emitter,
// BoundStores, UsageCollectionPass, the EC categorizer, and the source-generated visitor).

/// <summary>The verb a CORRESPONDING statement expands into per-pair implied statements
/// (ISO §14.9.25.2 MOVE Format 2 / §14.9.2.2 ADD Format 3 / §14.9.44.2 SUBTRACT Format 3).</summary>
public enum CorrVerb { Move, Add, Subtract }

/// <summary>One hoisted group-operand anchor, emitted ONCE before the first implied statement — §14.7.6: all item
/// identification for the pairs (including any subscript on the group operands) is done at the START of the
/// statement, never per implied statement. Exactly one field is set: <paramref name="RefGroup"/> is a member-path
/// group anchored by a <c>ref var</c> local (its table subscripts evaluate exactly once — the backend renders the
/// group place), or <paramref name="ViewOffset"/> is a Tier-B REDEFINES view group's computed window offset (a typed
/// position) pinned by a <c>long</c> local.</summary>
public sealed record CorrespondingHoist(string Local, Place? RefGroup, Position? ViewOffset);

/// <summary>One corresponding pair (§14.7.6): the resolved sending and receiving <see cref="Place"/>s of an
/// implied per-pair statement. Both are anchored on the statement's hoisted group locals where applicable.</summary>
public sealed record CorrespondingPair(Place Source, Place Target)
{
    /// <summary>MOVE CORRESPONDING only: the pair's implied MOVE (§14.9.25.4 GR11 — "the same as if the user had
    /// referred to each pair of corresponding identifiers in separate MOVE statements"), BOUND through
    /// <c>MoveBinder.BindMoveOf</c> so it carries the storage facts and classification the written MOVE of the same
    /// two items does (kb/Work PB880 — it used to be built by the emitter). Null for ADD / SUBTRACT, whose implied
    /// statements are arithmetic. A child statement, so the generated <c>StatementChildren</c> walk reaches it.</summary>
    public BoundMove? Move { get; init; }
}

/// <summary>A MOVE/ADD/SUBTRACT CORRESPONDING statement, expanded at BIND time into its corresponding pairs in D1
/// declaration order (§14.7.6 — "the order in which the elements in the group data item immediately following
/// CORRESPONDING are specified"). <paramref name="Rounding"/> is the statement's ONE rounded-phrase mode, applied
/// to EVERY pair store (§14.9.2.2/§14.9.44.2 — a single rounded-phrase after the receiving group); MOVE carries
/// <see cref="CobolRounding.Truncation"/>. <paramref name="SizeError"/> is STATEMENT-level (§14.7.6): one latching
/// flag across all checked pair stores, ONE phrase dispatch after every pair completes, the NOT branch suppressed
/// when any pair erred — never a per-pair phrase.</summary>
public sealed record BoundCorresponding(
    CorrVerb Verb,
    IReadOnlyList<CorrespondingHoist> Hoists,
    IReadOnlyList<CorrespondingPair> Pairs,
    CobolRounding Rounding,
    SizeErrorPhrase? SizeError) : BoundStatement, IArithmeticStatement;
