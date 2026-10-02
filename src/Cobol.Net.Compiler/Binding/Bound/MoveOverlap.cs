// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Bound;

/// <summary>What a MOVE does to its RECEIVER before it reads a STATICALLY overlapping sender (kb/Work PB1907,
/// docs/CONFORMANCE.md §3 D-OVL2).</summary>
public enum OverlapPrefillKind
{
    /// <summary>Nothing — the ordinary store: the sender is read in full, then the receiver is written (the
    /// snapshot every other MOVE, overlapping or not, performs).</summary>
    None,
    /// <summary>A JUSTIFIED RIGHT receiver's leading <see cref="OverlapPrefill.Positions"/> positions are
    /// space-filled before the sender is read (the padding of §14.9.25.4 GR6's right-justified alignment, written
    /// first).</summary>
    LeadingSpaces,
    /// <summary>A numeric DISPLAY receiver is set to zero before the sender is read.</summary>
    ZeroDigits,
}

/// <summary>The receiver pre-fill of one overlapping MOVE store: its kind and, for
/// <see cref="OverlapPrefillKind.LeadingSpaces"/>, how many leading positions.</summary>
public readonly record struct OverlapPrefill(OverlapPrefillKind Kind, int Positions)
{
    /// <summary>The ordinary store.</summary>
    public static OverlapPrefill None => default;
}

/// <summary>⛔ <b>ISO §14.6.10 1) — "operands … not described by the same data description entry … overlap: the
/// result is undefined" (Annex A.2 item 36; §4.4 makes a run unit that allows it a conforming one) — WHAT
/// WISEOWL COBOL DOES, WRITTEN ONCE</b> (kb/Work PB1907; docs/CONFORMANCE.md §3 D-OVL1/D-OVL2).
/// <para>The standard defines no result, so the choice is the implementor's and CLAUDE.md rule 1 settles it: where
/// GnuCOBOL measurably differs from the plain snapshot (the sender is read in full before the receiver is
/// written), follow GnuCOBOL. GnuCOBOL 3.2's <c>libcob</c> differs in exactly two corners, both because the library
/// prepares the RECEIVER before it reads the SENDER:</para>
/// <list type="bullet">
///   <item><b>JUSTIFIED RIGHT receiver, shorter sender</b> (<c>cob_move_alphanum_to_alphanum</c> and
///     <c>cob_move_display_to_alphanum</c>): the <c>size2 - size1</c> leading positions are space-filled, THEN the
///     sender is moved — so a sender overlapping those positions reads spaces.</item>
///   <item><b>Numeric DISPLAY receiver</b> (<c>store_common_region</c>, <c>cob_move_alphanum_to_display</c>): the
///     whole receiver is set to zero, THEN the digits are taken from the sender — so the overlapped digits are
///     zeros.</item>
/// </list>
/// <para>GnuCOBOL's compiler (<c>cb_build_move_field</c>) copies with <c>memcpy</c>/<c>memmove</c> — a snapshot —
/// when the two numeric descriptions are identical, so this rule asks the same question: identical descriptions
/// keep the snapshot.</para>
/// <para>⛔ It applies ONLY where the overlap is a COMPILE-TIME FACT (<see cref="StorageExtent"/>), so the
/// ordinary MOVE — which cannot overlap — carries <see cref="OverlapPrefill.None"/> and emits exactly what it always
/// did. Where the overlap is not provable (a run-time reference-modification bound, an OCCURS element) the
/// snapshot stands. ISO §14.6.10 2) makes operands described by the SAME entry behave as if disjoint, but Annex A.2
/// item 36 b) — "When one or more of the operands is reference-modified" — makes a reference-modified operand's
/// overlap undefined whatever its entry, so a move from a slice of the receiver itself is overlap like any other
/// (GnuCOBOL measured: <c>MOVE JR(1:2) TO JR</c>, JR PIC X(4) JUSTIFIED RIGHT, leaves four spaces). An
/// un-sliced operand moved to itself has equal descriptions and no pad, so neither corner can fire.</para></summary>
public static class MoveOverlap
{
    /// <summary>The receiver pre-fill for storing <paramref name="sender"/> into <paramref name="target"/> as a
    /// store of <paramref name="kind"/> — <see cref="OverlapPrefill.None"/> unless every condition holds.</summary>
    public static OverlapPrefill Classify(BoundOperand sender, Place target, MoveKind kind)
    {
        if (kind is not (MoveKind.Convert or MoveKind.GroupToElementary)
            || sender is not BoundFieldOperand { Place: var from }
            || target is not (MemberPlace or RedefViewPlace { Coding: null })
            || !target.Item.IsElementary) return OverlapPrefill.None;

        if (StorageExtent.Of(target) is not { } to || StorageExtent.Of(from) is not { } fromExtent
            || !fromExtent.Overlaps(to)) return OverlapPrefill.None;

        // The sender's CHARACTER positions must be its bytes (no national or bit leaf inside a group sender), so
        // the positions this rule counts are the bytes the extent measured.
        if (!IsCharacterPositioned(from, fromExtent)) return OverlapPrefill.None;

        var pic = target.Item.Pic!;
        if (pic.Usage is not Usage.Display || target.Item.IsDynamicLength || target.Item.IsAnyLength)
            return OverlapPrefill.None;

        // JUSTIFIED RIGHT receiver — alphanumeric or alphabetic, a character-image sender of fewer positions.
        if (pic.Category is PicCategory.Alphanumeric && !pic.IsCharacterEdited && target.Item.Justified)
        {
            int pad = to.Length - fromExtent.Length;
            return pad > 0 && IsCharacterSender(from)
                ? new OverlapPrefill(OverlapPrefillKind.LeadingSpaces, pad)
                : OverlapPrefill.None;
        }

        // Numeric DISPLAY receiver — an elementary (Convert) move from a differently described numeric DISPLAY
        // item or from an alphanumeric item. A GROUP sender is a group move (GR4): no conversion, no pre-fill.
        if (kind is MoveKind.Convert && pic.Category is PicCategory.Numeric && !pic.IsFloat
            && (IsCharacterSender(from) || IsOtherNumericDisplay(from, pic)))
            return new OverlapPrefill(OverlapPrefillKind.ZeroDigits, to.Length);

        return OverlapPrefill.None;
    }

