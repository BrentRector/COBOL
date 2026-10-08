// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Runtime;
namespace CobolNet.CodeGen;

/// <summary>
/// ⛔ THE ELEMENTARY CHARACTER RECEIVING STORE — the ONE place the MOVE rules' store of a character string into an
/// elementary alphanumeric or national receiving operand is decided, for every verb that transfers "according to
/// the rules for the MOVE statement" (kb/Work PB871).
/// <para>Two arms, and the second is why this class exists. A fixed-length receiver takes the string
/// left-justified, space-filled or truncated on the right to its width (a JUSTIFIED one right-justified —
/// §14.9.25.4 GR6c). A DYNAMIC-LENGTH receiver takes it WHOLE: §14.9.25.4 GR8 routes every MOVE into one through
/// §8.5.1.10.4 — "If a dynamic-length elementary item is a receiving operand and is not reference-modified, the
/// new value becomes the content of the item. The new length of the dynamic-length elementary item is determined
/// by the length of new content" and "If the maximum length is reached, the value is truncated on the right as
/// necessary." A dynamic-length item's PICTURE is ONE symbol (§13.18.19.3 SR1), so a store that fits the string to
/// the PICTURE's width writes exactly one character — which is what UNSTRING and ACCEPT did, because each carried
/// its own private copy of the fixed-length arm and only <see cref="MoveEmitter"/> knew the dynamic one.</para>
/// <para>A REFERENCE-MODIFIED dynamic-length item is §8.5.1.10.4's other case — "treated as a fixed-length data
/// item whose length is the dynamic-length elementary item's current length" — and never reaches here: every
/// caller routes a <c>RefModPlace</c> through <c>PlaceRenderer.Write</c>'s splice first.</para>
/// <para><c>ReceivingStoreDriftTests</c> fails if an emitter under <c>CodeGen/Verbs</c> builds either store
/// itself, so the next MOVE-rules verb inherits the dynamic arm instead of re-deriving the fixed one.</para>
/// </summary>
internal static class ReceivingStore
{
    /// <summary>The store expression for <paramref name="sendingChars"/> (a C# string expression holding the
    /// sending operand's characters) into the elementary alphanumeric / national <paramref name="target"/>.
    /// <paramref name="width"/> is the fixed-length receiver's width expression (its PICTURE length, or an
    /// ANY LENGTH receiver's runtime length); a dynamic-length receiver has no width and ignores it.</summary>
    public static string Characters(DataItem target, string sendingChars, string width) =>
        target.IsDynamicLength
            ? RuntimeApi.DynStore(sendingChars, target.DynMaxSize.ToString())
            : RuntimeApi.StrStoreAligned(sendingChars, width, target.Justified);

    /// <summary>⚖ DETERMINATION D-DL2 (docs/CONFORMANCE.md §3; kb/Work PB871) — the SIZE of a dynamic-length
    /// RECEIVING operand, wherever a statement's rules are written over "the number of character positions in"
    /// the receiver rather than over a MOVE (STRING §14.9.43.4 GR6/GR8, UNSTRING §14.9.48.4 GR11b, ACCEPT format 1
    /// §14.9.1.4 GR3/GR4), is its MAXIMUM size (§8.5.1.10.1). §8.5.1.10.4 fixes a dynamic-length item's size only
    /// as a SENDING operand or a reference-modified one (its current length); as a receiver §8.5.1.10.1 says the
    /// number of character positions "may vary during program execution", and the current length would make an
    /// empty item unwritable by STRING — every character an overflow (measured before this fix).</summary>
    public static int DynamicReceivingSize(DataItem target) => target.DynMaxSize;

    /// <summary>The width of an ANY LENGTH receiving operand — its CARRIER's current length, because ISO §13.18.2.4
    /// GR1 b) makes n "the length of the corresponding argument or returning item of the activating runtime
    /// element", never the one symbol its PICTURE spells. The ONE spelling, read by the
    /// MOVE store (<c>MoveEmitter</c>) and by <see cref="ExaminationSize"/>, so the size a statement examines and
    /// the size it stores cannot disagree about the same receiver (kb/Work PB979).</summary>
    public static string AnyLengthWidth(Place target) => $"{PlaceRenderer.Read(target)}.Length";

