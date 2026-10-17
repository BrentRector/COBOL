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
/// receiver of the set takes. Two shapes, decided by the receivers:
/// <list type="bullet">
/// <item>Every receiver is a floating-point item or a floating-point numeric-edited item (design D21 — no fixed
/// fraction scale, the sender's value is edited in its own form): the intermediate is itself floating-point
/// (<see cref="Float"/>). A fixed-point item cannot be that carrier — a binary64 receiver takes values far outside
/// any 38-digit scaled integer (<c>FUNCTION EXP(100)</c>) and all of a binary64's significant digits, which a
/// scale-9 item cut to <c>3.141592653</c> for <c>MOVE FUNCTION PI TO F (I)</c> where <c>MOVE FUNCTION PI TO G</c>
/// stores <c>3.141592653589793</c>. With ONE floating-point usage among the receivers the intermediate takes that
/// usage, so <c>MOVE a TO temp</c> is the conversion <c>MOVE a TO b</c> performs and <c>MOVE temp TO b</c> is GR6
/// c)'s same-usage transfer, "without change" — exact for every sender family. Receivers of several binary32 usages
/// share binary32 (a binary32 value converts to another binary32 usage exactly); any other mix, and any
/// floating-point edited receiver, takes binary64 — the float family's own returned value (DOC-A.1-92), so for it
/// every receiver holds what its single-receiver MOVE stores.</item>
/// <item>Otherwise the intermediate is fixed-point, at the widest <see cref="Scale"/> and
/// <see cref="IntegerDigits"/> of the fixed-point numeric and numeric-edited receivers
/// (<see cref="PicInfo.WidestReceiving"/>). A floating-point item or a floating-point numeric-edited item beside
/// them asks for every fraction digit the 38-digit carrier keeps beside the integer digits
/// (<see cref="AllFractionDigits"/>), so it receives at least seventeen significant digits
/// of a value at or above one — the binary64 round-trip count. That residual is this implementation's documented
/// limit for a MIXED set: no one description is exact for a binary64 receiver and a 31-digit fixed-point receiver
/// of an exact-valued function at once.</item>
/// </list>
/// Every other receiver — alphanumeric, national, boolean, a reference-modified receiver (which denotes an item that
/// is never numeric, §8.4.3.3.4 GR6 c) — contributes nothing.
/// </summary>
/// <param name="Scale">The fraction digits the fixed-point intermediate keeps at least.</param>
/// <param name="IntegerDigits">The integer digits the fixed-point intermediate keeps at least.</param>
/// <param name="Float">The floating-point description of the intermediate when every receiver is floating-point;
/// <see langword="null"/> for a fixed-point intermediate.</param>
internal readonly record struct ReceivingReach(int Scale, int IntegerDigits, PicInfo? Float = null)
{
    /// <summary>The fraction digits a floating-point receiver beside fixed-point ones asks of the fixed-point
    /// intermediate: more than the carrier holds, so the intermediate's own cap
    /// (<c>SendingValueTemp.ReturnedValuePicFor</c> — the 38-digit <c>Int128</c> less the integer digits) decides.</summary>
    internal const int AllFractionDigits = int.MaxValue;

    /// <summary>The reach of the receivers whose descriptions are <paramref name="pics"/> (a null PICTURE — an index,
    /// an object — contributes nothing; see the type's summary for the rule).</summary>
    internal static ReceivingReach Of(IReadOnlyList<PicInfo?> pics)
    {
        if (pics.Count > 0 && pics.All(IsFloatingPoint))
        {
            var first = pics[0]!;
            bool oneUsage = pics.All(p => p!.IsFloat && p.Usage == first.Usage);
            bool allSingle = pics.All(p => p!.IsFloat && p.IsSingle);
            return new ReceivingReach(0, 0, PicInfo.FloatItem(oneUsage || allSingle ? first.Usage : Usage.FloatLong));
        }
        var fixedPoint = pics.Where(p => p is { Category: PicCategory.Numeric or PicCategory.NumericEdited } && !IsFloatingPoint(p));
        var (scale, integerDigits) = PicInfo.WidestReceiving(fixedPoint);
        return new ReceivingReach(pics.Any(IsFloatingPoint) ? AllFractionDigits : scale, integerDigits);
    }

    /// <summary>A floating-point item (<see cref="PicInfo.IsFloat"/>) or a floating-point numeric-edited item
    /// (<see cref="PicInfo.IsFloatEdited"/>, design D21): a receiver with no fixed fraction scale, which takes the
    /// sender's value in its own form rather than at a scale.</summary>
    private static bool IsFloatingPoint(PicInfo? pic) => pic is { IsFloat: true } or { IsFloatEdited: true };
}
