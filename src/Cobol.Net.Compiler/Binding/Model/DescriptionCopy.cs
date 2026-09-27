// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace CobolNet.Binding.Model;

/// <summary>What ONE <see cref="DataItem"/> field is to the data-description COPY that the TYPE clause
/// (ISO §13.18.57.4 GR1), the SAME AS clause (§13.18.49.4 GR1/GR2a) and a TYPEDEF template's subordinate
/// reproduction (§13.18.58.4 GR1) all perform.
///
/// <para>⛔ THE SHAPE EXISTS BECAUSE A HAND LIST LOST A CLAUSE. Both copy helpers spelled their own field list
/// in an object initializer, and a field absent from a list is invisible: <c>GroupUsage</c> — the one field
/// that makes an item a bit or national group — was in NEITHER, so <c>01 R TYPE T</c> over a
/// <c>GROUP-USAGE NATIONAL</c> template bound as an ORDINARY alphanumeric group with the wrong class, the
/// wrong category and the wrong LENGTH, silently (kb/Work PB522). A classification that every stored field
/// MUST carry turns the next such field from a silent omission into a compile-time-visible choice, and
/// <c>DescriptionCopyCompletenessDriftTests</c> makes the choice behaviourally true.</para></summary>
[Flags]
public enum DescriptionCopyKind
{
    /// <summary>Carried by NO description copy: the item's identity (<see cref="DataItem.Uid"/>,
    /// <see cref="DataItem.CsName"/>), the structure a copier rebuilds itself
    /// (<see cref="DataItem.Children"/>, <see cref="DataItem.Parent"/>, <see cref="DataItem.Own88s"/>),
    /// a clause NAMED in a GR-1 exclusion list, or a value a POST-BUILD pass computes in the copy's own
    /// scope. The attribute's reason says which.</summary>
    None = 0,

    /// <summary>A data description clause carried by EVERY description copy — it appears in neither GR-1
    /// exclusion list (§13.18.57.4 GR1 excludes only the level-number, the name, alignment, GLOBAL,
    /// SELECT WHEN and TYPEDEF; §13.18.49.4 GR1 only the level-number, the name, CONSTANT RECORD, EXTERNAL,
    /// GLOBAL, REDEFINES and SELECT WHEN).</summary>
    Clause = 1,

    /// <summary>An ALIGNMENT clause — SYNCHRONIZED (§13.18.55) or ALIGNED (§13.18.1). Carried by a SAME AS
    /// copy and by a cloned subordinate, but NOT by a TYPE subject: §13.18.57.4 GR1 alone excludes
    /// "alignment", and §13.18.49.4 GR1 does not.</summary>
    Alignment = 2,

    /// <summary>Reproduced on a cloned SUBORDINATE (§13.18.58.4 GR1 — the subordinate entries ARE the type;
    /// §13.18.49.4 GR2a — "the same names, descriptions, and hierarchy") but excluded from an ENTRY copy onto
    /// a subject: the entry-name itself, OCCURS (§13.18.49.3 SR5 forbids it on data-name-1; §13.16.3 SR12/SR14
    /// make a subject's own OCCURS the array-of-description form), REDEFINES (both GR-1 exclusion lists), the
    /// declaration cursor, and a nested TYPE / SAME AS reference that re-expands per clone.</summary>
    MemberOnly = 4,

    /// <summary>Not a verbatim copy — a copier WRITES it as the PROVENANCE of a copied clause
    /// (<see cref="DataItem.ValueIsCopied"/>, <see cref="DataItem.ExternalFromType"/>). The drift test
    /// therefore neither requires it to equal the source's value nor requires it to stay default.</summary>
    CopyWritten = 8,

    /// <summary>A data description clause carried by every ENTRY copy (TYPE subject, SAME AS subject, reproduced
    /// subordinate — it is in neither GR-1 exclusion list) but NOT onto a COMPILER TEMPORARY
    /// (<see cref="DescriptionCopyScope.CompilerTemp"/>): the VALUE clause in both its spellings, ANY LENGTH, and
    /// BASED (§13.18.5 — a temporary is storage the pre-op creates, never a template awaiting an address).
    /// A temporary is the ISO §8.4.3.2.4 GR1 / §8.4.3.4.4 GR1 / §14.9.25.4 GR1 item whose content is always
    /// STORED by the pre-op that creates it before anything reads it, so an initial value is unobservable — and a
    /// Format-2 VALUE's FROM subscripts would address an OCCURS a temp's root never reproduces; ANY LENGTH is a
    /// parameter shape §13.18.2.3 SR2 admits "only in an elementary level 1 entry in the linkage section", and a
    /// temporary is a level-1 item of the CALLER's working storage.</summary>
    EntryOnly = 16,
}

/// <summary>WHICH description copy <c>DataBinder.CopyEntryDescription</c> is performing — the one axis on which
/// the ISO rules make the copies differ. Every data description copy in the compiler names one of these; none
/// spells its own field list (<c>DescriptionCopyCompletenessDriftTests</c>).</summary>
public enum DescriptionCopyScope
{
    /// <summary>The subject of a TYPE clause (ISO §13.18.57.4 GR1): every clause but alignment.</summary>
    TypeSubject,

    /// <summary>The subject of a SAME AS clause (§13.18.49.4 GR1, which has no alignment exclusion) or a
    /// reproduced subordinate of a TYPE / SAME AS description (§13.18.58.4 GR1, §13.18.49.4 GR2a): every
    /// clause.</summary>
    Entry,

    /// <summary>A compiler TEMPORARY cloned from a model item's description — a user-function result
    /// (§8.4.3.2.4 GR1: "the description, class, and category of the temporary data item is that specified by
    /// the description in the linkage section of the item specified in the RETURNING phrase"), an inline method
    /// invocation's result (§8.4.3.4.4 GR1), a property reference's temp, and §14.9.25.4 GR1's intermediate
    /// result item. The whole description travels — including alignment, which decides a group temp's slack
    /// bytes — except the <see cref="DescriptionCopyKind.EntryOnly"/> clauses.</summary>
    CompilerTemp,
}

/// <summary>⛔ REQUIRED ON EVERY STORED <see cref="DataItem"/> PROPERTY. Declares what the field is to the
/// ONE description copy (<c>DataBinder.CopyEntryDescription</c>, and the <c>DataBinder.CloneItem</c>,
/// <c>DataBinder.CreateCompilerTemp</c> and <c>DataBinder.CloneTempNode</c> that funnel through it) and WHY, so the classification is made where the field is declared rather than
/// inferred from a list somewhere else.
/// <para><c>DescriptionCopyCompletenessDriftTests</c> (Unit) fails when a stored property has no attribute,
/// when a <see cref="DescriptionCopyKind.Clause"/> / <see cref="DescriptionCopyKind.Alignment"/> field is not
/// actually transferred by the copy, and when a <see cref="DescriptionCopyKind.None"/> /
/// <see cref="DescriptionCopyKind.MemberOnly"/> field IS.</para></summary>
/// <param name="Kind">What the field is to the copy.</param>
/// <param name="Reason">The clause and rule that put it in that class — the ISO §/GR, or the pass that owns
/// the fact for a post-build value. Never empty.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class DescriptionCopyAttribute(DescriptionCopyKind Kind, string Reason) : Attribute
{
    /// <summary>What the field is to the description copy.</summary>
    public DescriptionCopyKind Kind { get; } = Kind;

    /// <summary>The clause / rule / owning pass that puts the field in that class.</summary>
    public string Reason { get; } = Reason;
}