    /// <summary>⛔ THE ELEMENTARY STORAGE-AREA STORE — the receiving inverse of
    /// <see cref="Emit.OperandText.AsStorageImage"/>, arm for arm (kb/Work PB1556). Every transfer that deposits
    /// an item's STORAGE bytes into an elementary receiving area without converting the representation asks it:
    /// a GROUP MOVE into an elementary receiver (ISO §14.9.25.4 GR4 — "there is no conversion of data from one
    /// form of internal representation to another") and a READ / RETURN that makes the record available in an
    /// ELEMENTARY record area (§14.9.30.4 GR13 c); §14.9.34.4 GR3). <paramref name="area"/> is the receiving
    /// area's bytes, already fitted by the caller's own rule (a MOVE's alphanumeric fill, a READ's GR15 record
    /// fill); <paramref name="positions"/> is the receiver's position count in its own units. The receiver's
    /// representation is DECODED FROM the bytes: a NATIONAL item's UTF-16BE pairs through the ONE inverse
    /// <c>CobolBits.NatReadWindow</c> (D-N1), a USAGE BIT item's packed bits through <c>CobolBits.Unpack</c>, and
    /// a NATIVE numeric carrier (zoned, radix-2, BCD, IEEE — any byte form, including the national-form numeric
    /// of §13.18.60.3 SR12, whose decoded characters are its zoned digit run) through
    /// <see cref="Emit.NumericRenderer.CarrierOfImage"/>, the ONE crossing decode. Every string-backed shape —
    /// alphanumeric [edited], numeric-edited, display boolean, a StoreAsImage numeric, a Tier-B view or a
    /// numeric image place (whose Write IS the decode) — stores the characters as they are.
    /// <para>It used to be written twice and right neither time: the MOVE arm decoded a BINARY / PACKED receiver
    /// with the ZONED parser (<c>StoreDisplay</c>, so the bytes 00 07 moved into <c>PIC 9(4) COMP</c> read as 0)
    /// and a float receiver did not compile (CS1503); the READ arm handled only the national and character
    /// shapes, so an elementary numeric record did not compile at all (CS0029).</para></summary>
    public static string StorageArea(Place target, string area, string positions)
    {
        var item = target.Item;
        string chars = StorageCharacters(item, area, positions);
        bool nativeNumeric = item.Pic is { Category: PicCategory.Numeric } && !item.StoreAsImage
            && target is not RedefViewPlace and not NumericImagePlace;
        return PlaceRenderer.Write(target, nativeNumeric ? Emit.NumericRenderer.CarrierOfImage(chars, item) : chars);
    }

    /// <summary>The receiver's position count in its OWN units for <see cref="StorageArea"/> — national character
    /// positions (<see cref="NationalWindow.PositionsOf"/>), boolean BIT positions (the PICTURE length), else its
    /// character positions; an ANY LENGTH receiver's comes from its carrier at run time (§13.18.2.4 GR1 b).</summary>
    public static string StoragePositions(Place target) =>
        target.Item.IsAnyLength ? AnyLengthWidth(target) : $"{StoragePositions(target.Item)}";

    /// <summary>The position count of a FIXED-length item in its own units — <see cref="StoragePositions(Place)"/> for an
    /// item that is not ANY LENGTH, whose count its description alone fixes.</summary>
    public static int StoragePositions(DataItem item) =>
        item.Pic is { Usage: Usage.Bit } bitPic ? bitPic.Length : NationalWindow.PositionsOf(item) ?? item.ImageWidth;