    private static bool IsCharacterPositioned(Place p, StorageExtent e) =>
        p is RefModPlace r ? IsCharacterPositioned(r.Inner, StorageExtent.Of(r.Inner) ?? e)
                           : p.Item.ImageWidth == e.Length;

    /// <summary>An alphanumeric-family sender: a reference-modified slice (always alphanumeric, §8.4.3.3.4 GR6), an
    /// unedited alphanumeric or alphabetic DISPLAY item, or a group.</summary>
    private static bool IsCharacterSender(Place from) =>
        from is RefModPlace { Inner.Item: var inner } r
            ? r.Category is PicCategory.Alphanumeric && inner.OperandPic is null or { Usage: Usage.Display }
            : from.Item.IsGroup && !from.Item.IsAsIfElementary
              || from.Item.Pic is { Category: PicCategory.Alphanumeric, Usage: Usage.Display, IsCharacterEdited: false };

    /// <summary>An unedited numeric DISPLAY sender whose description (usage, digits, scale, sign and sign form)
    /// differs from the receiver's — GnuCOBOL's compiler copies identical descriptions byte for byte.</summary>
    private static bool IsOtherNumericDisplay(Place from, PicInfo receiver) =>
        from.DenotedItem is not null   // a whole item, not a slice (a slice is alphanumeric, §8.4.3.3.4 GR6)
        && from.Item.Pic is { Category: PicCategory.Numeric, Usage: Usage.Display } s
        && !(s.Length == receiver.Length && s.Digits == receiver.Digits && s.Scale == receiver.Scale
             && s.Signed == receiver.Signed && s.SignKind == receiver.SignKind);
}
