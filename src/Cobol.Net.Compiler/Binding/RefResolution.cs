// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;

namespace CobolNet.Binding;

/// <summary>What the reference resolver answered for one written data reference — exactly one of three things
/// (kb/Work PB1030).</summary>
public enum RefOutcome
{
    /// <summary>The reference denotes storage: <see cref="RefResolution.Place"/> is set.</summary>
    Place,
    /// <summary>The SOURCE is wrong and the resolver has REPORTED the rule it breaks — an undefined name
    /// (§8.4.2.1), a subscript on a non-table item (§8.4.2.3.3 SR2), something in a subscript position that is not
    /// a subscript (§8.4.2.3.2), a reference to a declaration that was itself refused, …. A caller binds a refusal
    /// node; the statement funnel's refusal ledger fails the compile (COBOLNET2362) if the claim was false.</summary>
    Reported,
    /// <summary>A LEGAL reference in a shape WiseOwl COBOL has not built. The resolver has already put it on the
    /// unbuilt-operand ledger, so the statement funnel announces it (COBOLNET1756) even if the caller dropped it;
    /// <see cref="RefResolution.Shape"/> names which shape, and <see cref="DeferredShapes"/> names who owns
    /// building it.</summary>
    Deferred,
}

/// <summary>⛔ THE CENSUS OF REFERENCE SHAPES THE RESOLVER DEFERS (kb/Work PB1030). Every
/// <see cref="RefOutcome.Deferred"/> answer names one member, so the list of legal reference shapes WiseOwl COBOL has
/// not built is THIS enum and nowhere else; <see cref="DeferredShapes.Describe"/> gives each its text and its
/// owner. A new deferral is a new member — <c>RefResolutionDriftTests</c> fails a member with no description.</summary>
public enum DeferredShape
{
    /// <summary>LINAGE-COUNTER / LINE-COUNTER / PAGE-COUNTER (§8.4.3.14 / §8.4.3.15) reached the resolver at a
    /// site that does not route the special register to its runtime source.</summary>
    UnroutedSpecialRegister,
    /// <summary>A RENAMES … THROUGH span (§13.18.45.4 GR2) with a leaf that has no character image — a numeric
    /// leaf that is neither usage display nor usage national.</summary>
    RenamesNonCharacterLeaf,
    /// <summary>A whole (unsubscripted) OCCURS DYNAMIC table (data-model D9) outside the contexts that take one.</summary>
    DynamicWholeTable,
    /// <summary>A string-canonical REDEFINES class whose backing is not reachable from the reference — its parent
    /// struct is itself within an OCCURS DYNAMIC table, whose elements are reached through the receiving / sending
    /// accessors a REDEFINES view place does not carry (a class within a FIXED or OCCURS DEPENDING table is reached
    /// through the subscripted parent path; kb/Work PB1279).</summary>
    NestedClassBacking,
    /// <summary>An item the access-path builder has no path for (a whole-table reference outside a
    /// table-taking context, or an OCCURS level the path cannot address).</summary>
    UnbuiltAccessPath,
    /// <summary>Reference modification of a numeric item whose substrate has no character image.</summary>
    NumericRefModSubstrate,
    /// <summary>An expression subscript or reference-modifier bound in a context with no statement to evaluate it
    /// in (a data-division resolver has no segment materializer).</summary>
    SegmentWithoutStatement,
    /// <summary>A subscript or reference-modifier expression whose own operand is a deferred shape.</summary>
    DeferredSegmentOperand,
}

/// <summary>The text and the owner of each <see cref="DeferredShape"/> — ONE table, read by the ledger note, the
/// refusal nodes and the drift test.</summary>
public static class DeferredShapes
{
    /// <summary>The run-time guard text for <paramref name="shape"/>, naming its owner.</summary>
    public static string Describe(DeferredShape shape) => shape switch
    {
        DeferredShape.UnroutedSpecialRegister =>
            "a special register in a position that does not yet read it (ISO §8.4.3.14 / §8.4.3.15)",
        DeferredShape.RenamesNonCharacterLeaf =>
            "a RENAMES THROUGH span over a leaf with no character image (ISO §13.18.45)",
        DeferredShape.DynamicWholeTable =>
            "a whole OCCURS DYNAMIC table in this position (data-model D9)",
        DeferredShape.NestedClassBacking =>
            "a REDEFINES view whose backing lies within an OCCURS DYNAMIC table",
        DeferredShape.UnbuiltAccessPath =>
            "a table reference with no access path in this position",
        DeferredShape.NumericRefModSubstrate =>
            "reference modification of a numeric item with no character image",
        DeferredShape.SegmentWithoutStatement =>
            "an expression subscript outside a statement",
        DeferredShape.DeferredSegmentOperand =>
            "a subscript whose operand is not yet implemented",
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, "DeferredShape without a description (kb/Work PB1030)"),
    };
}

