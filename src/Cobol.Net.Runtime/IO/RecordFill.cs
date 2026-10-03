// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>⛔ THE ONE RIGHT-FILL OF A RECORD IMAGE TO A DECLARED WIDTH (kb/Work PB327, PB1140): the alphanumeric
/// space fill, or the national one. Every site that pads a record image to a span — the connector's record-area fit
/// (<see cref="FileConnector"/>, §14.9.30.4 GR15) and the SORT/MERGE transfers' short-record fill (§14.9.40.4 GR7 /
/// GR16, §14.9.24.4 GR2 / GR13, <see cref="CobolSort.FillTo"/>) — routes here, so the two spaces cannot diverge;
/// WHICH space applies is each rule's own decision and arrives as <paramref name="national"/>.</summary>
internal static class RecordFill
{
    /// <summary>Pad (right) or truncate <paramref name="image"/> to exactly <paramref name="width"/> characters. One
    /// char is one byte on this channel. The alphanumeric space is the character U+0020. The NATIONAL space is the
    /// two bytes 0x00 0x20 (§13.18.60.4 GR8 leaves the size to the implementor — D-N1 pins two, UTF-16BE), written as
    /// the pair aligned to the AREA's own even byte boundary: national positions start at even offsets, and an odd
    /// short image leaves a half position whose content §14.9.30.4 GR14/GR15 do not define. Padding the bytes with
    /// 0x20 instead would manufacture U+2020 characters (the trap <c>CobolBits.NatWriteWindow</c> documents on the
    /// write side). Truncation is identical in both arms (GR15's over-length arm, in bytes).</summary>
    public static string Fit(string image, int width, bool national)
    {
        if (image.Length == width) return image;
        if (image.Length > width) return image[..width];
        if (!national) return image.PadRight(width, ' ');
        var buf = new char[width];
        image.CopyTo(0, buf, 0, image.Length);
        for (int i = image.Length; i < width; i++) buf[i] = (i & 1) == 0 ? '\0' : ' ';
        return new string(buf);
    }
}
