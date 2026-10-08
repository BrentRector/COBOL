// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Frontend.Common;

/// <summary>
/// A contiguous region in source text, defined by start position and length.
/// </summary>
/// <param name="Start">Zero-based absolute character offset of the span's first character.</param>
/// <param name="Length">Number of characters in the span.</param>
public readonly record struct TextSpan(int Start, int Length)
{
    /// <summary>A zero-length span at position zero, used when no real span is available.</summary>
    public static readonly TextSpan Empty = new(0, 0);

    /// <summary>Exclusive end position (Start + Length).</summary>
    public int End => Start + Length;

    /// <summary>Formats as a half-open interval, e.g. "[0..5)".</summary>
    public override string ToString() => $"[{Start}..{End})";
}
