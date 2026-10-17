// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Procedure;

/// <summary>
/// ⛔ <b>WHAT THE RECEIVING OPERANDS A FROZEN NUMERIC VALUE IS MOVED TO CAN HOLD</b> (kb/Work PB370) — the one input
/// that describes the intermediate a MOVE freezes a numeric function-identifier's returned value into
/// (<c>SendingValueTemp.ReturnedValuePicFor</c>). ISO §14.9.25.4 GR1 writes the statement as
/// <c>MOVE a TO temp</c> / <c>MOVE temp TO b</c> "where 'temp' is an intermediate result item provided by the
/// implementor", and this implementation's determination (docs/CONFORMANCE.md DOC-A.1-92) is that each receiver
/// then holds what <c>MOVE a TO b</c> stores, so the item has to carry every digit — and every MAGNITUDE — a
/// receiver of the set takes. The binder does not know the function's carrier family (binary64, SDIDI or scaled
/// integer — decided by <c>IntrinsicRenderer</c>), so every shape below is one whose exactness holds for EVERY
/// family. Four shapes, decided by the receivers:
/// <list type="bullet">
/// <item>Every receiver is a floating-point item (<see cref="PicInfo.IsFloat"/>): the intermediate is a
/// floating-point item (<see cref="Floating"/>). A fixed-point item cannot be that carrier — a binary64 receiver
/// takes values far outside any 38-digit scaled integer (<c>FUNCTION EXP(100)</c>) and all of a binary64's
/// significant digits, which a scale-9 item cut to <c>3.141592653</c> for <c>MOVE FUNCTION PI TO F (I)</c> where
/// <c>MOVE FUNCTION PI TO G</c> stores <c>3.141592653589793</c>. With ONE floating-point usage among the receivers
/// the intermediate takes that usage, so <c>MOVE a TO temp</c> is the conversion <c>MOVE a TO b</c> performs and
/// <c>MOVE temp TO b</c> is GR6 c)'s same-usage transfer, "without change" — exact for every sender family.
/// Receivers of several binary32 usages share binary32 (a binary32 value converts to another binary32 usage
/// exactly); any other mix of usages takes binary64.</item>
/// <item>Every receiver is a floating-point item or a floating-point numeric-edited item (design D21), and at least
/// one is edited: the intermediate is the WIDEST floating-point numeric-edited item the PICTURE clause admits,
/// <c>+9.9(35)E+9999</c> (<see cref="Floating"/>) — 36 significand digits, §13.18.40.3 SR15's maximum, and four
/// exponent digits, §13.18.40.4 GR13 b)'s. An edited receiver takes the sender's value in its decimal form
/// (an SDIDI value exactly, a binary64 one through its shortest round-trip decimal) and TRUNCATES it to its own
/// significand (§14.6.8.4 2), and truncating to 36 digits and then to fewer is truncating to fewer, so every
/// edited receiver holds what its single-receiver MOVE stores — whatever the sender's family, where a binary64
/// intermediate cut <c>MOVE FUNCTION NUMVAL ("1.2345678901234567890123") TO D (I)</c> into
/// <c>PIC +9.9(20)E+99</c> to <c>+1.23456789012345670000E+00</c>. The decimal form is also the value a
/// floating-point item beside them converts from.</item>
/// <item>Every receiver is a fixed-point numeric or numeric-edited item (<see cref="FixedPointOnly"/>): the
/// intermediate is fixed-point at the receivers' widest <see cref="Scale"/> and <see cref="IntegerDigits"/>
/// (<see cref="PicInfo.WidestReceiving"/>) and nothing else — each receiver keeps a sub-range of the intermediate's
/// digit positions, and §14.6.8.2 4)'s "zero fill or truncation on either end" composes, so the returned value's
/// own 21 integer digits (<c>SendingValueTemp.FunctionValuePic</c>) are not added: added, they cut the fraction of
/// <c>MOVE FUNCTION NUMVAL ("1.2345678901234567890123") TO S (I)</c> into <c>PIC S9(3)V9(28)</c> to seventeen
/// digits, within a 38-digit carrier the receiver's 31 digits fit.</item>
/// <item>Otherwise (a set mixing fixed-point receivers with floating-point ones, or holding a character receiver,
/// whose text image is the returned value's own) the intermediate is fixed-point at the larger of the returned
/// value's description and the fixed-point receivers' widest digits. A floating-point item or a floating-point
/// numeric-edited item beside them asks for every fraction digit the 38-digit carrier keeps beside the integer
/// digits (<see cref="AllFractionDigits"/>), so it receives at least seventeen significant digits of a value at or
/// above one — the binary64 round-trip count.</item>
/// </list>
/// ⚠ THE DOCUMENTED LIMIT IS A MIXED SET (DOC-A.1-92), and only a mixed set: no one description is exact for a
/// binary64 receiver and a 31-digit fixed-point receiver of an exact-valued function at once; floating-point usages
/// of different precisions share binary64, which an exact-valued function reaches a binary32 receiver through; and
/// a binary32 item beside an edited one takes a binary64 value through its shortest decimal. A set of one receiver
/// is never mixed, so a single-receiver freeze stores exactly what the un-frozen MOVE does.
/// Every other receiver — alphanumeric, national, boolean, a reference-modified receiver (which denotes an item that
/// is never numeric, §8.4.3.3.4 GR6 c) — contributes no digits.
/// </summary>
/// <param name="Scale">The fraction digits the fixed-point intermediate keeps at least.</param>
/// <param name="IntegerDigits">The integer digits the fixed-point intermediate keeps at least.</param>
/// <param name="FixedPointOnly">Every receiver is a fixed-point numeric or numeric-edited item, so the receivers'
/// widest digits are the fixed-point intermediate's whole description.</param>
/// <param name="Floating">The description of the intermediate when every receiver is a floating-point item or a
/// floating-point numeric-edited item; <see langword="null"/> for a fixed-point intermediate.</param>
internal readonly record struct ReceivingReach(int Scale, int IntegerDigits, bool FixedPointOnly = false, PicInfo? Floating = null)
{
    /// <summary>The fraction digits a floating-point receiver beside fixed-point ones asks of the fixed-point
    /// intermediate: more than the carrier holds, so the intermediate's own cap
    /// (<c>SendingValueTemp.ReturnedValuePicFor</c> — the 38-digit <c>Int128</c> less the integer digits) decides.</summary>
    internal const int AllFractionDigits = int.MaxValue;

    /// <summary>The widest floating-point numeric-edited PICTURE: §13.18.40.3 SR15's 36 significand digits and
    /// §13.18.40.4 GR13 b)'s four exponent digits, the point after the first digit.</summary>
    private const string WidestFloatEditedPicture = "+9.9(35)E+9999";

    /// <summary>The reach of the receivers whose descriptions are <paramref name="pics"/> (a null PICTURE — an index,
    /// an object — contributes nothing; see the type's summary for the rule). <paramref name="edition"/> analyzes the
    /// floating-point edited intermediate's PICTURE through the one analyzer.</summary>
    internal static ReceivingReach Of(IReadOnlyList<PicInfo?> pics, EditionContext edition)
    {
        if (pics.Count > 0 && pics.All(p => p is { IsFloat: true }))
        {
            var first = pics[0]!;
            bool oneUsage = pics.All(p => p!.Usage == first.Usage);
            bool allSingle = pics.All(p => p!.IsSingle);
            return new ReceivingReach(0, 0, Floating: PicInfo.FloatItem(oneUsage || allSingle ? first.Usage : Usage.FloatLong));
        }
        if (pics.Count > 0 && pics.All(IsFloatingPoint))
            return new ReceivingReach(0, 0, Floating: PictureAnalyzer.Analyze(WidestFloatEditedPicture, Usage.Display,
                edition, "the intermediate a MOVE freezes a function's value into"));
        var fixedPoint = pics.Where(IsFixedPoint);
        var (scale, integerDigits) = PicInfo.WidestReceiving(fixedPoint);
        return new ReceivingReach(pics.Any(IsFloatingPoint) ? AllFractionDigits : scale, integerDigits,
            FixedPointOnly: pics.Count > 0 && pics.All(IsFixedPoint));
    }

    /// <summary>A floating-point item (<see cref="PicInfo.IsFloat"/>) or a floating-point numeric-edited item
    /// (<see cref="PicInfo.IsFloatEdited"/>, design D21): a receiver with no fixed fraction scale, which takes the
    /// sender's value in its own form rather than at a scale.</summary>
    private static bool IsFloatingPoint(PicInfo? pic) => pic is { IsFloat: true } or { IsFloatEdited: true };

    /// <summary>A fixed-point numeric or numeric-edited item: a receiver with a scale and a digit count.</summary>
    private static bool IsFixedPoint(PicInfo? pic) =>
        pic is { Category: PicCategory.Numeric or PicCategory.NumericEdited } && !IsFloatingPoint(pic);
}