/// <summary>⛔ THE REFERENCE RESOLVER'S ANSWER, CLOSED (kb/Work PB1030). The resolver used to answer
/// <c>Place?</c>, and a null meant one of three incompatible things — the name identifies nothing (reported), the
/// source is otherwise wrong (sometimes reported), or the shape is legal and not built (never reported) — so every
/// caller guessed, and they guessed differently: one site reported its own "unresolvable" error on top of the
/// resolver's COBOLNET1639, another bound a run-time <c>NotImplemented</c> for an ILLEGAL subscript
/// (<c>DISPLAY E("A")</c> compiled with a warning and aborted the run unit), and the statement funnel told the
/// cases apart only by whether the statement happened to draw an error. The answer is now one of
/// <see cref="RefOutcome"/>'s three, a caller's refusal node is chosen FROM it (<see cref="Refusal"/>,
/// <see cref="OperandError"/>, <see cref="ExprError"/>, <see cref="BoolError"/>), and a silent null is
/// unrepresentable: a Reported answer is checked by the refusal ledger, a Deferred one is on the unbuilt
/// ledger before the caller sees it.</summary>
public sealed class RefResolution
{
    /// <summary>Which of the three answers this is.</summary>
    public RefOutcome Outcome { get; }

    /// <summary>The storage the reference denotes — non-null exactly when <see cref="Outcome"/> is
    /// <see cref="RefOutcome.Place"/>.</summary>
    public Place? Place { get; }

    /// <summary>The deferred shape — meaningful only when <see cref="Outcome"/> is <see cref="RefOutcome.Deferred"/>.</summary>
    public DeferredShape Shape { get; }

    /// <summary>The reference as written, for the refusal nodes' text.</summary>
    public string Written { get; }

    private RefResolution(RefOutcome outcome, Place? place, DeferredShape shape, string written)
    {
        Outcome = outcome; Place = place; Shape = shape; Written = written;
    }

    internal static RefResolution Resolved(Place place, string written) => new(RefOutcome.Place, place, default, written);

    internal static RefResolution Refused(string written) => new(RefOutcome.Reported, null, default, written);

    internal static RefResolution Deferred(DeferredShape shape, string written) => new(RefOutcome.Deferred, null, shape, written);

    /// <summary>The unbuilt-ledger / run-time guard text of a <see cref="RefOutcome.Deferred"/> answer.</summary>
    public string Feature => $"reference '{Written}' — {DeferredShapes.Describe(Shape)}";

    private void RequireNoPlace()
    {
        if (Outcome == RefOutcome.Place)
            throw new InvalidOperationException($"reference '{Written}' resolved; it has no refusal (kb/Work PB1030)");
    }

    /// <summary>The place, or null with a diagnostic GUARANTEED — for a position whose caller has no node that can
    /// carry a deferral (a helper answering "place or already reported", a pointer or object-reference operand
    /// screened for its category next). A <see cref="RefOutcome.Deferred"/> shape is reported there as
    /// recognized-not-implemented (COBOLNET0899, an error: the staged-loud posture the receiving chokepoint
    /// takes), so the caller's null always means "reported" and its own category diagnostic is never issued for
    /// a reference that did not resolve.</summary>
    public Place? PlaceOrReported(EditionContext edition)
    {
        if (Outcome == RefOutcome.Deferred)
            edition.Error(Editions.Diagnostics.DiagnosticCatalog.ReferenceShapeNotImplemented,
                $"{Feature}: WiseOwl COBOL does not yet implement this reference shape in this position (COBOLNET_DESIGN §1.4)");
        return Place;
    }

    /// <summary>The STATEMENT-level node for a reference that did not resolve:a callee-reported
    /// <see cref="BoundRejected"/> for <see cref="RefOutcome.Reported"/>, a <see cref="BoundUnsupported"/> for
    /// <see cref="RefOutcome.Deferred"/>.</summary>
    public BoundStatement Refusal(EditionContext edition)
    {
        RequireNoPlace();
        return Outcome == RefOutcome.Reported ? BoundRejected.Reported(edition) : new BoundUnsupported(Feature);
    }

    /// <summary>The OPERAND-level node for a reference that did not resolve.</summary>
    public BoundOperandError OperandError(EditionContext edition)
    {
        RequireNoPlace();
        return Outcome == RefOutcome.Reported
            ? BoundOperandError.Refused(edition, $"reference '{Written}'")
            : BoundOperandError.Carry(Feature, unbuilt: true);   // already on the unbuilt ledger (the resolver put it there)
    }

    /// <summary>The EXPRESSION-level node for a reference that did not resolve.</summary>
    public BoundExprError ExprError(EditionContext edition)
    {
        RequireNoPlace();
        return Outcome == RefOutcome.Reported
            ? BoundExprError.Refused(edition, $"reference '{Written}'")
            : BoundExprError.Carry(Feature, unbuilt: true);
    }

    /// <summary>The CONDITION-level node for a reference that did not resolve.</summary>
    public BoundBoolError BoolError(EditionContext edition)
    {
        RequireNoPlace();
        return Outcome == RefOutcome.Reported
            ? BoundBoolError.Refused(edition, $"reference '{Written}'")
            : BoundBoolError.Carry(Feature, unbuilt: true);
    }
}
