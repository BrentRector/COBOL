// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>
/// ⛔ THE ONE ISO §15.50.4 r1/r2/r3 LENGTH OF A FIXED ITEM — the value <c>FUNCTION LENGTH</c> folds to and the value
/// §13.10.4 GR6 gives a <c>CONSTANT AS LENGTH OF data-name-2</c> ("determined as specified in the LENGTH intrinsic
/// function with the exception that when data-name-2 is an occurs-depending group item, the maximum size of the data
/// item is used"). It lived inside <c>IntrinsicBinder</c> while the CONSTANT entry read <see cref="DataItem.ImageWidth"/>
/// — a second, drifted copy that answered 4 for an alphanumeric group of X(2) + N(2), where the function answered 6,
/// and a bit item's byte OCCUPANCY where r1 counts its boolean positions (kb/Work PB1213). A data-model fact, so the
/// data-division and procedure-division binders both read it here.
/// </summary>
internal static class ItemLength
{
    /// <summary>§15.50.4 r1/r2/r3 for a FIXED item (kb/Work PB61): an elementary boolean item's BOOLEAN positions
    /// (r1), an elementary usage-national item's NATIONAL positions (r2), and for everything else — an
    /// alphanumeric group, a DISPLAY leaf, a COMP/PACKED leaf, an INDEX/POINTER/PROGRAM-POINTER/COMP-1/COMP-2
    /// carrier — the length "in alphanumeric character positions" (r3), which is the byte width under WiseOwl COBOL's
    /// 1-byte-per-alphanumeric-position model (D-N1: national = 2 bytes = 2 positions inside an alphanumeric group).
    /// A group's byte width is its MAXIMUM allocation, so an occurs-depending group answers its maximum — GR6's
    /// exception for the CONSTANT entry; the FUNCTION LENGTH fold never reaches an ODO group (r4 is a runtime
    /// length). A dynamic-length item (r6) is never folded here: FUNCTION LENGTH reads it at run time and §13.10.3
    /// SR12 bars it from a CONSTANT entry.</summary>
    internal static int Positions(DataItem item)
    {
        // THE ONE category reader (D20/PB79): an elementary item's own picture, a bit / national GROUP's as-if picture
        // — a bit group's boolean positions are its exact bit extent (no trailing filler), a national group's national
        // positions its character image width. Only an alphanumeric group falls to r3.
        if (item.OperandPic is { } pic)
        {
            if (pic.Category is PicCategory.Boolean) return pic.Length;                     // r1 — boolean positions
            // r2 — NATIONAL character positions, counted by THE ONE national-position authority
            // (Place.NationalWindow.PositionsOf, the same count the storage geometry and the byte-window gate
            // read), never re-derived as pic.Length. ⛔ The two differ for exactly the shape §13.18.60.3 SR12's
            // national-form numeric made reachable (kb/Work PB646): a SIGN IS SEPARATE position is a character
            // position and not a digit position (§13.18.52 GR6a), so `PIC S9(3) USAGE NATIONAL SIGN IS LEADING
            // SEPARATE` is FOUR national positions while pic.Length is its three digits — and r3's DISPLAY arm
            // below already counted the separate sign, so the two arms of ONE rule disagreed.
            // A national GROUP is not elementary: PositionsOf answers null and §13.18.29.4 GR2b's as-if
            // PICTURE N(m) length is the count (the `?? pic.Length` arm).
            if (pic.Usage is Usage.National || pic.Category is PicCategory.National)
                return NationalWindow.PositionsOf(item) ?? pic.Length;                      // r2 — national positions
        }
        return item.ByteWidth;                                                              // r3 — alphanumeric positions ≡ bytes
    }
}