    /// <summary>The CHARACTERS of an elementary item whose storage holds the bytes <paramref name="area"/> — the decode
    /// half of <see cref="StorageArea"/> for every carrier that is not a native numeric cell: a NATIONAL item's UTF-16BE
    /// pairs through <c>CobolBits.NatReadWindow</c> (D-N1), a USAGE BIT item's packed bits through
    /// <c>CobolBits.Unpack</c>, every other string-backed item the bytes as they are. Asked by <see cref="StorageArea"/>
    /// and by <c>MoveEmitter.RecordValue</c>, whose receiving record exists only as a value (kb/Work PB2587), so a group
    /// move decodes its receiver one way whether the receiver is a place or a record.</summary>
    public static string StorageCharacters(DataItem item, string area, string positions) => item.Pic switch
    {
        { Usage: Usage.National } => RuntimeApi.NatReadWindow(area, "0", positions),
        { Usage: Usage.Bit } => RuntimeApi.BitsUnpack(area, positions),
        _ => area,
    };

    /// <summary>The SIZE of a receiving operand in CHARACTER POSITIONS at execution — a C# int expression, because an
    /// ANY LENGTH receiver's size is its carrier's (ISO §13.18.2.4 GR1 b), "n is the length of the corresponding
    /// argument or returning item of the activating runtime element") and so is not known to the binder. It is the
    /// ONE answer to "how big is the receiver" for every statement whose rules are written over the receiver's size
    /// rather than over a MOVE — ACCEPT format 1 (§14.9.1.4 GR3/GR4: "the same size as the receiving data item"),
    /// and UNSTRING's examined size (<see cref="ExaminationSize"/>, which is this less a separate sign's position).
    /// ⛔ ACCEPT used to size its device window from the DECLARED width, so an ANY LENGTH receiver — whose PICTURE
    /// is one symbol (§13.18.2.3 SR1) — took ONE character of the record whatever argument it was bound to
    /// (kb/Work PB1013), the same defect UNSTRING carried until PB979.
    /// <list type="bullet">
    /// <item>a reference-modified receiver — §8.4.3.3.4 GR6's unique data item, the evaluated slice's length;</item>
    /// <item>a group — its character positions (a bit / national group's as-if positions, D20/PB79);</item>
    /// <item>a dynamic-length item — <see cref="DynamicReceivingSize"/> (DETERMINATION D-DL2);</item>
    /// <item>an ANY LENGTH item — <see cref="AnyLengthWidth"/>;</item>
    /// <item>otherwise the elementary item's text positions, <see cref="DataItem.DisplayTextWidthOf"/> (a numeric
    ///   item's digits plus a SEPARATE sign's position).</item>
    /// </list></summary>
    public static string CharacterPositions(Place target) => target switch
    {
        RefModPlace => $"{PlaceRenderer.Read(target)}.Length",
        _ when target.Item.IsGroup => $"{target.Item.AsIfPic?.Length ?? target.Item.ImageWidth}",
        _ when target.Item.IsDynamicLength => $"{DynamicReceivingSize(target.Item)}",
        _ when target.Item.IsAnyLength => AnyLengthWidth(target),
        _ => $"{(target.Item.Pic is { } pic ? DataItem.DisplayTextWidthOf(pic) : 0)}",
    };

    /// <summary>ISO §14.9.48.4 GR11 b) — UNSTRING without a governing delimiter: "the number of characters examined
    /// is equal to the size of the current receiving area. However, if the sign of the receiving item is defined
    /// as occupying a separate character position, the number of characters examined is one less than the size of
    /// the current receiving area. Size is defined as number of character positions." A C# int expression, because
    /// the size is the receiver's AT EXECUTION (kb/Work PB979 — it was a binder integer: one character for an ANY
    /// LENGTH receiver, and a reference-modified one was staged as not implemented):
    /// <list type="bullet">
    /// <item>a numeric item (never reference-modified, dynamic-length or ANY LENGTH here — §13.18.2.3 SR1 and
    ///   §8.5.1.10.1 admit neither for a numeric item) — its DIGIT positions: a SEPARATE sign is GR11 b)'s "one
    ///   less", an over-punched one occupies no position (SR4 bars the symbol P);</item>
    /// <item>otherwise the receiver's <see cref="CharacterPositions"/> — the ONE size reader.</item>
    /// </list></summary>
    public static string ExaminationSize(Place target) => target switch
    {
        not RefModPlace when target.Item is { IsGroup: false, Pic: { Category: PicCategory.Numeric } pic } => $"{pic.Digits}",
        _ => CharacterPositions(target),
    };
}
