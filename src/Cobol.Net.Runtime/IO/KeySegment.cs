// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>One contiguous byte window of an indexed file's record key, in the record's character image. A record
/// key is a SEQUENCE of these, and its value is their images concatenated in order: ISO §12.4.5.12.4 GR2 and
/// §12.4.5.6.4 GR2 — "Record-key-name-1 defines a record key consisting of the concatenation of all occurrences
/// of data-name-2 in the order specified" — make the SOURCE form a key of one segment per data-name-2, and the
/// data-name-1 form is the one-segment case of the same shape, so the connector has ONE key extraction for both
/// (kb/Work PB1099 Q1, Annex A.3 item 40).</summary>
/// <param name="Offset">The segment's byte offset in the record image — in the record's FIXED run when
/// <paramref name="Layout"/> is set.</param>
/// <param name="Length">The segment's byte width (a national position is two bytes, D-N1).</param>
/// <param name="Layout">The record type's layout when a variable-length member precedes the segment, so the
/// connector locates it in each record as written (kb/Work PB1025, docs/CONFORMANCE.md §3 D-KWV); null for a
/// segment at a fixed position.</param>
public readonly record struct KeySegment(int Offset, int Length, CobolContiguousLayout? Layout = null);
